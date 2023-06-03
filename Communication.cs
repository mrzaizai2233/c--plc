using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace IDLOGSCONSOLE
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

    public enum CommandType
    {
        BitRead = 0,
        BitWrite = 1,
        WordRead = 2,
        WordWrite = 3
      
    }

    public enum BatchCommandType
    {
        RandomRead = 0,
        RandomWrite = 1
    }
    class BatchCMD
    {
        byte[] bCMD = { };

        public int NumberOfWord = 0;
        public int NumberOfDword = 0;

        byte[] bNumberOfWord = new byte[1];

        byte[] bNumberOfDword = new byte[1];
        
        public BatchCMD()
        {

        }

        public byte[] ByteCMD
        {
            get { 
                return bCMD; 
            }
        }

        public byte[] FinalByteCMD
        {
            get
            {
                return bNumberOfWord.Concat(bNumberOfDword).Concat(bCMD).ToArray();
            }
        }

        public void IncCountWord()
        {
            NumberOfWord++;
            byte[] bLength = BitConverter.GetBytes(NumberOfWord);
            Array.Copy(bLength, bNumberOfWord, 1);

            
        }
        public void IncCountDword()
        {
            NumberOfDword++;
            byte[] bLength = BitConverter.GetBytes(NumberOfDword);
            Array.Copy(bLength, bNumberOfDword, 1);

        }
        public void AddWord(DeviceName deviceName, int headDeviceNumber, byte[] data = null)
        {
            IncCountWord();
            AddCMD(deviceName, headDeviceNumber, data);

        }
        public void AddDword(DeviceName deviceName, int headDeviceNumber, byte[] data = null)
        {
            IncCountDword();
            AddCMD(deviceName, headDeviceNumber, data);
        }
        public void AddCMD(DeviceName deviceName, int headDeviceNumber, byte[] data = null)
        {
            byte[] bDeviceCode = new byte[1];
            byte[] bHeadDeviceNumber = new byte[3];
            Array.Copy(BitConverter.GetBytes(headDeviceNumber), bHeadDeviceNumber, 3);
            Array.Copy(BitConverter.GetBytes(Convert.ToByte(deviceName)), bDeviceCode, 1);
            bCMD = bCMD.Concat(bHeadDeviceNumber).Concat(bDeviceCode).ToArray();
        }
    }
    class MC_Protocol
    {
        ProtocolType g_type;
        Socket g_socket;
        IPEndPoint g_endPoint;

        BatchCMD batchCMD;

        byte[] bBaseCmd = { 0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 };
        byte[] bMonitorTimer = { 0x10, 0x00 };

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

            if (type == CommandType.BitWrite || type == CommandType.WordWrite)
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
            g_type = type;
        }

        private byte[] BuildBatchCMD(BatchCommandType type , BatchCMD batchCMD)
        {
            byte[] bRequest;
            byte[] MainCommand = new byte[4];
            byte[] bDataLength;

            switch (type)
            {
                case BatchCommandType.RandomRead:
                    MainCommand = new byte[] { 0x03, 0x04, 0x00, 0x00 };
                    break;
                case BatchCommandType.RandomWrite:
                    MainCommand = new byte[] { 0x03, 0x14, 0x01, 0x00 };
                    break;
            }
            bDataLength = BitConverter.GetBytes(Convert.ToInt16(batchCMD.FinalByteCMD.Length + 6));
            return  bBaseCmd.Concat(bDataLength).Concat(bMonitorTimer).Concat(MainCommand).Concat(batchCMD.FinalByteCMD).ToArray();
        }

       

        private byte[] getDataBytesInResult(byte[] buffer)
        {
            //byte 7 và byte 8 là lưu độ dài của mảng byte data
            int lenOfResult;

            //copy byte 7 và byte 8 sang một mảng mới
            lenOfResult = BitConverter.ToInt16(buffer, 7);
            if (lenOfResult == 0)
                return BitConverter.GetBytes(0);
            //first 2 byte is end code
            byte[] byteOfData = new byte[lenOfResult - 2];
            Array.Copy(buffer, 11, byteOfData, 0, lenOfResult - 2);
            return byteOfData;
        }


        public string ReadBits(DeviceName deviceName, int headOfDeviceNumber, int numberOfDevice = 1)
        {

            byte[] byteOfData;
            byte[] cmd = BuildCMD(CommandType.BitRead, deviceName, headOfDeviceNumber, numberOfDevice);
            byte[] buffer = SendCommand(cmd);
         
            byteOfData = getDataBytesInResult(buffer);

            //return 0x10,0x11 ...
            // neu so luong la le thi xoa phan tu cuoi
            string result = BitConverter.ToString(byteOfData, 0).Replace("-", "");

            if (numberOfDevice % 2 != 0)
                result = result.Remove(numberOfDevice);
            return result;
        }

        public bool ReadBit(DeviceName deviceName, int headOfDeviceNumber)
        {
            string result = ReadBits(deviceName, headOfDeviceNumber);
            if (result == "0")
                return false;
            else
                return true;
        }

        public short[] ReadWords(DeviceName deviceName, int headOfDeviceNumber, int numberOfDevice = 1)
        {
            byte[] byteOfData;
            byte[] cmd = BuildCMD(CommandType.WordRead, deviceName, headOfDeviceNumber, numberOfDevice);
            byte[] buffer = SendCommand(cmd);
            byteOfData = getDataBytesInResult(buffer);
            return ByteToWord(byteOfData);
        }

        public int[] ReadDWords(DeviceName deviceName, int headOfDeviceNumber, int numberOfDevice = 1)
        {
            byte[] byteOfData;
            byte[] cmd = BuildCMD(CommandType.WordRead, deviceName, headOfDeviceNumber, numberOfDevice *= 2);
            byte[] buffer = SendCommand(cmd);
            byteOfData = getDataBytesInResult(buffer);
            return ByteToDword(byteOfData);
        }

        public string ReadString(DeviceName deviceName, int headOfDeviceNumber, int numberOfDevice = 1)
        {
            byte[] byteOfData;
            byte[] cmd = BuildCMD(CommandType.WordRead, deviceName, headOfDeviceNumber, numberOfDevice);
            byte[] buffer = SendCommand(cmd);
            byteOfData = getDataBytesInResult(buffer);
            return Encoding.ASCII.GetString(byteOfData).Trim();
        }

        public byte[] ReadBatch(BatchCMD batchCMD)
        {
            byte[] cmd = BuildBatchCMD(BatchCommandType.RandomRead, batchCMD);
            byte[] buffer =  SendCommand(cmd);
            // tính số lượng byte cần lấy ra từ buffer
            //1 WORD = 2 byte => NumberOfWord * 2
            //1 DWORD = 2 DWORD => NumberOfDword * 4
            int totalByte = (batchCMD.NumberOfDword * 4) + (batchCMD.NumberOfWord * 2);
            byte[] ByteOfData = new byte[totalByte];
            //Do batchRead không có số lượng byte trả về trong buffer nên lấy từ byte thứ 11 -> tổng số NumberOfWord + NumberOfDword
            Array.Copy(buffer, 11, ByteOfData, 0, totalByte );

            return ByteOfData;


        }

        public void WriteBits(DeviceName deviceName, int headOfDeviceNumber, string values)
        {
            int numberOfDevicePoints = values.Length;
            byte[] bWriteData = dataForWriteBit(values);

            byte[] cmd = BuildCMD(CommandType.BitWrite, deviceName, headOfDeviceNumber, numberOfDevicePoints, bWriteData);
            SendCommand(cmd);
        }

        public void WriteBit(DeviceName deviceName, int headOfDeviceNumber, bool value)
        {
            string v = "0";
            if (value == true)
                v = "1";
            WriteBits(deviceName, headOfDeviceNumber, v);
        }

        public void WriteBatchWord(DeviceName deviceName, int headOfDeviceNumber, byte[] byteData, int numberOfDevice = 1)
        {
            byte[] cmd = BuildCMD(CommandType.WordWrite, deviceName, headOfDeviceNumber, numberOfDevice, byteData);
            SendCommand(cmd);
        }

        public void WriteWord(DeviceName deviceName, int headOfDeviceNumber, short data)
        {
            WriteBatchWord(deviceName, headOfDeviceNumber, BitConverter.GetBytes(data));
        }

        public void WriteString(DeviceName deviceName, int headOfDeviceNumber, string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            WriteBatchWord(deviceName, headOfDeviceNumber, bytes, bytes.Length);
        }

        private short[] ByteToWord(byte[] bytes)
        {
            int lenOfByte = bytes.Length;
            int numberOfWord = lenOfByte / 2;
            short[] results = new short[numberOfWord];
            int indexOfByte = 0;
            for (int i = 0; i < numberOfWord; i++)
            {
                results[i] = BitConverter.ToInt16(bytes, indexOfByte);
                indexOfByte += 2;
            }
            return results;

        }

        private int[] ByteToDword(byte[] bytes)
        {
            int lenOfByte = bytes.Length;
            int numberOfWord = lenOfByte / 4;
            int[] results = new int[numberOfWord];
            int indexOfByte = 0;
            for (int i = 0; i < numberOfWord; i++)
            {
                results[i] = BitConverter.ToInt32(bytes, indexOfByte);
                indexOfByte += 4;
            }
            return results;

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


        private byte[] processDataForSend(CommandType cmdType, string data, int dataLength)
        {
            byte[] byteData = new byte[dataLength / 2];
            foreach (char b in data)
            {

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
                data += "0";
            }
            //2 bit thì dùng 1 byte nên tạo byte data bằng việc độ dài của bit/2
            byte[] byteData = new byte[data.Length / 2];
            int currentIndex = 0;
            string bitString = "";
            //lấy từng kí tự để thêm 000 vào cho thành 1/2 byte
            foreach (char c in data)
            {
                //1=>0001
                bitString += c.ToString().PadLeft(4, '0');
                currentIndex++;
                //khi đến 2 kí từ thì thì đủ 8 bit cho 1 byte thì chuyển nó thành 1 byte
                if (currentIndex % 2 == 0)
                {
                    byteData[(currentIndex / 2) - 1] = Convert.ToByte(bitString, 2);
                    bitString = "";
                }
            }

            return byteData;

        }

        private byte[] SendCommand(byte[] cmd)
        {
            byte[] buffer = new byte[512];
            try
            {
                Socket socket = (g_type == ProtocolType.Tcp) ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) : g_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);



                socket.SendTimeout = 100;
                socket.ReceiveTimeout = 100;
                Console.WriteLine("Connecting");
                IAsyncResult result  = socket.BeginConnect(g_endPoint,null,null);
                bool success = result.AsyncWaitHandle.WaitOne(100, false);
                if (success)
                {
                    socket.EndConnect(result);

                    success = false;
                    Console.WriteLine("Sending");
                    socket.Send(cmd);
                    Console.WriteLine("Reciving");
                    socket.Receive(buffer);
                    Console.WriteLine("shutdowning");
                    socket.Shutdown(SocketShutdown.Both);
                    Console.WriteLine("Disconnecting");
                    socket.Disconnect(false);
                    Console.WriteLine("Closing");
                    socket.Close();
                    Console.WriteLine("Close complete");
                } else {
                    socket.Close();
                }

                

            }
            catch (Exception e){ };
            return buffer;

        }
        //l
        public void Connect()
        {
            try
            {
                g_socket = (g_type == ProtocolType.Tcp) ? new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) : g_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                g_socket.Connect(g_endPoint);
            }
            catch (Exception e) { throw new Exception(e.Message); }

        }

        public void Disconnect()
        {
            try
            {
                g_socket.Close();
            }
            catch (Exception e) { throw new Exception(e.Message); }

        }

    }



}