using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DryMUD
{
    class Config
    {
        public const int port = 4000;
        public const string data_directory = "../../../data/";

        public const int tick_process_output_buffers_ms = 3000;
        public const int tick_process_input_buffer_ms = 3000;

        public static void Load()
        {
            // @TODO
        }
    }
}
