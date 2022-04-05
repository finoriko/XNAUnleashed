using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ChaseAndEvade
{
    public abstract class BaseMenuState : BaseGameState, IStartMenuState
    {
        protected SpriteFont font;
        private GamePadState currentGamePadState;
        private GamePadState previousGamePadState;
        protected int selected;

        protected Texture2D texture;
        protected Point menuSize = new Point(800, 600);

        public string[] Entries;

        public BaseMenuState(Game game)
            : base(game)
        {
        }

        protected abstract void CancelMenu();

        public override void Update(GameTime gameTime)
        {
            PlayerIndex playerIndex;
            if (Input.WasPressed(PlayerIndexInControl, Buttons.Back, Keys.Escape, out playerIndex) ||
                Input.WasPressed(PlayerIndexInControl, Buttons.B, Keys.Back, out playerIndex))
            {
                CancelMenu();
            }

            if (Input.KeyboardState.WasKeyPressed(Keys.Up) ||
               (currentGamePadState.DPad.Up == ButtonState.Pressed &&
                previousGamePadState.DPad.Up == ButtonState.Released) ||
               (currentGamePadState.ThumbSticks.Left.Y > 0 &&
                previousGamePadState.ThumbSticks.Left.Y <= 0))
            {
                selected--;
            }
            if (Input.KeyboardState.WasKeyPressed(Keys.Down) ||
               (currentGamePadState.DPad.Down == ButtonState.Pressed &&
                previousGamePadState.DPad.Down == ButtonState.Released) ||
               (currentGamePadState.ThumbSticks.Left.Y < 0 &&
                previousGamePadState.ThumbSticks.Left.Y >= 0))
            {
                selected++;
            }

            if (selected < 0)
                selected = Entries.Length - 1;
            if (selected == Entries.Length)
                selected = 0;

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Start, Keys.Enter, out playerIndex) ||
                (Input.WasPressed(PlayerIndexInControl, Buttons.A, Keys.Space, out playerIndex)))
            {
                PlayerIndexInControl = playerIndex;
                MenuSelected(playerIndex, selected);
            }

            previousGamePadState = currentGamePadState;
            currentGamePadState = Input.GamePads[(int)playerIndex];

            base.Update(gameTime);
        }

        public abstract void MenuSelected(PlayerIndex playerIndex, int selected);

        public override void Draw(GameTime gameTime)
        {
            Vector2 pos = new Vector2(
                (GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);

            Vector2 position = new Vector2(pos.X + 240, pos.Y + 150);

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.Draw(texture, pos, Color.White);

            if (Entries != null)
            {
                for (int i = 0; i < Entries.Length; i++)
                {
                    DrawMenuItem(gameTime, ref position, i, Entries[i]);
                }
            }
            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected void DrawMenuItem(GameTime gameTime, ref Vector2 position,
            int i, string text)
        {
            Color color;
            float scale;

            if (i == selected)
            {
                // The selected entry is yellow, and has an animating size.
                double time = gameTime.TotalGameTime.TotalSeconds;

                float pulsate = 0;
                //turn on pulsating if enabled
                if (Enabled)
                    pulsate = (float)Math.Sin(time * 12) + 1;

                color = Color.Firebrick;
                scale = 1 + pulsate * 0.05f;
            }
            else
            {
                color = Color.WhiteSmoke;
                scale = 1;
            }
            // Draw text, centered on the middle of each line.
            Vector2 origin = new Vector2(0, font.LineSpacing / 2);
            Vector2 shadowPosition = new Vector2(position.X - 2, position.Y);

            //Draw Shadow
            OurGame.SpriteBatch.DrawString(font, text,
                shadowPosition, Color.DarkSlateGray, 0, origin, scale,
                SpriteEffects.None, 0);
            //Draw Text
            OurGame.SpriteBatch.DrawString(font, text,
                position, color, 0, origin, scale, SpriteEffects.None, 0);

            position.Y += font.LineSpacing;
        }

        protected override void LoadContent()
        {
            font = Content.Load<SpriteFont>(@"Fonts\menu");

            if (texture == null)
            {
                texture = new Texture2D(GraphicsDevice, menuSize.X, menuSize.Y,
                    1, TextureUsage.None, SurfaceFormat.Color);

                uint[] pixelData = new uint[menuSize.X * menuSize.Y];
                for (int i = 0; i < pixelData.Length; i++)
                {
                    pixelData[i] = new Color(0, 0, 0, 0.90f).PackedValue;
                }

                texture.SetData<uint>(pixelData);
            }

            base.LoadContent();
        }
    }
}
