using System;
using System.Collections.Generic;
using System.Text;

namespace DryMUD
{
    class NewPlayer
    {
        private string name;
        private long password_hash;
        
        enum State
        {
            NotSet,

            Name,
            Password,
            PasswordAgain
        }

        private State state = State.NotSet;

        public bool CreatingNewPlayer(string input)
        {
            switch (state)
            {
                case State.NotSet:
                    {
                        Log.Error("New player state has not been set.");
                    } break;

                case State.Name:
                    {
                    } break;

                case State.Password:
                    {
                    } break;

                case State.PasswordAgain:
                    {
                    } break;
            }
            return false;
        }
    }
}
