using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Server
{
    class Program
    {
        const int PORT_NO = 2201;
        static string ipAddress = Dns.GetHostAddresses("")[3].ToString();

        static Socket serverSocket;
        static Dictionary<string, List<Resource>> resourcesMap = new Dictionary<string, List<Resource>>(); // key is the hash of file, the List<Resource>, is the list of devices that hosts that files...
        static List<string> clientsLogged = new List<string>();

        static void Main(string[] args)
        {
            Console.WriteLine("Listening on " + ipAddress);

            // Create a new socket
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Associates the socket with a defined end-point
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT_NO));

            // Max client numbers 
            serverSocket.Listen(4); //the maximum pending client, define as you wish

            // Define acceptCallback method as a callback to be called when received a client message
            serverSocket.BeginAccept(new AsyncCallback(acceptCallback), null);
            Console.WriteLine();
            string result = "";
            do
            {
                // Define the Ryu's hadouken to be a key to finish the server from client
                result = Console.ReadLine();

            } while (result.ToLower().Trim() != "hadouken");
        }

        private const int BUFFER_SIZE = 4096;
        private static byte[] buffer = new byte[BUFFER_SIZE];

        // Method that handle messages sent from clients
        private static void acceptCallback(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                socket = serverSocket.EndAccept(result); // The objectDisposedException will come here... thus, it is to be expected!
                //Do something as you see it needs on client acceptance
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket);
                serverSocket.BeginAccept(new AsyncCallback(acceptCallback), null); //to receive another client
            }
            catch (Exception e)
            { // this exception will happen when "this" is be disposed...        
                //Do something here             
                Console.WriteLine(e.ToString());
            }
        }

        private static void receiveCallback(IAsyncResult result)
        {
            Socket socket = null;
            string callbackMessage = String.Empty;
            try
            {
                socket = (Socket)result.AsyncState; //this is to get the sender
                if (socket.Connected)
                {
                    int received = socket.EndReceive(result);
                    if (received > 0)
                    {
                        byte[] data = new byte[received];
                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length);


                        string clientData = Encoding.UTF8.GetString(data);
                        string clientAddress = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
                        //@TODO[Refactor] split this area into various functions
                        switch (clientData)
                        {
                            case "login":
                                clientsLogged.Add(clientAddress);
                                socket.Send(Encoding.ASCII.GetBytes("success"));
                                break;
                            case var isUpload when isUpload.ToUpper().Contains("UPLOAD"):
                                callbackMessage = "Request to upload resource processed.";
                                string[] uploadInput = clientData.Split(';');

                                if (uploadInput.Length == 3)
                                {

                                    if (clientsLogged.Contains(clientAddress))
                                    {
                                        string hash = uploadInput[0].Split(':')[1];
                                        string fileName = uploadInput[1];
                                        AddResource(hash, fileName, clientAddress);

                                    }
                                    else
                                        socket.Send(Encoding.ASCII.GetBytes("failed to authenticate, before upload, you will need to login"));

                                }
                                else
                                    socket.Send(Encoding.ASCII.GetBytes("failed to upload"));

                                break;
                            case var isGet when isGet.ToUpper().Contains("GET"):
                                callbackMessage = "Request to get resource processed.";
                                string clientIp = String.Empty;
                                foreach (KeyValuePair<string, List<Resource>> resources in resourcesMap)
                                {
                                    foreach (Resource resource in resources.Value)
                                    {
                                        if (resource.FileName.Equals(clientData))
                                        {
                                            clientIp = resource.FromIp;
                                            break;
                                        }
                                    }
                                }

                                if (clientIp.Length > 0)
                                {
                                    socket.Send(Encoding.ASCII.GetBytes("FileAt:" + clientIp));
                                }
                                else
                                    socket.Send(Encoding.ASCII.GetBytes("Error 404 file not found"));

                                break;
                        }

                        Console.WriteLine(clientData);

                        socket.Send(Encoding.ASCII.GetBytes(callbackMessage));

                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket); //repeat beginReceive
                    }
                    else
                        // fails!
                        Console.WriteLine("receiveCallback fails!"); //don't repeat beginReceive

                }
            }
            catch (Exception e)
            { // this exception will happen when "this" is be disposed...
                Console.WriteLine("receiveCallback fails with exception! " + e.ToString());
            }
        }

        static private void AddResource(string Hash, string FileName, string ClientIp)
        {
            Resource r = new Resource();
            r.FileName = FileName;
            r.Hash = Hash;
            r.FromIp = ClientIp;

            if (resourcesMap.ContainsKey(Hash))
                resourcesMap[Hash].Add(r);
            else
            {
                List<Resource> newResourceList = new List<Resource>();
                newResourceList.Add(r);
                resourcesMap.Add(Hash, newResourceList);
            }

        }
    }
}