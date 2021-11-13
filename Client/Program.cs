using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new TcpClient();

            client.Connect("localhost", 34817);
            
            while (true)
            {
                Console.Write("Введите сообщение: ");

                var line = Console.ReadLine();

                var bytes = Encoding.Unicode.GetBytes(line);

                client.Client.Send(BitConverter.GetBytes(bytes.Length));
                client.Client.Send(bytes);
            }
        }
    }
}
