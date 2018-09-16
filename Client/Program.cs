using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    class Program
    {
        const int PORT_NO = 2201;
        static string SERVER_IP = string.Empty;
        static Socket clientSocket; //put here
        static string ipAddress = Dns.GetHostAddresses("")[3].ToString();

        static void Main(string[] args)
        {
            PeerListener.startListening();

            Console.Write("Type the Server IP: ");
            SERVER_IP = Console.ReadLine();


            //Similarly, start defining your client socket as soon as you start. 
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            loopConnect(3, 3); //for failure handling
            Console.Write("Success! You are connected with the server");
            string result = "";
            do
            {
                Console.Clear();
                Console.WriteLine("Type upload or get...");
                result = Console.ReadLine();

                switch(result)
                {
                    case "upload":
                        Console.Write("Type the amount of files to upload: ");
                        string amountInput = Console.ReadLine();
                        int amount = int.Parse(amountInput);
                        
                        for (int i = 1; i <= amount; i++)
                        {
                            Console.WriteLine("What is the path of " + i  + "° file?");
                            string pathInput = Console.ReadLine();
                            if (SendFile(pathInput)) {
                                Console.WriteLine("File " + pathInput + " sent with success!");
                            } 

                        }
                        break;
                    case "get":
                        Console.Write("What is the name of the file you want? ");
                        string fileNameInput = Console.ReadLine();
                        clientSocket.Send(stringToBytes("get:" + fileNameInput));
                        break;
                }


                if (result.ToLower().Trim() != "hadouken")
                {
                    clientSocket.Send(stringToBytes(result));
                }
            } while (result.ToLower().Trim() != "hadouken");
        }

        static byte[] stringToBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        static void loopConnect(int noOfRetry, int attemptPeriodInSeconds)
        {
            int attempts = 0;
            while (!clientSocket.Connected && attempts < noOfRetry)
            {
                try
                {
                    ++attempts;
                    IAsyncResult result = clientSocket.BeginConnect(IPAddress.Parse(SERVER_IP), PORT_NO, endConnectCallback, null);
                    result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(attemptPeriodInSeconds));
                    System.Threading.Thread.Sleep(attemptPeriodInSeconds * 1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.ToString());
                }
            }
            if (!clientSocket.Connected)
            {
                Console.WriteLine("Connection attempt is unsuccessful!");
                return;
            }
        }

        private const int BUFFER_SIZE = 4096;
        private static byte[] buffer = new byte[BUFFER_SIZE]; //buffer size is limited to BUFFER_SIZE per message
        private static void endConnectCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndConnect(ar);
                if (clientSocket.Connected)
                {
                    clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), clientSocket);
                }
                else
                {
                    Console.WriteLine("End of connection attempt, fail to connect...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("End-connection attempt is unsuccessful! " + e.ToString());
            }
        }

        const int MAX_RECEIVE_ATTEMPT = 10;
        static int receiveAttempt = 0;
        private static void receiveCallback(IAsyncResult result)
        {
            System.Net.Sockets.Socket socket = null;
            try
            {
                socket = (System.Net.Sockets.Socket)result.AsyncState;
                if (socket.Connected)
                {
                    int received = socket.EndReceive(result);
                    if (received > 0)
                    {
                        receiveAttempt = 0;
                        byte[] data = new byte[received];
                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);
                        string serverResponse = Encoding.UTF8.GetString(data);

                        if (serverResponse.Contains("FileFoundAt:"))
                        {
                            //@TODO Create a new socket to connect at port 2202 (PeerListener :D)
                        } else
                        {
                            Console.WriteLine("Server: " + Encoding.UTF8.GetString(data));
                        }
                        

                        

                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket);
                    }
                    else if (receiveAttempt < MAX_RECEIVE_ATTEMPT)
                    { //not exceeding the max attempt, try again
                        ++receiveAttempt;
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket);
                    }
                    else
                    { //completely fails!
                        Console.WriteLine("receiveCallback is failed!");
                        receiveAttempt = 0;
                        clientSocket.Close();
                    }
                }
            }
            catch (Exception e)
            { // this exception will happen when "this" is be disposed...
                Console.WriteLine("receiveCallback is failed! " + e.ToString());
            }
        }

        static private string CreateHash(byte[] fileBytes)
        {
            var a = SHA256.Create("joao");

            //string Hash = GenerateHash(fileName, ipClient)
            //Resource res = new Resource {FileName = ClientData }
            return a.ComputeHash(fileBytes).ToString();

        }

        static private Boolean SendFile(string fileName)
        {
            byte[] fileBytes = File.ReadAllBytes(fileName);
            clientSocket.Send(stringToBytes("upload:" + CreateHash(fileBytes) + ";" + fileName + ";" + ipAddress));
            return true;
        }
    }
}