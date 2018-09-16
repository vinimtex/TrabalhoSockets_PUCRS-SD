using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client
{
    class PeerListener
    {
        const int PORT_NO = 2202;
        static string SERVER_IP = string.Empty;
        static Socket serverSocket; //put here
        static string ipAddress = Dns.GetHostAddresses("")[3].ToString();

        static public void startListening()
        {
            Console.WriteLine("Listening others peers on " + ipAddress);

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT_NO));

            serverSocket.Listen(4);

            serverSocket.BeginAccept(new AsyncCallback(acceptCallback), null);
            Console.WriteLine();
            string result = "";
            do
            {

                result = Console.ReadLine();

            } while (result.ToLower().Trim() != "hadouken");
        }

        private const int BUFFER_SIZE = 4096;
        private static byte[] buffer = new byte[BUFFER_SIZE];

        private static void acceptCallback(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                socket = serverSocket.EndAccept(result); 

                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket);
                serverSocket.BeginAccept(new AsyncCallback(acceptCallback), null); //to receive another client
            }
            catch (Exception e)
            {     
                Console.WriteLine(e.ToString());
            }
        }

        const int MAX_RECEIVE_ATTEMPT = 10;
        static int receiveAttempt = 0;
        private static void receiveCallback(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                socket = (Socket)result.AsyncState; 
                if (socket.Connected)
                {
                    int received = socket.EndReceive(result);
                    if (received > 0)
                    {
                        byte[] data = new byte[received];
                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);

                        string clientAddress = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
                        string clientData = Encoding.UTF8.GetString(data);

                        if(File.Exists(clientData))
                        {
                            //@TODO check if the hash is valid befor send the file  
                            socket.SendFile(clientData);
                        } else
                        {
                            socket.Send(Encoding.ASCII.GetBytes("Error 404 file not found."));
                        }

                        receiveAttempt = 0; //reset receive attempt
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket); //repeat beginReceive
                    }
                    else if (receiveAttempt < MAX_RECEIVE_ATTEMPT)
                    { 
                        ++receiveAttempt;
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket); //repeat beginReceive
                    }
                    else
                    {
                        Console.WriteLine("receiveCallback fails!"); //
                        receiveAttempt = 0;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("receiveCallback fails with exception! " + e.ToString());
            }
        }
    }
}
