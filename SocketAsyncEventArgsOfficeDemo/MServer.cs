using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketAsyncEventArgsOfficeDemo
{
    class MServer
    {
        private int m_numConnections;       //最大连接数
        private int m_receiveBufferSize;    //每个套接字I/O操作的缓存区大小
        BufferManager m_bufferManager;      //表示所有套接字操作的可重用缓存区集
        const int opsToPreEAlloc = 2;        //读、写(与accepts无关)
        Socket listenSocket;                //监听Socket
        //可重用的SocketAsyncEventArgs对象池，用于写入、读取和接收套接字操作
        SocketAsyncEventArgsPool m_readWritePool;
        int m_totalBytesRead;               //服务器接收到的总字符数
        int m_numConnectedSockets;          //连接到服务器的客户端总数
        Semaphore m_maxNumberAcceptedClients;       //多线程信号量
        Dictionary<string, SocketAsyncEventArgs> m_sendSaeaDic;   //保存发送SAEA


        //创建一个未初始化的服务器实例
        //开始监听
        //先调用Init方法，然后调用Start方法
        //
        //<param name = "numConnections">同时处理的最大连接数</param>
        //<param name = "receiveBufferSize">用于每个套接字操作的缓存区大小</param>
        public MServer(int numConnections, int receiveBufferSize)
        {
            m_totalBytesRead = 0;               
            m_numConnectedSockets = 0;                  //已连接的客户端数量
            m_numConnections = numConnections;          //可以连接的客户端最大数量
            m_receiveBufferSize = receiveBufferSize;    //每个套接字I/O操作的缓存区大小

            //初始化BufferManeger对象，设置缓存区总的大小，这里还没有真正的分配
            //大小为：最大连接数量客户端  每个有读、写两块区域，大小都为receiveBufferSize
            m_bufferManager = new BufferManager(receiveBufferSize * numConnections * opsToPreEAlloc,
                                                receiveBufferSize);
            m_readWritePool = new SocketAsyncEventArgsPool(numConnections * 2);     //初始化了SAEA对象池，最大容量为总连接数量的两倍
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);     //初始化信号量，param1为剩余数量，param2为总数量
            m_sendSaeaDic = new Dictionary<string, SocketAsyncEventArgs>();         //初始化sendSAEA字典
        }

        //通过预分配可重用缓存区和上下文对象来初始化服务器。这些对象不需要
        //预先分配或重用，这样做使为了说明如何轻松地使用API创建可重用对象以
        //提高服务器性能
        public void Init()
        {
            //这里才真正向系统申请缓存区
            m_bufferManager.InitBuffer();

            //声明一个SAEA对象，用于后面的操作
            SocketAsyncEventArgs readWriteEventArg;

            //new最大连接数量个SAEA对象，依次绑定好事件(或者委托?)，分配用户信息存储空间，分配好空间，然后放入SAEA池
            for (int i = 0; i < m_numConnections * 2; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                //绑定完成事件
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                //分配用户信息空间，UserToken是object类型的对象，而右边的new AsyncUserToken()是自定义的用户信息类
                readWriteEventArg.UserToken = new AsyncUserToken();

                //将缓冲池中的字节缓冲区分配给SAEA对象
                m_bufferManager.SetBuffer(readWriteEventArg);

                //将SAEA放入SAEA池中
                m_readWritePool.Push(readWriteEventArg);
            }
        }

        //启动服务，监听连接请求
        //
        //<param name = "localEndPoint"> 服务端监听的目标端口 </param>
        public void Start(IPEndPoint localEndPoint)
        {
            //创建监听用socket
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            //开始监听，最大同时监听数为100；
            listenSocket.Listen(100);

            //开始接收连接
            StartAccept(null);

            Console.WriteLine("按任意键终止服务器进程...");
            Console.ReadKey();          //获取用户按下的下一个字符或者功能键
        }

        //开始接收服务器请求
        //
        //<param nmame = "acceptEvent">在服务器的监听套接字上发出accept操作时使用的上下文对象</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            //这里应该是使用一个新的SAEA用于接收连接
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);              
            }
            else
            {
                //因为正在重用上下文对象，所以必须清除套接字
                acceptEventArg.AcceptSocket = null;           
            }


            //申请一个信号量
            m_maxNumberAcceptedClients.WaitOne();
            //将acceptEventArg用于接收连接，如果IO挂起，则返回true
            Console.WriteLine("开始等待连接");
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }

        }

        //此方法是和Acceptsync关联的回调函数，在接收完成时调用
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
    

        //starty()与ProcessAccept()构成一个接受连接的循环
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            //原子操作，使m_numConnectedSockets增加1
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted.There are {0} clients connected to the server", m_numConnectedSockets);
            //弹出一个SAEA池中的SAEA,这个SAEA是用来接收消息的
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            //获取已接受的客户端连接的套接字和IP端口放入用户信息中(receiveSaea)
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;
            ((AsyncUserToken)readEventArgs.UserToken).Remote = e.RemoteEndPoint;

            //弹出一个SAEA，这个SAEA是用来发送消息的
            SocketAsyncEventArgs sendSaea = m_readWritePool.Pop();
            //将发送SAEA放入sendSaeaDic字典，方便以后使用
            m_sendSaeaDic.Add(e.AcceptSocket.RemoteEndPoint.ToString(), sendSaea);

            //获取已接受的客户端连接的套接字和IP端口放入用户信息中(sendSaea)
            ((AsyncUserToken)sendSaea.UserToken).Socket = e.AcceptSocket;
            ((AsyncUserToken)sendSaea.UserToken).Remote = e.RemoteEndPoint;

            Console.WriteLine("The Client IP:" + e.RemoteEndPoint);

            //e.AcceptSocket代表已接收到的客户端连接的socket
            //将readEventArgs用于接收消息
            //IO挂起后，返回true，同时引发参数的Completed事件   如果IO操作同步完成，返回false，并且不会引发Completed事件
            //这里的ReceiveAsync是必须的，是为了进入读取->接收->读取的循环。当然，在后面也可以自己写成读取->读取的内循环
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }

            //接收下一个连接
            StartAccept(e);
        }

        //当一个接收或者发送操作完成时调用此函数
        //
        //<param name = "e"> SocketAsyncEventArg </param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            //确定刚刚完成的操作类型并调用关联的处理程序
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("Last Operation error");
            }
        }

        //异步接收操作完成时调用此方法
        //如果远程主机关闭了连接，那么就关闭套接字
        //接收到了数据，将数据返回给客户端
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                //检查这个远程主机是否关闭连接
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //增加服务器接收到的总字数
                    Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                    Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);

                    //读取数据
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                    string str = System.Text.Encoding.Default.GetString(data);

                    Console.WriteLine("Receive message :" + str);

                    //将数据返回给客户端
                    e.SetBuffer(e.Offset, e.BytesTransferred);
                    //这里是从读取到发送
                    bool willRaiseEvent = token.Socket.SendAsync(e);
                    Console.WriteLine("开始等待接收");
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception xe)
            {
                Console.WriteLine(xe.Message + "\r\n" + xe.StackTrace);
            }
        }

        //异步发送操作完成时调用此方法
        //该方法在套接字上发出另一个接收以读取任何其他接收  ??
        //从客户端发送的数据
        //
        //<param name = "e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                //完成了将数据返回给客户端
                AsyncUserToken token = (AsyncUserToken)e.UserToken;


                //这里就是从发送循环到读取
                //读取从客户端发送的下一个数据块
                //bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                //Console.WriteLine("开始等待发送");
                //if (!willRaiseEvent)
                //{
                //    ProcessReceive(e);
                //}
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            //关闭与客户端关联的套接字
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            //throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            //原子操作，使m_numConnectedSockets减一
            Interlocked.Decrement(ref m_numConnectedSockets);

            //释放SocktAsyncEventArg，然后可被重用到其他客户端(这里使放入)
            m_readWritePool.Push(e);
            m_readWritePool.Push(m_sendSaeaDic[token.Remote.ToString()]);
            m_sendSaeaDic.Remove(token.Remote.ToString());

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has benn disconnected from the server.There are {0} clients connected to the server", m_numConnectedSockets);
        }
    }
}
