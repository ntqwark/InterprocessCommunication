using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client();
            client.Connect();
        }
    }

    class Client
    {
        TcpClient tcpClient;

        public void Connect()
        {
            tcpClient = new TcpClient();
            tcpClient.Connect("localhost", 34817);

            string status = ReadMessage();
            Console.WriteLine(status);

            while (true)
            {
                string line = Console.ReadLine();
                string result = SendMessage(line);

                Console.WriteLine(result);
            }
        }

        private string ReadMessage()
        {
            byte[] lengthBytes = new byte[4];
            tcpClient.Client.Receive(lengthBytes);

            byte[] messageBytes = new byte[BitConverter.ToInt32(lengthBytes, 0)];
            tcpClient.Client.Receive(messageBytes);

            return Encoding.UTF8.GetString(messageBytes);
        }

        public string SendMessage(string message)
        {
            byte[] bytesToSend = Encoding.UTF8.GetBytes(message);
            byte[] bytesLen = BitConverter.GetBytes(bytesToSend.Length);

            tcpClient.Client.Send(bytesLen);
            tcpClient.Client.Send(bytesToSend);

            string result = ReadMessage();
            return result;
        }
    }
}
