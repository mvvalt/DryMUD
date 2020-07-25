using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DryMUD
{
    class Log
    {
        public static void Error(string msg)
        {
            Console.WriteLine($"Error: {msg}");
        }

        public static void Warning(string msg)
        {
            Console.WriteLine($"Warning: {msg}");
        }
    }
}
