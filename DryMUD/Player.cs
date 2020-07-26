using System;
using System.IO;
using System.Text.Json;

namespace DryMUD
{
    class PlayerSaveData
    {
        public string Name { get; set; }

        public string Password { get; set; }

        public int InRoom { get; set; }
    }


    class Player
    {
        public Session session = null;
        public PlayerSaveData save_data = new PlayerSaveData();


        public bool Load(string character_name, Session session_handle)
        {
            session = session_handle;

            Console.WriteLine($"Loading player {character_name}.");

            try
            {
                string json_string = File.ReadAllText($"{Config.data_directory}players/{character_name}.txt");
                save_data = JsonSerializer.Deserialize<PlayerSaveData>(json_string);

                session.Send("Welcome back!\n");
            }
            catch (Exception)
            {
                Log.Error($"Unable to load character {character_name}.");
                return false;
            }

            Console.WriteLine($"Player {character_name} loaded!");
            return true;
        }

        public void Save()
        {
            try
            {
                JsonSerializerOptions jso = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json_string = JsonSerializer.Serialize(save_data, jso);
                File.WriteAllText($"{Config.data_directory}players/{save_data.Name}.txt", json_string);
            }
            catch (Exception)
            {
                Log.Error($"Unable to save character {save_data.Name}.");
            }
        }
    }
}
