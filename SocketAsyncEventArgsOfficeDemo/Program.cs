using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SocketAsyncEventArgsOfficeDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            MServer m_socket = new MServer(200, 1024);
            m_socket.Init();
            m_socket.Start(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 5730));
        }
    }
}
