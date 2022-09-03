using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SocketClient
{
    class MC_Protocol
    {
        public TcpClient g_client;
        private string _mainCmd = "500000FF03FF00";
        public MC_Protocol(string IPAddress, int Port)
        {
            g_client = new TcpClient(IPAddress, Port);
            g_client.Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }
    }
}
