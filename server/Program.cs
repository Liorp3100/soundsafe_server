using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace server
{
    class FileReceiverServer
    {
        static void Main()
        {
            mysql.Sql();
            /*
           int port = 5000;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(10);

            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                Socket clientSocket = serverSocket.Accept();
                Thread clientThread = new Thread(() => HandleClient(clientSocket));
                clientThread.Start();
            }
        }

        static void HandleClient(Socket clientSocket)
        {
            try
            {
                Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint}");

                // Generate RSA key pair
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
                RSAParameters privateKey = rsa.ExportParameters(true);
                string publicKeyXml = rsa.ToXmlString(false);
                byte[] publicKeyBytes = Encoding.UTF8.GetBytes(publicKeyXml);
                byte[] publicKeyLength = BitConverter.GetBytes(publicKeyBytes.Length);

                // Send public key to client
                clientSocket.Send(publicKeyLength);
                clientSocket.Send(publicKeyBytes);

                // Receive encrypted AES key + IV
                byte[] aesMetaLengthBytes = new byte[4];
                clientSocket.Receive(aesMetaLengthBytes);
                int aesMetaLength = BitConverter.ToInt32(aesMetaLengthBytes, 0);
                byte[] encryptedAesMeta = new byte[aesMetaLength];
                clientSocket.Receive(encryptedAesMeta);

                byte[] decryptedAesMeta = RsaDecrypt(encryptedAesMeta, privateKey);
                byte[] aesKey = decryptedAesMeta[..32];
                byte[] aesIV = decryptedAesMeta[32..48];

                // Receive file name
                byte[] fileNameLengthBytes = new byte[4];
                clientSocket.Receive(fileNameLengthBytes);
                int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);
                byte[] fileNameBytes = new byte[fileNameLength];
                clientSocket.Receive(fileNameBytes);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);

                // Receive encrypted file bytes
                using (MemoryStream receivedStream = new MemoryStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = clientSocket.Receive(buffer)) > 0)
                    {
                        receivedStream.Write(buffer, 0, bytesRead);
                        if (bytesRead < buffer.Length) break;
                    }

                    byte[] encryptedFileBytes = receivedStream.ToArray();
                    byte[] decryptedFileBytes = DecryptFileBytes(encryptedFileBytes, aesKey, aesIV);

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string outputFilePath = Path.Combine(desktopPath, fileName);
                    File.WriteAllBytes(outputFilePath, decryptedFileBytes);

                    Console.WriteLine($"File saved: {outputFilePath}");
                }

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        static byte[] RsaDecrypt(byte[] data, RSAParameters privateKey)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(privateKey);
                return rsa.Decrypt(data, false);
            }
        }

        static byte[] DecryptFileBytes(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                using (MemoryStream ms = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(encryptedBytes, 0, encryptedBytes.Length);
                    cs.FlushFinalBlock();
                    return ms.ToArray();
                }
            }
       */ }
    }
            
}
