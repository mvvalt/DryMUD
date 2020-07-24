using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Network
{
    class Connection
    {
        public const int receive_buffer_size = 1024;
        public const int send_buffer_size = 4; // @DEV


        public Socket socket = null;

        public byte[] receive_buffer_data = new byte[receive_buffer_size];
        public StringBuilder receive_buffer_string = new StringBuilder();

        public StringBuilder send_buffer_string = new StringBuilder();
    }


    class ConnectionHandler
    {   
        public const int connection_queue_size = 100;

        private static int listener_port;

        private static readonly object lock_object = new object();
        private static Dictionary<Socket, Connection> socket_to_connection = new Dictionary<Socket, Connection>();
        private static Socket listener;
        
        public ConnectionHandler()
        {   
        }

        public static void Start(int port)
        {
            listener_port = port;

            IPHostEntry ip_host_info = Dns.GetHostEntry("localhost"); // Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip_address = ip_host_info.AddressList[0];
            IPEndPoint local_end_point = new IPEndPoint(ip_address, listener_port);

            listener = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(local_end_point);
            listener.Listen(connection_queue_size);
            listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        public static void AcceptCallback(IAsyncResult async_result)
        {
            Console.WriteLine("Incoming connection.");

            Socket socket = listener.EndAccept(async_result);

            Connection client = new Connection();
            client.socket = socket;

            lock (lock_object)
            {
                socket_to_connection.Add(socket, client);
            }

            try
            {
                socket.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
                // @DEV
                Send(socket, "This is a lot of text to test the metered output of FlushSendBuffers().");
            }
            catch(SocketException)
            {
                Disconnect(socket);
            }

            listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        public static void ReadCallback(IAsyncResult async_result)
        {
            string content = String.Empty;

            Connection client = (Connection)async_result.AsyncState;
            Socket socket = client.socket;

            try
            {
                int bytes_read = socket.EndReceive(async_result);
                if (bytes_read > 0)
                {
                    client.receive_buffer_string.Append(Encoding.ASCII.GetString(client.receive_buffer_data, 0, bytes_read));

                    // Check the input buffer for a line-terminated command
                    content = client.receive_buffer_string.ToString();
                    int line_terminator_index = content.IndexOf('\n');
                    if (line_terminator_index >= 0)
                    {
                        string command = content.Substring(0, line_terminator_index);
                        string new_receive_buffer_data = content.Substring(line_terminator_index + 1);
                        client.receive_buffer_string = new StringBuilder(new_receive_buffer_data);

                        Console.WriteLine("Command: {0}", command);
                        Send(socket, command); // @DEV
                    }

                    socket.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
                }
            }
            catch(SocketException)
            {
                Disconnect(socket);
            }
        }

        public static void Send(Socket socket, string data)
        {
            Connection client = socket_to_connection[socket];
            client.send_buffer_string.Append(data);

            if (client.send_buffer_string.Length > Connection.send_buffer_size)
            {
                // @TODO: Send buffer is getting too large, do something about it
            }
        }

        public static void FlushSendBuffers()
        {
            foreach(Connection client in socket_to_connection.Values)
            {
                if (client.send_buffer_string.Length > 0)
                {
                    int len = Connection.send_buffer_size;
                    if (client.send_buffer_string.Length < Connection.send_buffer_size)
                    {
                        len = client.send_buffer_string.Length;
                    }

                    byte[] byte_data = Encoding.ASCII.GetBytes(client.send_buffer_string.ToString(0, len));
                    client.send_buffer_string.Remove(0, len);

                    try
                    {
                        client.socket.BeginSend(byte_data, 0, byte_data.Length, 0, new AsyncCallback(SendCallback), client.socket);
                    }
                    catch (SocketException)
                    {
                        Disconnect(client.socket);
                    }
                }
            }
        }

        private static void SendCallback(IAsyncResult async_result)
        {
            Socket socket = (Socket)async_result.AsyncState;

            int bytes_sent = socket.EndSend(async_result);
            Console.WriteLine("Send {0} bytes to client.", bytes_sent);
        }

        public static void Disconnect(Socket socket)
        {
            // @TODO: go through any game data lists and see if they are controlled by this connection

            lock (lock_object)
            {
                socket_to_connection.Remove(socket);
            }

            Console.WriteLine("Socket disconnected.");

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
