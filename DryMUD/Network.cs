using DryMUD;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Network
{
    class Connection
    {
        public const int receive_buffer_size = 4 * 1024;
        public const int send_buffer_size = 4 * 1024;

        public Socket socket = null;

        public byte[] receive_buffer_data = new byte[receive_buffer_size];
        public StringBuilder receive_buffer_string = new StringBuilder();

        public StringBuilder send_buffer_string = new StringBuilder();


        public long id = -1;
        public Session session = null;
    }


    class ConnectionHandler
    {   
        public const int connection_queue_size = 100;

        private static int listener_port;

        private static readonly object lock_object = new object();
        private static readonly Dictionary<Socket, Connection> socket_to_connection = new Dictionary<Socket, Connection>();
        private static Socket listener;

        private static long next_connection_id = 0;
        private static string greeting_message;
        
        public ConnectionHandler()
        {   
        }

        public static void Start(int port, string greeting_path)
        {
            greeting_message = System.IO.File.ReadAllText(greeting_path);

            listener_port = port;

            // @TODO: Test this with a non-local host
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
            lock (lock_object)
            {
                Socket socket = listener.EndAccept(async_result);

                // @TODO: Test this with a non-local host
                if (socket.RemoteEndPoint is IPEndPoint remote_end_point)
                {
                    Console.WriteLine($"Incoming connection ({next_connection_id}) from {remote_end_point.Address}.");
                }
                else
                {
                    Console.WriteLine("Incoming connection from [undetermined].");
                }

                Connection client = new Connection
                {
                    socket = socket,
                    id = next_connection_id++
                };
                client.session = new Session(client);
                

                socket_to_connection.Add(socket, client);

                Send(socket, greeting_message);

                try
                {
                    socket.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
                }
                catch (SocketException)
                {
                    Disconnect(socket);
                }

                listener.BeginAccept(new AsyncCallback(AcceptCallback), null);
            }
        }

        public static void ReadCallback(IAsyncResult async_result)
        {
            lock (lock_object)
            {
                Connection client = (Connection)async_result.AsyncState;

                try
                {
                    int bytes_read = client.socket.EndReceive(async_result);
                    if (bytes_read > 0)
                    {
                        client.receive_buffer_string.Append(Encoding.ASCII.GetString(client.receive_buffer_data, 0, bytes_read));

                        if (client.receive_buffer_string.Length >= Connection.receive_buffer_size)
                        {
                            Console.WriteLine($"Connection {client.id} flooding the input buffer.");
                            Disconnect(client.socket);
                        }
                    }
                    client.socket.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
                }
                catch (SocketException)
                {
                    Disconnect(client.socket);
                }
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

        public static void ProcessInputBuffers()
        {
            foreach (Connection client in socket_to_connection.Values)
            {
                string input_buffer_string = client.receive_buffer_string.ToString();
                int line_terminator_index = input_buffer_string.IndexOf('\n');
                if (line_terminator_index >= 0)
                {
                    string command = input_buffer_string.Substring(0, line_terminator_index);
                    command = command.Replace("\r", string.Empty);
                    command = command.Replace("\n", string.Empty);

                    string new_receive_buffer_string = input_buffer_string.Substring(line_terminator_index + 1);
                    client.receive_buffer_string = new StringBuilder(new_receive_buffer_string);

                    client.session.ProcessInput(command);
                }
            }
        }

        public static void ProcessOutputBuffers()
        {
            foreach (Connection client in socket_to_connection.Values)
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
            lock (lock_object)
            {
                Socket socket = (Socket)async_result.AsyncState;

                int bytes_sent = socket.EndSend(async_result);
                Console.WriteLine($"Sent {bytes_sent} bytes to client ({socket_to_connection[socket].id}).");
            }
        }

        public static void Disconnect(Socket socket)
        {
            Console.WriteLine($"Socket ({socket_to_connection[socket].id}) disconnected.");

            // @TODO: go through any game data lists and see if they are controlled by this connection
            socket_to_connection.Remove(socket);

            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }
    }
}
