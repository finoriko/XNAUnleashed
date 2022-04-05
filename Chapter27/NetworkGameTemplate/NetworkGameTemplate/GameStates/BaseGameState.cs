using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using XELibrary;

namespace NetworkGameTemplate
{
    public partial class BaseGameState : GameState
    {
        protected NetworkGameTemplate OurGame;
        protected ContentManager Content;

        public BaseGameState(Game game)
            : base(game)
        {
            Content = game.Content;
            OurGame = (NetworkGameTemplate)game;
        }
    }
}
