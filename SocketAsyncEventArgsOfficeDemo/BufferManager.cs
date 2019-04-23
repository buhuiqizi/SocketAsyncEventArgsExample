using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncEventArgsOfficeDemo
{

    //整个类的逻辑，先申请一块很大的缓存区区域 m_buffer,然后开始申请和释放
    //释放SAEA占有的缓存区后，将下标存入m_freeIndexPool堆栈中
    //当申请缓存区时，先看看m_freeIndexPool中有没有元素，有元素就说明有一块用了又被还回来的缓存区，使用这个缓存区
    //如果m_freeIndexPool中没有剩余的缓存区，那么就考虑从m_buffer中申请一块足够大的缓存区
    //因为给每个SAEA分配的缓存区大小都是一样的，所以不会有碎片问题


    class BufferManager
    {
        int m_numBytes;     //缓冲区控制的总字节数
        byte[] m_buffer;    //缓冲区管理器维护的基础字节数组

        Stack<int> m_freeIndexPool;     //int 类型的堆栈,后进先出
        int m_currentIndex;             //缓存区已占用大小
        int m_bufferSize;               //每个对象分配的缓存区大小

        //初始化成员变量
        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }

        //分配整个缓冲池所使用的缓存区空间
        public void InitBuffer()
        {
            //创建一个大缓冲区并将会分给每个SocketAsyncEventArgs对象
            m_buffer = new byte[m_numBytes];
        }

        //将缓冲池的缓冲区分配给指定的SocketAsyncEvetnArgs对象
        //
        //<return> 成功返回true,失败返回false </return>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            //如果m_freeIndexPoll中没有剩余的缓存区，那么就从m_buffer中分配缓存区
            if (m_freeIndexPool.Count > 0)
            {
                //buffer 用于异步套接字的缓存区  offset 数据缓存区开始位置处的偏移量 m_bufferSize 可在缓存区发送或接收的最大数据量
                //这里是从m_freeIndexPool中取出一个int类型的下标值
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
            }
            else
            {
                //如果剩下的缓存区小于需要的缓存区，则分配失败
                if ((m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                //将以占用下标往后移
                m_currentIndex += m_bufferSize;
            }
            return true;
        }

        //从一个SocketAsyncEventArg移除缓存
        //将这个缓存放回缓存池
        public  void FreeBuffer(SocketAsyncEventArgs args)
        {
            //向m_freeIndexpool中添加一个int类型对象 offset就是获取的偏移量，是setbuffer时自己传入的
            m_freeIndexPool.Push(args.Offset);
            //将SAEA对象指向的缓存区为null,0,0
            args.SetBuffer(null, 0, 0);
        }
    }
}
