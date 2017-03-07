using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientAsync
{
    class TcpClientProgram
    {
        private static TcpClient client;
        private static byte[] bufferIn = new byte[30];
        private static byte[] bufferOut = new byte[30];
        private static string str = string.Empty;
        static string ClientName = String.Empty;

        static void Main(string[] args)
        {
            

            client = new TcpClient();
            client.BeginConnect(GetMyIp(), 11000, OnCompleteConnectCallBack, client);

            #region Loop for writing
            while (true)
            {
                if (ClientName == string.Empty)
                {
                    do
                    {
                        Console.Write($"Enter your chatname: ");
                        ClientName = Console.ReadLine();

                    } while (ClientName == "");
                    Console.Title = ClientName.ToUpper() + " - Connected";
                    bufferOut = Encoding.UTF8.GetBytes("NAME;" + ClientName + "\n");
                    client.GetStream().BeginWrite(bufferOut, 0, bufferOut.Length,
                        OnCompleteWriteCallBack, client);
                }

                if (Console.ReadKey().Key == ConsoleKey.Enter)
                {
                    Console.Write($"{ClientName}>> ");
                    var text = Console.ReadLine();
                    var sender = $"{ClientName}>>;";
                    var eof = "\n";
                    var mess = sender + text + eof;
                    if(text.Contains("SETNAME;"))
                    {
                        ClientName = text.Remove(0, 8);
                        Console.Title = text.Remove(0, 8) + " - Connected";
                    }
                    else if (text.Contains("CLEAR;"))
                        Console.Clear();
                    else if (text.Contains("EXIT;"))
                        break;
                    bufferOut = Encoding.UTF8.GetBytes(mess);
                    client.GetStream().BeginWrite(bufferOut, 0, bufferOut.Length, OnCompleteWriteCallBack, client);
                }
            }
            #endregion
        }

        private static void OnCompleteWriteCallBack(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient) ar.AsyncState;
            tcpClient.GetStream().EndWrite(ar);
        }

        private static string GetMyIp()
        {
            var defaultIP = ("127.0.0.1");
            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            return hostEntry.AddressList.Length <= 0
                ? defaultIP
                : (from address in hostEntry.AddressList
                    where address.AddressFamily == AddressFamily.InterNetwork
                    select address).SingleOrDefault().ToString();
        }

        private static void OnCompleteConnectCallBack(IAsyncResult ar)
        {
            try
            {
                TcpClient tcpClient = (TcpClient) ar.AsyncState;
                tcpClient.EndConnect(ar);
                //bufferOut = Encoding.UTF8.GetBytes("NAME;"+ClientName);
                //client.GetStream().BeginWrite(bufferOut,0,bufferOut.Length,
                //    OnCompleteWriteCallBack,client);

                tcpClient.GetStream().BeginRead(bufferIn, 0, bufferIn.Length,
                    OnCompleteReadCallBack, tcpClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }

        private static void OnCompleteReadCallBack(IAsyncResult ar)
        {
            TcpClient tcpClient = (TcpClient) ar.AsyncState;
            string strRec = string.Empty;
            try
            {
                var readBytes = tcpClient.GetStream().EndRead(ar);
                strRec = Encoding.UTF8.GetString(bufferIn,0,readBytes);
                if (strRec.IndexOf("\n") >= 0)
                {
                    if (ClientName == "")
                    {
                        ClientName = strRec.Substring(0,strRec.IndexOf("\n"));
                        Console.Title = ClientName + " - Connected";
                    }
                    else
                    {
                        str += strRec.Substring(0, strRec.IndexOf("\n") + 1);
                        Console.WriteLine($"{str}");
                    }
                    
                    str = "";
                }
                else
                    str += strRec;

                tcpClient.GetStream().BeginRead(bufferIn, 0, bufferIn.Length,
                    OnCompleteReadCallBack, tcpClient);
            }
            catch 
            {
                Console.WriteLine($"The server was disconnected");
            }
        }
    }
}
