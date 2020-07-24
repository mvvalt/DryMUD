using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Network
{
    class Connection
    {
        public const int receive_buffer_size = 1024;
        public const int send_buffer_size = 3 * 1024;


        public Socket socket = null;

        public byte[] receive_buffer_data = new byte[receive_buffer_size];
        public StringBuilder receive_buffer_string = new StringBuilder();

        public byte[] send_buffer_data = new byte[send_buffer_size];
        public StringBuilder send_buffer_string = new StringBuilder();
    }


    class ConnectionHandler
    {
        public const int listener_port = 4000;
        public const int connection_queue_size = 100;
        public static ManualResetEvent manual_reset_event = new ManualResetEvent(false);

        public ConnectionHandler()
        {
        }

        public static void Listen()
        {
            IPHostEntry ip_host_info = Dns.GetHostEntry("localhost"); // Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ip_address = ip_host_info.AddressList[0];
            IPEndPoint local_end_point = new IPEndPoint(ip_address, listener_port);

            Socket listener = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


            listener.Bind(local_end_point);
            listener.Listen(connection_queue_size);

            while (true)
            {
                manual_reset_event.Reset();

                Console.WriteLine("Waiting for connection on port {0}...", listener_port);
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                manual_reset_event.WaitOne();
            }
        }

        public static void AcceptCallback(IAsyncResult async_result)
        {
            manual_reset_event.Set();

            Console.WriteLine("Incoming connection.");

            Socket listener = (Socket)async_result.AsyncState;
            Socket handler = listener.EndAccept(async_result);

            Connection client = new Connection();
            client.socket = handler;
            handler.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
        }

        public static void ReadCallback(IAsyncResult async_result)
        {
            string content = String.Empty;

            Connection client = (Connection)async_result.AsyncState;
            Socket handler = client.socket;

            int bytes_read = handler.EndReceive(async_result);
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
                    Send(handler, command);
                }

                handler.BeginReceive(client.receive_buffer_data, 0, Connection.receive_buffer_size, 0, new AsyncCallback(ReadCallback), client);
            }
        }

        private static void Send(Socket handler, String data)
        {
            byte[] byte_data = Encoding.ASCII.GetBytes(data);
            handler.BeginSend(byte_data, 0, byte_data.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult async_result)
        {
            Socket handler = (Socket)async_result.AsyncState;

            int bytes_sent = handler.EndSend(async_result);
            Console.WriteLine("Send {0} bytes to client.", bytes_sent);
        }
    }
}
