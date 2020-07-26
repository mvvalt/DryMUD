using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace DryMUD
{
    class Config
    {
        public const int port = 4000;
        public const string data_directory = "../../../data/";

        public const int tick_process_output_buffers_ms = 16;
        public const int tick_process_input_buffer_ms = 16;

        public static List<string> player_name_censor = new List<string>();

        public static int new_player_start_room = 42;

        public static void Load()
        {
            StreamReader file = new StreamReader($"{data_directory}player_name_censor.txt");
            string name;
            while ((name = file.ReadLine()) != null)
            {
                player_name_censor.Add(name);
            }
            file.Close();
        }
    }
}
