using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DryMUD
{   
    class Session
    {
        private enum State
        {
            // Login
            CharacterName,
            Password,

            // New
            New_CharacterName,
            New_CharacterPassword,
            New_CharacterPasswordAgain,

            // Playing
        }
        private State state = State.CharacterName;

        private string character_name;
        private string password_hash;

        private readonly Network.Connection network_connection = null;


        // https://stackoverflow.com/questions/4181198/how-to-hash-a-password/10402129#10402129
        private const int password_hash_iterations = 100000;

        private string CreatePasswordHash(string password)
        {   
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, password_hash_iterations);
            byte[] hash = pbkdf2.GetBytes(20);

            byte[] hash_bytes = new byte[36];
            Array.Copy(salt, 0, hash_bytes, 0, 16);
            Array.Copy(hash, 0, hash_bytes, 16, 20);

            return Convert.ToBase64String(hash_bytes);
        }

        private bool VerifyPasswordHash(string saved_password_hash, string password)
        {
            byte[] hash_bytes = Convert.FromBase64String(saved_password_hash);
            byte[] salt = new byte[16];
            Array.Copy(hash_bytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, password_hash_iterations);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if (hash_bytes[i + 16] != hash[i])
                {   
                    return false;
                }
            }

            return true;
        }


        public Session(Network.Connection connection)
        {
            network_connection = connection;
        }

        public void Send(string message)
        {
            Network.ConnectionHandler.Send(network_connection.socket, message);
        }

        public void ProcessInput(string input)
        {
            switch (state)
            {
                case State.CharacterName:
                    {
                        const string enter_name = "Enter your character's name, or [new] to create one: ";

                        if (input.ToLower() == "new")
                        {
                            state = State.New_CharacterName;
                            Send("Enter a name for your character: ");
                        }
                        else if (input.Trim() == "")
                        {
                            Send(enter_name);
                        }
                        else
                        {
                            if (!input.All(char.IsLetter))
                            {
                                Send($"Character names must contain only letters. {enter_name}");
                                return;
                            }

                            if (!File.Exists($"{Config.data_directory}players/{input}.plr"))
                            {
                                Send($"No character by that name exists. {enter_name}");
                                return;
                            }

                            character_name = input;
                            state = State.Password;
                            Send("Enter your password: ");
                        }
                    } break;

                case State.Password:
                    {
                        StreamReader file = new StreamReader($"{Config.data_directory}players/{character_name}.plr");
                        string saved_password_hash = file.ReadLine();
                        file.Close();

                        if (!VerifyPasswordHash(saved_password_hash, input))
                        {
                            Send("The password does not match. Enter your password: ");
                            return;
                        }

                        // @TODO: passwords match, load the character

                    } break;


                case State.New_CharacterName:
                    {
                        if (File.Exists($"{Config.data_directory}players/{input}.plr"))
                        {
                            Send("That name has already been chosen. Enter a name for your character: ");
                            return;
                        }

                        if (Config.character_name_censor.Contains(input))
                        {
                            Send("That name is not allowed. Enter a name for your character: ");
                            return;
                        }

                        character_name = input;
                        state = State.New_CharacterPassword;
                        Send("Enter a password for your character: ");
                    } break;

                case State.New_CharacterPassword:
                    {
                        password_hash = CreatePasswordHash(input);
                        state = State.New_CharacterPasswordAgain;
                        Send("Enter your password again: ");
                    } break;

                case State.New_CharacterPasswordAgain:
                    {
                        if (!VerifyPasswordHash(password_hash, input))
                        {   
                            state = State.New_CharacterPassword;
                            Send("The password does not match. ");
                            Send("Enter a password for your character: ");
                        }

                        // @TODO: more character loading stuff
                    } break;
            }
        }
    }
}
