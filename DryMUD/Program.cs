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
            const int port = 4000;
            const int tick_flush_network_buffers_ms = 3000;

            Console.WriteLine("Starting network handler.");
            Network.ConnectionHandler.Start(port);

            Timer timer_flush_network_buffers = new Timer(tick_flush_network_buffers_ms);

            while (running)
            {
                if (timer_flush_network_buffers.CheckTick())
                {
                    Network.ConnectionHandler.FlushSendBuffers();
                }
            }
        }
    }

}
