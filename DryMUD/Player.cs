using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace DryMUD
{
    class PlayerData
    {
        public string name { get; set; }

        public string password { get; set; }

        public int in_room_id { get; set; }
    }


    class Player
    {
        public Session session = null;
        public PlayerData player_data;


        public void Load(string character_name, Session session_handle)
        {
            session = session_handle;

            Console.WriteLine($"Loading player {character_name}.");

            try
            {
                string json_string = File.ReadAllText($"{Config.data_directory}players/{character_name}.txt");
                player_data = JsonSerializer.Deserialize<PlayerData>(json_string);
                session.Send("Loaded!\n");
            }
            catch (Exception)
            {
                Log.Error($"Unable to load character {character_name}.");
                session_handle.Disconnect("Unable to load character.");
            }

            Console.WriteLine($"Player {character_name} loaded!");
        }

        public void Save()
        {
            try
            {
                JsonSerializerOptions jso = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json_string = JsonSerializer.Serialize(player_data);
                File.WriteAllText($"{Config.data_directory}players/{player_data.name}.txt", json_string);
            }
            catch (Exception)
            {
                Log.Error($"Unable to save character {player_data.name}.");
            }
        }
    }
}
