using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new Host();

            host.RunService();
        }
    }

    class Host
    {
        TcpListener listener;
        LinkedList<Client> clients;

        int port = 34817;

        public void RunService()
        {
            listener = new TcpListener(IPAddress.Any, port);
            clients = new LinkedList<Client>();

            listener.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    ProcessClients();

                    Task.Delay(0);
                }
            });

            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                var client = new Client(tcpClient.Client);

                lock (clients)
                {
                    clients.AddLast(client);
                }

                Console.WriteLine("Accepted new client!");
            }
        }

        private void ProcessClients()
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    ProcessClient(client);
                }
            }
        }

        // состав пакета: int - длина строки в байтах, байты строки
        private void ProcessClient(Client client)
        {
            if (client.Socket.Available == 0)
                return;

            if (client.Status == Client.StatusEnum.Waiting)
            {
                if (client.Socket.Available >= 4)
                {
                    var lengthBytes = new byte[4];
                    client.Socket.Receive(lengthBytes);

                    client.BytesToReceiveCount = BitConverter.ToInt32(lengthBytes, 0);
                    client.Status = Client.StatusEnum.Receiving;
                }
            }

            if (client.Status == Client.StatusEnum.Receiving)
            {
                if (client.Socket.Available >= client.BytesToReceiveCount)
                {
                    client.Status = Client.StatusEnum.Processing;
                }
            }

            if (client.Status == Client.StatusEnum.Processing)
            {
                var messageBytes = new byte[client.BytesToReceiveCount];
                client.Socket.Receive(messageBytes);

                string message = Encoding.Unicode.GetString(messageBytes);
                Console.WriteLine("Получена строка: " + message);

                client.BytesToReceiveCount = 0;
                client.Status = Client.StatusEnum.Waiting;
            }
        }
    }

    class Client
    {
        public Socket Socket { get; set; }
        public StatusEnum Status { get; set; }
        public int BytesToReceiveCount { get; set; }

        public Client(Socket socket)
        {
            Socket = socket;
            Status = StatusEnum.Waiting;
            BytesToReceiveCount = 0;
        }

        // Waiting - длина сообщения еще не пришла (4 байта)
        // Receiving - длина сообщения получена но мы ждем все сообщение
        // Processing - все байты были получены, обработка сообщения
        public enum StatusEnum
        {
            Waiting,
            Receiving,
            Processing
        }
    }
}
