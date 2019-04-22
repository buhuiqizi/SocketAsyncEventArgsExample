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

        ///<summary>
        ///所属用户信息      要自定义
        ///</summary>>
        //public UserInfoModel UserInfo { get; set; }

        ///<summary>
        ///数据缓存区
        ///</summary>
        public List<byte> Buffer { get; set; }

        public AsyncUserToken()
        {
            this.Buffer = new List<byte>();
        }
    }
}
