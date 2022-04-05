using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleNetworkDemo
{
    public class GamerObject
    {
        public Vector2 Input;
        public Vector2 Velocity;
        public Vector2 Position;
        private Vector2 screenSize;
        public Texture2D GamerPicture;

        public GamerObject(int gamerIndex, Texture2D gamerPicture,
            int screenWidth, int screenHeight)
        {
            // Use the gamer index to determine a start point
            // each gamer will start in a different spot
            Position.X = screenWidth * 0.25f + (gamerIndex % 5) * screenWidth * 0.125f;
            Position.Y = screenHeight * 0.25f + (gamerIndex * .20f) * screenHeight * .20f;

            screenSize = new Vector2(screenWidth, screenHeight);

            GamerPicture = gamerPicture;
        }

        public void Update()
        {
            Velocity = Input * 2.0f;

            //Update the position
            Position += Velocity;

            //Clamp so the pic won’t go off the screen
            Position = Vector2.Clamp(Position, Vector2.Zero, screenSize);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(GamerPicture, Position, Color.White);
        }
    }
}
