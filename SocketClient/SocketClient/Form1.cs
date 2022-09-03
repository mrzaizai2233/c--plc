using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace SocketClient
{
    public partial class Form1 : Form
    {
        Socket g_socket;
        private string _mainCmd = "500000FF03FF00";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string message = textBox1.Text;
            try
            {


                string cmd = "";
                cmd += "5000";//sub header
                //Accesss route
                cmd += "00";//Network No.
                cmd += "FF";//PC No.
                cmd += "03FF";//Request
                cmd += "00";
                //data length
                cmd += "0020";
                //wait time
                cmd += "000A";
                //command read
                //cmd += "0401";
                //command write
                cmd += "1401";
                //sub command
                cmd += "0000";
                //device code
                cmd += "D*";
                //device
                cmd += "001000";             
                cmd += "0002";
                cmd += "11111111";
                g_socket.Send(Encoding.ASCII.GetBytes(cmd));
             /*   byte[] returnBytes = new byte[256];
                g_socket.Receive(returnBytes);*/


            } catch (Exception ex)
            {
                MessageBox.Show("send fail");
            }


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("192.168.3.39"), 12288);
            g_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                g_socket.Connect(endPoint);
                Thread threding = new Thread(receiveFromServer);
                threding.Start();

            } catch(Exception ex) {
                MessageBox.Show("Connect Fail");
            }
            
        }

        public void receiveFromServer()
        {
            string fullResult;
            string dataString;
            int lengthResult;
            int countWord;
            while (true)
            {
                byte[] rcvByte = new byte[512];
                g_socket.Receive(rcvByte);
                textBox2.Invoke((MethodInvoker)(() => {

                    fullResult = Encoding.ASCII.GetString(rcvByte);
                    lengthResult = Convert.ToInt32(fullResult.Substring(14, 4),16);
                    dataString = fullResult.Substring(22, lengthResult);
                    countWord = (lengthResult - 4) / 4;
                    for(int i = 0; i < countWord; i++)
                    {
                        textBox2.Text += Convert.ToInt32(dataString.Substring(i *4, 4), 16).ToString() + "\n";
                    }
                }));
                byte[] dataByte = new byte[128];
                Array.Copy(rcvByte, 22, dataByte, 0, dataByte.Length);

                byte[] _4byte = new byte[4];
                
                /*textBox2.Invoke((MethodInvoker)(() =>
                {
                    Array.Copy(dataByte, 0, _4byte, 0,4);
                    //textBox2.Text += Convert.ToString(dataByte[0],2);
                    textBox2.Text += Convert.ToInt16(_4byte);
                }));*/
            }
            
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            try
            {


                string cmd = "";
                cmd += "5000";//sub header
                //Accesss route
                cmd += "00";//Network No.
                cmd += "FF";//PC No.
                cmd += "03FF";//Request
                cmd += "00";
                //data length
                cmd += "0018";
                //wait time
                cmd += "000A";
                //command read
                //cmd += "0401";
                //command write
                cmd += "0401";
                //sub command
                cmd += "0000";
                //device code
                cmd += cbDevice.Text;
                //device
                cmd += txtDeviceNumber.Text.PadLeft(6, '0');
                cmd += txtDeviceLength.Text.PadLeft(4, '0');
                g_socket.Send(Encoding.ASCII.GetBytes(cmd));
                /*   byte[] returnBytes = new byte[256];
                   g_socket.Receive(returnBytes);*/
            }
            catch (Exception ex)
            {
                MessageBox.Show("send fail");
            }
        }
    }
}
