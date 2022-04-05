using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XELibrary;

namespace ChaseAndEvade
{
    public sealed class TitleIntroState : BaseGameState, ITitleIntroState
    {
        private Texture2D texture;

        public TitleIntroState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(ITitleIntroState), this);
        }

        public override void Update(GameTime gameTime)
        {
            PlayerIndex newPlayerIndex;
            if (Input.WasPressed(PlayerIndexInControl, Buttons.Back, Keys.Escape, out newPlayerIndex))
                OurGame.Exit();

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Start, Keys.Enter, out newPlayerIndex))
            {
                //We know which controller hit "Start", 
                //so set them as the player in control
                PlayerIndexInControl = newPlayerIndex;

                // push our start menu onto the stack
                GameManager.PushState(OurGame.StartMenuState.Value, PlayerIndexInControl);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 pos = new Vector2((GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);

            GraphicsDevice device = GraphicsDevice;

            device.Clear(Color.Black);

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.Draw(texture, pos, Color.White);
            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void LoadContent()
        {
            texture = Content.Load<Texture2D>(@"Textures\titleIntro");

            base.LoadContent();
        }

    }
}
