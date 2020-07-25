using System;
using System.Diagnostics;

namespace DryMUD
{
    class Timer
    {
        private readonly double tick_ms;
        private Stopwatch stopwatch = new Stopwatch();
        private double elapsed_us;

        public Timer(long tick_length_ms)
        {
            tick_ms = (double)tick_length_ms;
            elapsed_us = 0;
        }

        public bool CheckTick()
        {
            stopwatch.Stop();
            elapsed_us += ((double)stopwatch.ElapsedTicks / Stopwatch.Frequency) * 1000000.0;
            stopwatch.Restart();

            if (elapsed_us >= (tick_ms * 1000.0))
            {
                elapsed_us -= (tick_ms * 1000.0);
                return true;
            }

            return false;
        }
    }

    class Program
    {
        private static bool running = true;

        static void Main(string[] args)
        {
            // @TODO: Config options should be in a file
            const int port = 4000;
            const int tick_process_output_buffers_ms = 3000;
            const int tick_process_input_buffer_ms = 3000;
            const string data_directory = "../../../data/";

            Console.WriteLine("Starting network handler.");
            Network.ConnectionHandler.Start(port, $"{data_directory}greeting.txt");

            Timer timer_process_output_buffers = new Timer(tick_process_output_buffers_ms);
            Timer timer_process_input_buffers = new Timer(tick_process_input_buffer_ms);

            while (running)
            {
                if (timer_process_output_buffers.CheckTick())
                {
                    Network.ConnectionHandler.ProcessOutputBuffers();
                }
                if (timer_process_input_buffers.CheckTick())
                {
                    Network.ConnectionHandler.ProcessInputBuffers();
                }
            }
        }
    }

}
