using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XELibrary;

namespace NetworkGameTemplate
{
    public sealed class OptionsMenuState : BaseGameState, IOptionsMenuState
    {
        private Texture2D texture;
        private GamePadState currentGamePadState;
        private GamePadState previousGamePadState;
        private int selected;


        public OptionsMenuState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IOptionsMenuState), this);
        }

        public override void Update(GameTime gameTime)
        {
            PlayerIndex newPlayerIndex;

            if (Input.WasPressed(PlayerIndexInControl, Buttons.B, Keys.Escape, out newPlayerIndex))
                GameManager.PopState();

            if (Input.KeyboardState.WasKeyPressed(Keys.Up) ||
               (currentGamePadState.IsButtonDown(Buttons.DPadUp) &&
                previousGamePadState.IsButtonUp(Buttons.DPadUp)) ||
               (currentGamePadState.ThumbSticks.Left.Y > 0 &&
                previousGamePadState.ThumbSticks.Left.Y <= 0))
            {
                selected--;
            }
            if (Input.KeyboardState.WasKeyPressed(Keys.Down) ||
               (currentGamePadState.IsButtonDown(Buttons.DPadDown) &&
                previousGamePadState.IsButtonUp(Buttons.DPadDown)) ||
               (currentGamePadState.ThumbSticks.Left.Y < 0 &&
                previousGamePadState.ThumbSticks.Left.Y >= 0))
            {
                selected++;
            }
            if (selected < 0)
                selected = 1;
            if (selected == 2)
                selected = 0;

            if ((Input.WasPressed(PlayerIndexInControl, Buttons.Start, Keys.Enter, out newPlayerIndex)) ||
                (Input.WasPressed(PlayerIndexInControl, Buttons.A, Keys.Space, out newPlayerIndex)))
            {
                switch (selected)
                {
                    case 0: 
                        {
                            break;
                        }
                    case 1: 
                        {
                            break;
                        }
                }
            }

            previousGamePadState = currentGamePadState;
            currentGamePadState = Input.GamePads[(int)PlayerIndexInControl.Value];

            base.Update(gameTime);
        }


        public override void Draw(GameTime gameTime)
        {
            Vector2 pos = new Vector2((GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.Draw(texture, pos, Color.White);

            switch (selected)
            {
                case 0: 
                    {
                        break;
                    }
                case 1: 
                    {
                        break;
                    }
            } 

            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void LoadContent()
        {
            texture = Content.Load<Texture2D>(
                @"Textures\optionsMenu");
        }
    }
}
