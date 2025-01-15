// Program.cs (Server)
using Microsoft.AspNetCore.Hosting.Server;
using System;
using System.Threading.Tasks;

namespace TcpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("TCP Server Starting...");

            Console.WriteLine("Enter IP address to listen on (press Enter for localhost):");
            string ipAddress = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = "127.0.0.1";

            Console.WriteLine("Enter port number:");
            int port = int.Parse(Console.ReadLine());

            Server server = new Server(ipAddress, port);
            Console.WriteLine($"Server started on {ipAddress}:{port}");
            Console.WriteLine("Press Ctrl+C to stop the server");
            await server.StartAsync();
        }
    }
}