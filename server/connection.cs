//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;

//class ChatServer
//{
//    static void Main()
//    {
//        int port = 12345; // Port number
//        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

//        serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
//        serverSocket.Listen(5);

//        Console.WriteLine($"Chat server started on port {port}. Waiting for clients...");

//        while (true)
//        {
//            // Accept a client connection
//            Socket clientSocket = serverSocket.Accept();
//            Console.WriteLine("Client connected!");

//            // Handle client communication in a separate thread
//            Thread clientThread = new Thread(() => HandleClient(clientSocket));
//            clientThread.Start();
//        }
//    }

//    static void HandleClient(Socket clientSocket)
//    {
//        try
//        {
//            while (true)
//            {
//                // Receive message from the client
//                byte[] buffer = new byte[1024];
//                int bytesRead = clientSocket.Receive(buffer);

//                string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//                Console.WriteLine($"Client says: {clientMessage}");

//                // Echo the message back to the client
//                string serverResponse = $"Server: {clientMessage}";
//                clientSocket.Send(Encoding.UTF8.GetBytes(serverResponse));
//            }
//        }
//        catch
//        {
//            Console.WriteLine("Client disconnected.");
//        }
//        finally
//        {
//            clientSocket.Close();
//        }
//    }
//}
