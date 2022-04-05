using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using XELibrary;

namespace Concentration
{
    public partial class BaseGameState : GameState
    {
        protected Concentration OurGame;
        protected ContentManager Content;

        public BaseGameState(Game game)
            : base(game)
        {
            Content = game.Content;
            OurGame = (Concentration)game;
        }
    }
}
