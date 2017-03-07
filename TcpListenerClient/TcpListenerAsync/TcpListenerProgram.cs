﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TcpListenerAsync
{
    class TcpListenerProgram
    {
        private static TcpListener listener;
        static byte[] bufferIn = new byte[30];
        static byte[] bufferOut = new byte[30];
        private static string str = string.Empty;
        private static Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();

        static void Main(string[] args)
        {
            listener = new TcpListener(GetIpAddress(), 11000);
            listener.Start();
            Console.WriteLine($"Server is running..");
            listener.BeginAcceptTcpClient(OnCompleteAcceptClientCallBack, listener);
            Console.Title = "Server - Running";
            
            while (true)
            {
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    Console.Write($"Server>> ");
                    var str = Console.ReadLine();
                    str += "\n";
                    var mess = $"Server>> {str}";
                    bufferOut = Encoding.UTF8.GetBytes(mess);
                    foreach (var client in clients)
                    {
                        client.Key.GetStream()
                            .BeginWrite(bufferOut, 0, bufferOut.Length, OnCompleteWriteClientCallBack, client);
                    }
                }
            }
        }

        public static IPAddress GetIpAddress()
        {
            var defaultIp = IPAddress.Parse("127.0.0.1");
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            return hostEntry.AddressList.Length <= 0
                ? defaultIp
                : (from address in hostEntry.AddressList
                    where address.AddressFamily == AddressFamily.InterNetwork
                    select address).FirstOrDefault();
        }

        static void OnCompleteAcceptClientCallBack(IAsyncResult iar)
        {
            try
            {
                TcpClient client = listener.EndAcceptTcpClient(iar);
                
                listener.BeginAcceptTcpClient(OnCompleteAcceptClientCallBack, listener);
                
                client.GetStream().BeginRead(bufferIn, 0, bufferIn.Length,
                    OnCompleteReadFromTcpClientStreamCallBack, client);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private static void OnCompleteReadFromTcpClientStreamCallBack(IAsyncResult ar)
        {
            TcpClient client = (TcpClient)ar.AsyncState;
            string strReceived = string.Empty;
            try
            {
                var byteSize = client.GetStream().EndRead(ar);
                strReceived = Encoding.UTF8.GetString(bufferIn, 0, byteSize);
                if (strReceived.IndexOf("\n") > -1)
                {
                    str += strReceived.Substring(0, strReceived.IndexOf("\n") + 1);
                    HandleReceivedMessage(client);
                }
                else
                    str += strReceived;

                client.GetStream().BeginRead(bufferIn, 0, bufferIn.Length,
                    OnCompleteReadFromTcpClientStreamCallBack, client);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{clients[client]} was disconnected");
                client.Close();
                clients.Remove(client);
                Console.WriteLine($"Clients connected ({clients.Count})");
                Console.ResetColor();
            }
        }

        private static void HandleReceivedMessage(TcpClient client)
        {
            string[] strArr = str.Split(';');

            if (clients.ContainsValue(strArr[1]))
            {
                SendPrivate(strArr[0], strArr[1], strArr[2]);
            }
            else if (strArr[0] == "NAME")
            {
                var clientName = strArr[1].Remove(strArr[1].Length -1, 1);
                clients.Add(client, clientName);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{clients.Last().Value} is connected");
                Console.ResetColor();
            }
            else if (strArr[1] == "SHOWALL")
            {
                var listClients = string.Empty;
                foreach (var c in clients)
                    listClients += c.Value + ", ";

                SendInfo("Connected Clients>> ", strArr[0], listClients);
            }
            else if (strArr[1] == "DATE")
            {
                var dateStr = DateTime.Now.Date.ToShortDateString();
                SendInfo("Server>> ", strArr[0], dateStr);
            }
            else if (strArr[1] == "TIME")
            {
                var dateStr = DateTime.Now.ToLongTimeString();
                SendInfo("Server>> ", strArr[0], dateStr);
            }
            else if (strArr[1] == "SETNAME")
            {
                var oldName = strArr[0].Remove(strArr[0].Length-2, 2);
                var newName = strArr[2].Remove(strArr[2].Length - 1, 1);

                clients[client] = newName;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"<{oldName}> changed name to <{newName}>");
                Console.ResetColor();
            }
            else
            {
                Broadcast(strArr[0], strArr[1]);
            }

            str= string.Empty;
        }

        private static void SendInfo(string sender, string receiver, string message)
        {
            var clientStr = receiver.Remove(receiver.Length - 2, 2);
            bufferOut = Encoding.UTF8.GetBytes(sender + " " + message + "\n");

            var client = (from c in clients where c.Value == clientStr select c).Single();
            client.Key.GetStream()
                      .BeginWrite(bufferOut, 0, bufferOut.Length, OnCompleteWriteClientCallBack, client.Key);

        }

        private static void Broadcast(string sender, string message)
        {
            var sendingClient = sender.Remove(sender.Length - 2, 2);
            
            bufferOut = Encoding.UTF8.GetBytes(sender + " " + message);

            foreach (KeyValuePair<TcpClient, string> client in clients)
            {
                if(client.Value != sendingClient)
                    client.Key.GetStream()
                      .BeginWrite(bufferOut, 0, bufferOut.Length, OnCompleteWriteClientCallBack, client.Key);
            }
        }
        static void SendPrivate(string sender, string receiver, string message)
        {
            var mess = sender + message;
            bufferOut = Encoding.UTF8.GetBytes(mess);
            
            var client = (from c in clients where c.Value == receiver select c).Single();
            client.Key.GetStream()
                      .BeginWrite(bufferOut, 0, bufferOut.Length, OnCompleteWriteClientCallBack, client.Key);
        }

        static void OnCompleteWriteClientCallBack(IAsyncResult iar)
        {
            var client = (TcpClient) iar.AsyncState;
            client.GetStream().EndWrite(iar);
        }
    }
}
