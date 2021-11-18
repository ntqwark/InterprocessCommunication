using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBase();
            host.RunService();
        }
    }

    class HostBase
    {
        TcpListener listener;
        LinkedList<ClientBase> clients;

        int port = 34817;

        public void RunService()
        {
            listener = new TcpListener(IPAddress.Any, port);
            clients = new LinkedList<ClientBase>();

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
                var client = new ClientBase(tcpClient.Client);
                client.SendMessage("connected");

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
                var disconnectedClients = new LinkedList<ClientBase>();

                foreach (var client in clients)
                {
                    try
                    {
                        ProcessClient(client);
                    }
                    catch (Exception x)
                    {
                        Console.Error.WriteLine($"При обработке клиента {client.Socket.RemoteEndPoint} возникла ошибка:");
                        Console.Error.WriteLine(x.Message);

                        try { client.SendMessage("Dalbaeb ti"); } catch { }

                        disconnectedClients.AddLast(client);
                    }
                }

                foreach (var client in disconnectedClients)
                {
                    client.Socket.Close();
                    clients.Remove(client);
                }

                Console.Title = $"{clients.Count}";
            }
        }

        // состав пакета: int - длина строки в байтах, байты строки
        private void ProcessClient(ClientBase client)
        {
            if (client.Socket.Available == 0)
                return;

            if (client.Status == ClientBase.StatusEnum.Waiting)
            {
                if (client.Socket.Available >= 4)
                {
                    var lengthBytes = new byte[4];
                    client.Socket.Receive(lengthBytes);

                    client.BytesToReceiveCount = BitConverter.ToInt32(lengthBytes, 0);
                    client.Status = ClientBase.StatusEnum.Receiving;

                    if (client.BytesToReceiveCount == 0)
                    {
                        throw new Exception("Wrong package length");
                    }
                }
            }

            if (client.Status == ClientBase.StatusEnum.Receiving)
            {
                if (client.Socket.Available >= client.BytesToReceiveCount)
                {
                    client.Status = ClientBase.StatusEnum.Processing;
                }
            }

            if (client.Status == ClientBase.StatusEnum.Processing)
            {
                var messageBytes = new byte[client.BytesToReceiveCount];
                client.Socket.Receive(messageBytes);

                string message = Encoding.UTF8.GetString(messageBytes);

                ProcessReceivedMessage(client, message);

                client.BytesToReceiveCount = 0;
                client.Status = ClientBase.StatusEnum.Waiting;
            }
        }

        private void ProcessReceivedMessage(ClientBase client, string message)
        {
            Console.WriteLine($"Получена строка: {message}");
            Console.WriteLine($"  От: {client.Socket.RemoteEndPoint}");

            client.SendMessage("ok");
        }
    }

    class ClientBase
    {
        public Socket Socket { get; set; }
        public StatusEnum Status { get; set; }
        public int BytesToReceiveCount { get; set; }

        public ClientBase(Socket socket)
        {
            Socket = socket;
            Status = StatusEnum.Waiting;
            BytesToReceiveCount = 0;
        }

        public void SendMessage(string message)
        {
            byte[] bytesToSend = Encoding.UTF8.GetBytes(message);
            byte[] bytesLen = BitConverter.GetBytes(bytesToSend.Length);

            Socket.Send(bytesLen);
            Socket.Send(bytesToSend);
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
