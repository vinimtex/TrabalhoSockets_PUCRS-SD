﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Server
{
    class Program
    {
        const int PORT_NO = 2201;
        static string ipAddress = Dns.GetHostAddresses("")[3].ToString();
        static Socket serverSocket;
        static Dictionary<string, List<Resource>> clientMap = new Dictionary<string, List<Resource>(); 

        static void Main(string[] args)
        {
            Console.WriteLine("Listening on "+ ipAddress);

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
        private static byte[] buffer = new byte[BUFFER_SIZE]; //buffer size is limited to BUFFER_SIZE per message

        // Method that handle messages sent from clients
        private static void acceptCallback(IAsyncResult result)
        { 
            //if the buffer is old, then there might already be something there...
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

        const int MAX_RECEIVE_ATTEMPT = 10;
        static int receiveAttempt = 0; //this is not fool proof, obviously, since actually you must have multiple of this for multiple clients, but for the sake of simplicity I put this
        private static void receiveCallback(IAsyncResult result)
        {
            Socket socket = null;
            try
            {
                socket = (Socket)result.AsyncState; //this is to get the sender
                if (socket.Connected)
                { //simple checking
                    int received = socket.EndReceive(result);
                    if (received > 0)
                    {
                        byte[] data = new byte[received]; 
                        Buffer.BlockCopy(buffer, 0, data, 0, data.Length); //There are several way to do this according to https://stackoverflow.com/questions/5099604/any-faster-way-of-copying-arrays-in-c in general, System.Buffer.memcpyimpl is the fastest
                                                                           

                        string clientData = Encoding.UTF8.GetString(data);
                        string clientAddress = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();

                        AddResource(clientAddress, clientData);

                        Console.WriteLine(clientData);


                        string msg = "Tafarel";
                        socket.Send(Encoding.ASCII.GetBytes(msg));  

                        receiveAttempt = 0; //reset receive attempt
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket); //repeat beginReceive
                    }
                    else if (receiveAttempt < MAX_RECEIVE_ATTEMPT)
                    { //fail but not exceeding max attempt, repeats
                        ++receiveAttempt; //increase receive attempt;
                        socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), socket); //repeat beginReceive
                    }
                    else
                    { //completely fails!
                        Console.WriteLine("receiveCallback fails!"); //don't repeat beginReceive
                        receiveAttempt = 0; //reset this for the next connection
                    }
                }
            }
            catch (Exception e)
            { // this exception will happen when "this" is be disposed...
                Console.WriteLine("receiveCallback fails with exception! " + e.ToString());
            }
        }

        private void AddResource(string ClientAddress, string ClientData)
        {
            var a = SHA256.Create("joao");
            //string Hash = GenerateHash(fileName, ipClient)
            //Resource res = new Resource {FileName = ClientData }
           

        }

        //private string GenerateHash(string fileName, string ipAddress)
        //{
            
        //}

    }
}