using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncEventArgsOfficeDemo
{

    //用户类

    class AsyncUserToken
    {
        ///<summary>
        ///客户端IP地址
        ///</summary>
        public IPAddress IPAddress { get; set; }

        ///<summary>
        ///远程地址
        ///</summary>>
        public EndPoint Remote { get; set; }

        ///<summary>
        ///通信SOCKET
        ///</summary>>
        public Socket Socket { get; set; }

        ///<summary>
        ///连接时间
        ///</summary>
        public DateTime ConnectTime { get; set; }

        ///<symmary>
        ///包长
        ///</symmary>
        public int packageLen { get; set; }

        ///<symmary>
        ///包类型
        ///</symmary>
        public int packageType { get; set; }

        ///<symmary>
        ///是否还有数据可以发送
        ///</symmary>
        public List<int> sendPacketNum { get; set; }

        ///<summary>
        ///所属用户信息      要自定义
        ///</summary>>
        //public UserInfoModel UserInfo { get; set; }

        ///<summary>
        ///数据缓存区
        ///</summary>
        public List<byte> receiveBuffer { get; set; }
        public List<byte> sendBuffer { get; set; }

        public AsyncUserToken()
        {
            this.receiveBuffer = new List<byte>();
            this.sendBuffer = new List<byte>();
            this.sendPacketNum = new List<int>();
            this.packageLen = 0;
            this.packageType = 0;
        }
    }
}
