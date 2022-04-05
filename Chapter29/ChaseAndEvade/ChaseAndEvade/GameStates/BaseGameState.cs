using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using XELibrary;

namespace ChaseAndEvade
{
    public partial class BaseGameState : GameState
    {
        protected ChaseAndEvade OurGame;
        protected ContentManager Content;

        public BaseGameState(Game game)
            : base(game)
        {
            Content = game.Content;
            OurGame = (ChaseAndEvade)game;
        }
    }
}
