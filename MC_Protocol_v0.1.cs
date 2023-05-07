using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MELSEC
{

    public enum DeviceName : byte
    {
        X = 0x9C,
        Y = 0x9D,
        M = 0x90,
        L = 0x92,
        B = 0xA0,
        D = 0xA8,
        W = 0xB4,

        TS = 0xC1,
        TC = 0xC0,
        TN = 0xC2,

        CS = 0xC4,
        CC = 0xC3,
        CN = 0xC5,
    }

    public enum CommandType : int
    {
        BitRead = 0,
        BitWrite = 1,
        WordRead = 2,
        WordWrite = 3,
    }
    class MC_Protocol
    {


        bool sending = false;
        Socket g_socket ;
        IPEndPoint g_endPoint;
        byte[] bBaseCmd = { 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 };
        byte[] bMonitorTimer = { 0x10, 0x00 };
        byte[] buffer = new byte[512];

        private byte[] BuildCMD(CommandType type, DeviceName deviceName, int headDeviceNumber, int numberOfDevicePoints, byte[] bData = null)
        {
            byte[] bRequest;
            byte[] bDataRequest;
            byte[] bDataLength = new byte[2];
            byte[] MainCommand = new byte[4];
            byte[] bHeadDeviceNumber = new byte[3];
            byte[] bNumberOfDevicePoints = new byte[2];
            byte[] bDeviceCode = new byte[1];
           
            Array.Copy(BitConverter.GetBytes(Convert.ToByte(deviceName)), bDeviceCode, 1);
            Array.Copy(BitConverter.GetBytes(headDeviceNumber), bHeadDeviceNumber, 3);
            Array.Copy(BitConverter.GetBytes(numberOfDevicePoints), bNumberOfDevicePoints, 2);
            switch (type)
            {
                case CommandType.BitRead:
                    MainCommand = new byte[] { 0x01, 0x04, 0x01, 0x00 };
                    break;
                case CommandType.BitWrite:
                    MainCommand = new byte[] { 0x01, 0x14, 0x01, 0x00 };
                    break;
                case CommandType.WordRead:
                    MainCommand = new byte[] { 0x01, 0x04, 0x00, 0x00 };
                    break;
                case CommandType.WordWrite:
                    MainCommand = new byte[] { 0x01, 0x14, 0x00, 0x00 };
                    break;
            }

            bDataRequest = 
                bMonitorTimer
                .Concat(MainCommand)
                .Concat(bHeadDeviceNumber)
                .Concat(bDeviceCode)
                .Concat(bNumberOfDevicePoints).ToArray();
  
            if (type == CommandType.BitWrite)
            {
                bDataRequest = bDataRequest.Concat(bData).ToArray();
            }
            bDataLength = BitConverter.GetBytes(Convert.ToInt16(bDataRequest.Length));
            bRequest = bBaseCmd.Concat(bDataLength).Concat(bDataRequest).ToArray();

            return bRequest;
        }


        public MC_Protocol(string adress, int port, ProtocolType type = ProtocolType.Tcp)
        {
            
            g_endPoint = new IPEndPoint(IPAddress.Parse(adress), port);
            if (type == ProtocolType.Tcp)
                g_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            else
                g_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        }

         public void receiveFromServer()
        {
           
            byte[] lenByteOfResult = new byte[2];
            int lenOfResult;
            while (true)
            {
                byte[] rcvByte = new byte[512];
                g_socket.Receive(rcvByte);
                //get len of result in recevie data
                Array.Copy(rcvByte, 7, lenByteOfResult, 0, 2);
                lenOfResult = BitConverter.ToInt16(lenByteOfResult,0);
                //first 2 byte is end code
                byte[] byteOfData = new byte[lenOfResult - 2];

                Array.Copy(rcvByte, 11, byteOfData, 0, lenOfResult - 2);
                string dataString = "";
                foreach (byte b in byteOfData)
                {
                    dataString += Convert.ToString(b, 2);
                }

                
            }
            
        }


         private byte[] getDataBytesInResult(byte[] buffer)
         {
            //byte 7 và byte 8 là lưu độ dài của mảng byte data
            int lenOfResult;
           
            //copy byte 7 và byte 8 sang một mảng mới
            lenOfResult = BitConverter.ToInt16(buffer,7);
            //first 2 byte is end code
            byte[] byteOfData = new byte[lenOfResult - 2];
            Array.Copy(buffer, 11, byteOfData, 0, lenOfResult - 2);
            return byteOfData;
        }

         public void WriteBits(DeviceName deviceName, int headOfDeviceNumber, string values)
         {
             int numberOfDevicePoints = values.Length;
             byte[] bWriteData = dataForWriteBit(values);

             byte[] cmd = BuildCMD(CommandType.BitWrite, deviceName, headOfDeviceNumber, numberOfDevicePoints, bWriteData);
             string cmdString = BitConverter.ToString(cmd, 0);
             g_socket.Send(cmd);

         }

         public void WriteBit(DeviceName deviceName, int headOfDeviceNumber, bool value)
         {
             string v = "0";
             if (value == true)
                 v = "1";
             WriteBits(deviceName, headOfDeviceNumber, v);
         }


        public string ReadBits(DeviceName deviceName,int headOfDeviceNumber,int numberOfDevice = 1){
            byte[] buffer = new byte[512];
            byte[] byteOfData;
            byte[] cmd = BuildCMD(CommandType.BitRead, deviceName, headOfDeviceNumber, numberOfDevice);

            g_socket.Send(cmd);
          
            int byteReceived = g_socket.Receive(buffer);
            byteOfData = getDataBytesInResult(buffer);
            //return 0x10,0x11 ...
            // neu so luong la le thi xoa phan tu cuoi
            return BitConverter.ToString(byteOfData, 0).Replace("-", "").Remove(byteOfData.Length - 1);
        }

        public void WriteBatchWord(DeviceName deviceName, int headOfDeviceNumber, byte[] byteData, int numberOfDevice = 1)
        {
            byte[] cmd = BuildCMD(CommandType.WordWrite, deviceName, headOfDeviceNumber, numberOfDevice, byteData);
        }

        public void WriteWord(DeviceName deviceName, int headOfDeviceNumber, int data)
        {
            WriteBatchWord(deviceName, headOfDeviceNumber, BitConverter.GetBytes(data));
        }

        private string ConvertBytesToBitString(byte[] data)
        {
            string outString = "";
            foreach (byte b in data)
            {
                outString += Convert.ToString(b, 2);
            }
            return outString;
        }


        private byte[] processDataForSend(CommandType cmdType,string data, int dataLength)
        {
            byte[] byteData = new byte[dataLength/2];
            foreach(char b in data){

            }
            return byteData;
        }

        private byte[] dataForWriteBit(string data)
        {
            // send data from M0->M4 data cần gửi là 1011 cần chuyển chuỗi này về byte để gửi
            // thì cứ 1 byte là dữ liệu của 2 bit 0x10,0x11 => 0001 0000 0001 0001
            // nếu số lượng bit là lẻ thì cần phải chuyển sang thành số chẵn thêm 0 vào chuỗi dữ liệu gửi

            // kiểm tra độ dài nếu là lẻ thì thêm 0
            if (data.Length % 2 != 0)
            {
                data +="0";
            }
            //2 bit thì dùng 1 byte nên tạo byte data bằng việc độ dài của bit/2
            byte[] byteData = new byte[data.Length/2];
            int currentIndex = 0;
            string bitString = "";
            //lấy từng kí tự để thêm 000 vào cho thành 1/2 byte
            foreach (char c in data)
            {
                //1=>0001
                bitString += c.ToString().PadLeft(4, '0');
                currentIndex++;
                //khi đến 2 kí từ thì thì đủ 8 bit cho 1 byte thì chuyển nó thành 1 byte
                if (currentIndex %2 == 0 )
                {
                    byteData[(currentIndex/2)-1] = Convert.ToByte(bitString, 2);
                    bitString = "";
                }
            }

            return byteData;

        }

        private byte[] dataForWriteWord(string data)
        {
            return new byte[] { };
        }
   
        //
        public int Connect(){
            try
            {
                g_socket.Connect(g_endPoint);
            
                return 1;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

    }



}
