// Server.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace TcpServer
{
    public class Server
    {
        private TcpListener _server;
        private readonly Dictionary<string, List<Dictionary<string, int>>> _data;
        private readonly byte[] _key = Encoding.UTF8.GetBytes("YourSecretKey123");
        private readonly byte[] _iv = Encoding.UTF8.GetBytes("YourSecretIV1234");

        public Server(string ipAddress, int port)
        {
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);
            _data = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, int>>>>(
                "{\"SetA\":[{\"One\":1,\"Two\":2}],\"SetB\":[{\"Three\":3,\"Four\":4}],\"SetC\":[{\"Five\":5,\"Six\":6}],\"SetD\":[{\"Seven\":7,\"Eight\":8}],\"SetE\":[{\"Nine\":9,\"Ten\":10}]}");
        }

        public async Task StartAsync()
        {
            try
            {
                _server.Start();
                Console.WriteLine("Server started. Waiting for connections...");

                while (true)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    var clientEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    Console.WriteLine($"Client connected from {clientEndPoint?.Address}:{clientEndPoint?.Port}");
                    _ = HandleClientAsync(client); // Handle multiple clients
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string encryptedRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Decrypt the request
                    string request = Decrypt(encryptedRequest);
                    Console.WriteLine($"Received request: {request}");

                    // Extract SetX and Key (e.g., from "SetAOne" get "SetA" and "One")
                    string set = "";
                    string key = "";
                    for (int i = 0; i < request.Length; i++)
                    {
                        if (i > 3 && char.IsUpper(request[i]))
                        {
                            set = request.Substring(0, i);
                            key = request.Substring(i);
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(set) || string.IsNullOrEmpty(key))
                    {
                        await SendEncryptedResponseAsync(stream, "EMPTY");
                        return;
                    }

                    Console.WriteLine($"Parsed request - Set: {set}, Key: {key}");

                    if (!_data.ContainsKey(set))
                    {
                        await SendEncryptedResponseAsync(stream, "EMPTY");
                        return;
                    }

                    var subset = _data[set];
                    int value = -1;

                    foreach (var dict in subset)
                    {
                        if (dict.ContainsKey(key))
                        {
                            value = dict[key];
                            break;
                        }
                    }

                    if (value == -1)
                    {
                        await SendEncryptedResponseAsync(stream, "EMPTY");
                        return;
                    }

                    // Send time 'value' number of times
                    for (int i = 0; i < value; i++)
                    {
                        string timeResponse = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                        await SendEncryptedResponseAsync(stream, timeResponse);
                        await Task.Delay(1000); // Wait 1 second between responses
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private async Task SendEncryptedResponseAsync(NetworkStream stream, string response)
        {
            string encryptedResponse = Encrypt(response);
            byte[] responseBytes = Encoding.UTF8.GetBytes(encryptedResponse);
            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            Console.WriteLine($"Sent response: {response}");
        }

        private string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        private string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}
