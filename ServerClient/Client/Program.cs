using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
           

            try
            {
                // Connect to a Remote server
                // Get Host IP Address that is used to establish a connection
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1
                // If a host has multiple addresses, you will get a list of addresses
               
                
              


                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("192.168.1.39"), 12288);

                // Create a TCP/IP  socket.
                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    client.Connect(remoteEP);
                    Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());
                    //"5000 00 FF 03FF 00 0018 0010 0401 0000 M* 000010 0005
                    //Header//Sub Header//Access Route//Request Data Length//Monitoring Timer//Request Data
                    String cmd = "";
                    //Sub Header
                    cmd = "";
                    cmd = cmd + "5000";// sub HEAD (NOT)
                    //Access route
                    cmd = cmd + "00";   //  network number (NOT)
                    cmd = cmd + "FF";   //  PLC NUMBER
                    cmd = cmd + "03FF"; //  Request destination module I/ O No.
                    cmd = cmd + "00";   //  Request destination module station No.
                    //Request Data Length
                    cmd = cmd + "0018";//  Length of demand data (24)
                    //Monitoring Timer
                    cmd = cmd + "000A";//  wait time for respone (x250ms) CPU inspector data 4
                    //Request Data
                    cmd = cmd + "0401";//  Read command 4
                    cmd = cmd + "0000";//  Sub command 4
                    cmd = cmd + "D*";//   device code 2
                    cmd = cmd + "009500"; //device number 6 
                    cmd = cmd + "0001";  //couter device  4  total 24

                    /*String cmd = "";
                    String OutAddress = "123";
                    cmd = "";
                    cmd = cmd + "5000";// sub HEAD (NOT)
                    cmd = cmd + "00";//   network number (NOT)
                    cmd = cmd + "FF";//PLC NUMBER
                    cmd = cmd + "03FF";// DEMAND OBJECT MUDULE I/O NUMBER
                    cmd = cmd + "00";//  DEMAND OBJECT MUDULE DEVICE NUMBER
                    cmd = cmd + "001C";//  Length of demand data
                    cmd = cmd + "000A";//  CPU inspector data
                    cmd = cmd + "1401";//  Write command
                    cmd = cmd + "0000";//  Sub command
                    cmd = cmd + "D*";//   device code
                    cmd = cmd + "009501"; //adBase 
                    cmd = cmd + OutAddress;  //BASE ADDRESS*/

                    // Send the data through the socket.
                    int bytesSent = client.Send(Encoding.ASCII.GetBytes(cmd));
                    byte[] bytes = new byte[512];
                    // Receive the response from the remote device.
                    Console.WriteLine(cmd);
                    int bytesRec = client.Receive(bytes);
                    Console.WriteLine(Encoding.ASCII.GetString(bytes));
                    Console.ReadLine();
                    int a = 5;
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.ReadLine();
        }
    }
}
