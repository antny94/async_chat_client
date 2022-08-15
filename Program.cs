using System.Net;
using System.Net.Sockets;
using System.Text;

namespace async_chat_client
{
    internal class Program
    {
        private static Socket _clientSocket = new Socket(
          AddressFamily.InterNetwork,
          SocketType.Stream,
          ProtocolType.Tcp);

        private static string _hostIP = ""; // Enter server ip address here
        private static readonly int _hostPORT = 4949;
        private static string userName = "";
        private static Task? serverListener;

        public struct ClientMessage {
            public string type;
            public string message;

            //byte[] overload
            public static explicit operator byte[](ClientMessage v)
            {
                byte[] temp = new byte[1024];
                byte[] cmType = Encoding.ASCII.GetBytes(v.type);
                byte[] seperator = Encoding.ASCII.GetBytes("::");
                byte[] cmMessage = Encoding.ASCII.GetBytes(v.message);
                int tLength = cmType.Length;
                int sLength = seperator.Length;
                int messageIndexStart = tLength + sLength;
                cmType.CopyTo(temp, 0);
                seperator.CopyTo(temp, tLength);
                cmMessage.CopyTo(temp, messageIndexStart);
                return temp;
            }
        }
        static void Main(string[] args)
        {
            Console.Title = "Chat Client";
            StartClient();
        }

        static void StartClient()
        {
            GetUserName();          // Get user name of client
            ConnectToServer();      // Establish connection with the server first
            try
            {
                List<Task> listTask = new List<Task>();
                Task a = new Task(SendUserMessages);    // Get and send user messages
                Task b = new Task(ServerListener);      // Establish a receiver to receive information from the server
                listTask.Add(a);
                listTask.Add(b);
                Parallel.ForEach(listTask, individual_task => individual_task.Start());
                Task.WaitAll(listTask.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static async void SendUserMessages()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Send a message!");
                    string g = await GetUserMessage();

                    byte[] buffer = Encoding.ASCII.GetBytes(g);
                    _clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
            }
        }

        static async Task<string> GetUserMessage()
        {
            string userMsg = Console.ReadLine();
            string seperator = ": ";
            return (userName + seperator + userMsg);
        }

        static void GetUserName()
        {
            Console.WriteLine("Enter your username");
            userName = Console.ReadLine();
            //userName += ":";
        }

        private static void ConnectToServer()
        {
            int attempts = 0;

            while (!_clientSocket.Connected)
            {
                try
                {
                    attempts++;
                    // If remote host IP address is known, then replace IPAddress.Loopback with the remote host IP address

                    Console.WriteLine("Connecting...");
                    if (_hostIP.Length < 1)
                    {
                        _clientSocket.Connect(IPAddress.Loopback, _hostPORT);
                    }
                    else
                    {
                        _clientSocket.Connect(IPAddress.Parse(_hostIP), _hostPORT); //Connect to public ip address of server
                    }

                    //Send username to the server
                    SendNewUserMsg(userName);
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    Console.Clear();
                    Console.WriteLine("Connect attempt: " + attempts);
                }

            }

            Console.Clear();
            Console.WriteLine("Succesfully connected with host.");
        }

        private static void SendNewUserMsg(string msg)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            _clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void SendClientMessage(string msg)
        {
            ClientMessage cMsg = new ClientMessage();
            cMsg.type = "NUM";
            cMsg.message = msg;

            byte[] buffer = (byte[])cMsg;
            _clientSocket.Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        private static void ServerListener()
        {
            // Establishes a constantly-running receiver to get information from server
            serverListener = new Task( () => {
                while (true)
                {
                    try
                    {
                        byte[] receiver = new byte[1024];
                        int b = _clientSocket.Receive(receiver);
                        if (receiver[0] != 0)
                        {
                            string client_msg = Encoding.ASCII.GetString(receiver);
                            Console.WriteLine(client_msg);
                        }
                    }

                    catch (SocketException e)
                    {
                        Console.WriteLine("Host has ended the connection.");

                        //Exits console application
                        //Environment.Exit(0);

                        break;
                    }
                }

                //If we reach here, then host connection has ended
                Environment.Exit(0);
            });

            // Start server listener task
            serverListener.Start();
        }
    }
}