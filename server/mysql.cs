using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace server
{
    class mysql
    {
        static Dictionary<string, Socket> connectedClients = new Dictionary<string, Socket>();

        public static void Sql()
        {
            int port = 5000;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);

            Console.WriteLine("Server is running...");

            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }

        static void HandleClient(Socket clientSocket)
        {
            byte[] buffer = new byte[1024];
            string username = null;

            try
            {
                while (true)
                {
                    int read = clientSocket.Receive(buffer);
                    if (read == 0) break;

                    string received = Encoding.UTF8.GetString(buffer, 0, read);
                    string[] parts = received.Split('|');

                    if (parts.Length < 2)
                    {
                        SendResponse(clientSocket, "Invalid format");
                        continue;
                    }

                    string action = parts[0];

                    if (action == "WAV_FILE")
                    {
                        HandleWavFile(clientSocket, username);
                        continue;
                    }

                    if (parts.Length != 3)
                    {
                        SendResponse(clientSocket, "Invalid format. Expected: ACTION|username|password");
                        continue;
                    }

                    string user = parts[1];
                    string password = parts[2];

                    if (action == "REGISTER")
                    {
                        if (UserExists(user))
                        {
                            SendResponse(clientSocket, "Username already exists");
                        }
                        else
                        {
                            string serialKey = GenerateSerialKey();
                            string hashedPassword = ComputeSha256Hash(password);
                            InsertToDatabase(user, hashedPassword, serialKey);
                            SendResponse(clientSocket, serialKey);

                            username = user;
                            lock (connectedClients)
                            {
                                if (!connectedClients.ContainsKey(username))
                                    connectedClients.Add(username, clientSocket);
                            }
                        }
                    }
                    else if (action == "LOGIN")
                    {
                        if (!UserExists(user))
                        {
                            SendResponse(clientSocket, "User not found");
                        }
                        else
                        {
                            string hashedPassword = ComputeSha256Hash(password);
                            string serialKey = GetSerialKeyIfPasswordMatches(user, hashedPassword);
                            if (serialKey != null)
                            {
                                SendResponse(clientSocket, serialKey);

                                username = user;
                                lock (connectedClients)
                                {
                                    if (!connectedClients.ContainsKey(username))
                                        connectedClients.Add(username, clientSocket);
                                }
                            }
                            else
                            {
                                SendResponse(clientSocket, "Wrong password");
                            }
                        }
                    }
                    else if (action == "NOTIFY")
                    {
                        string targetUser = password;

                        lock (connectedClients)
                        {
                            if (connectedClients.ContainsKey(targetUser))
                            {
                                Socket targetSocket = connectedClients[targetUser];
                                SendResponse(targetSocket, $"User {user} wants to talk to you.");
                            }
                            else
                            {
                                SendResponse(clientSocket, "User not online.");
                            }
                        }
                    }
                    else if (action == "ACCEPT")
                    {
                        string fromUser = user;        // המשתמש שאישר
                        string toUser = password;      // המשתמש ששלח את הבקשה

                        lock (connectedClients)
                        {
                            if (connectedClients.TryGetValue(toUser, out Socket toSocket))
                            {
                                SendResponse(toSocket, $"ACCEPTED|{fromUser}");
                            }

                            if (connectedClients.TryGetValue(fromUser, out Socket fromSocket))
                            {
                                SendResponse(fromSocket, $"ACCEPTED|{toUser}");
                            }
                        }
                    }
                    else
                    {
                        SendResponse(clientSocket, "Unknown action");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client: {ex.Message}");
            }
            finally
            {
                if (username != null)
                {
                    lock (connectedClients)
                    {
                        connectedClients.Remove(username);
                    }
                }

                try { clientSocket.Shutdown(SocketShutdown.Both); } catch { }
                clientSocket.Close();
            }
        }

        static void HandleWavFile(Socket senderSocket, string senderUsername)
        {
            try
            {
                // First, receive the file size (4 bytes)
                byte[] sizeBuffer = new byte[4];
                int totalReceived = 0;
                while (totalReceived < 4)
                {
                    int received = senderSocket.Receive(sizeBuffer, totalReceived, 4 - totalReceived, SocketFlags.None);
                    if (received == 0) return;
                    totalReceived += received;
                }

                int fileSize = BitConverter.ToInt32(sizeBuffer, 0);
                Console.WriteLine($"Receiving WAV file of size: {fileSize} bytes");

                // Receive the actual WAV file data
                byte[] wavData = new byte[fileSize];
                totalReceived = 0;
                while (totalReceived < fileSize)
                {
                    int received = senderSocket.Receive(wavData, totalReceived, fileSize - totalReceived, SocketFlags.None);
                    if (received == 0) return;
                    totalReceived += received;
                }

                Console.WriteLine($"Received complete WAV file from {senderUsername}");

                // Generate random WAV data with the same size
                byte[] randomWavData = GenerateRandomWavData(fileSize);

                // Send the random WAV file to all other connected clients
                lock (connectedClients)
                {
                    foreach (var client in connectedClients)
                    {
                        if (client.Key != senderUsername && client.Value != senderSocket)
                        {
                            try
                            {
                                // Send "WAV_INCOMING" notification first
                                SendResponse(client.Value, "WAV_INCOMING");
                                Thread.Sleep(100); // Small delay to ensure message is processed

                                // Send file size first
                                byte[] sizeBytesToSend = BitConverter.GetBytes(fileSize);
                                client.Value.Send(sizeBytesToSend);

                                // Send the random WAV data
                                client.Value.Send(randomWavData);
                                Console.WriteLine($"Sent random WAV file to {client.Key}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send WAV to {client.Key}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling WAV file: {ex.Message}");
            }
        }

        static byte[] GenerateRandomWavData(int size)
        {
            Random random = new Random();
            byte[] randomData = new byte[size];

            // Generate basic WAV header if size is large enough (44 bytes minimum for WAV header)
            if (size >= 44)
            {
                // WAV file header structure
                string riff = "RIFF";
                string wave = "WAVE";
                string fmt = "fmt ";
                string data = "data";

                int fileSize = size - 8;
                short audioFormat = 1; // PCM
                short numChannels = 1; // Mono
                int sampleRate = 44100;
                int byteRate = sampleRate * numChannels * 2; // 16-bit
                short blockAlign = (short)(numChannels * 2);
                short bitsPerSample = 16;
                int dataSize = size - 44;

                int pos = 0;

                // RIFF header
                Array.Copy(Encoding.ASCII.GetBytes(riff), 0, randomData, pos, 4); pos += 4;
                Array.Copy(BitConverter.GetBytes(fileSize), 0, randomData, pos, 4); pos += 4;
                Array.Copy(Encoding.ASCII.GetBytes(wave), 0, randomData, pos, 4); pos += 4;

                // fmt chunk
                Array.Copy(Encoding.ASCII.GetBytes(fmt), 0, randomData, pos, 4); pos += 4;
                Array.Copy(BitConverter.GetBytes(16), 0, randomData, pos, 4); pos += 4; // fmt chunk size
                Array.Copy(BitConverter.GetBytes(audioFormat), 0, randomData, pos, 2); pos += 2;
                Array.Copy(BitConverter.GetBytes(numChannels), 0, randomData, pos, 2); pos += 2;
                Array.Copy(BitConverter.GetBytes(sampleRate), 0, randomData, pos, 4); pos += 4;
                Array.Copy(BitConverter.GetBytes(byteRate), 0, randomData, pos, 4); pos += 4;
                Array.Copy(BitConverter.GetBytes(blockAlign), 0, randomData, pos, 2); pos += 2;
                Array.Copy(BitConverter.GetBytes(bitsPerSample), 0, randomData, pos, 2); pos += 2;

                // data chunk header
                Array.Copy(Encoding.ASCII.GetBytes(data), 0, randomData, pos, 4); pos += 4;
                Array.Copy(BitConverter.GetBytes(dataSize), 0, randomData, pos, 4); pos += 4;

                // Fill the rest with random audio data
                byte[] audioData = new byte[dataSize];
                random.NextBytes(audioData);
                Array.Copy(audioData, 0, randomData, pos, dataSize);
            }
            else
            {
                // If size is too small for a proper WAV, just fill with random data
                random.NextBytes(randomData);
            }

            return randomData;
        }

        static void SendResponse(Socket socket, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            socket.Send(data);
        }


        static void InsertToDatabase(string username, string hashedPassword, string serialKey)
        {
            string connStr = "server=localhost;user=root;database=soundsafe;password=Gr7vXe9pLm2q;";
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "INSERT INTO users (username, password, SerialKey) VALUES (@Username, @Password, @SerialKey)";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", hashedPassword);
                cmd.Parameters.AddWithValue("@SerialKey", serialKey);
                cmd.ExecuteNonQuery();
            }
        }


        static string GetSerialKeyIfPasswordMatches(string username, string hashedPassword)
        {
            string connStr = "server=localhost;user=root;database=soundsafe;password=Gr7vXe9pLm2q;";
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT SerialKey FROM users WHERE username = @Username AND password = @Password";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", hashedPassword);

                object result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }

        static bool UserExists(string username)
        {
            string connStr = "server=localhost;user=root;database=soundsafe;password=Gr7vXe9pLm2q;";
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM users WHERE username = @Username";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);

                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        static string GenerateSerialKey()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            StringBuilder sb = new StringBuilder(16);
            for (int i = 0; i < 16; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            return sb.ToString();
        }

        static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}