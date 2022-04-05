using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ChaseAndEvade
{
    public sealed class MessageDialogState : BaseGameState, IMessageDialogState
    {
        private Texture2D backgroundTexture;
        private string message;
        private bool isError;

        public MessageDialogState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(MessageDialogState), this);
        }

        protected override void LoadContent()
        {
            backgroundTexture = new Texture2D(GraphicsDevice, GraphicsDevice.Viewport.Width, 150, 1,
                TextureUsage.None, SurfaceFormat.Color);
            uint[] pixelData = new uint[GraphicsDevice.Viewport.Width * 150];
            for (int i = 0; i < pixelData.Length; i++)
            {
                pixelData[i] = new Color(0, 0, 0, 0.9f).PackedValue;
            }

            backgroundTexture.SetData<uint>(pixelData);

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            if (isError)
            {
                PlayerIndex playerIndex;
                if (Input.WasPressed(PlayerIndexInControl, Buttons.A, Keys.Enter, out playerIndex) ||
                    Input.WasPressed(PlayerIndexInControl, Buttons.B, Keys.Escape, out playerIndex))
                {
                    GameManager.PopState(); //we are done ...

                    //reset properties
                    isError = false;
                    message = string.Empty;
                }
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 viewport = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 fontLength =
                OurGame.Font.MeasureString(Message);
            Vector2 pos = (viewport - fontLength) / 2;

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.Draw(backgroundTexture,
                new Vector2(0, pos.Y - (OurGame.Font.LineSpacing * 2)), Color.White);
            OurGame.SpriteBatch.DrawString(OurGame.Font,
                Message,
                pos, MessageColor, 0, Vector2.Zero, 1.2f, SpriteEffects.None, 0);
            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        private Color MessageColor
        {
            get
            {
                if (isError)
                    return (Color.Red);
                else
                    return (Color.Blue);
            }
        }

        public string Message
        {
            get { return (message); }
            set { message = value; }
        }

        public bool IsError
        {
            get { return (isError); }
            set { isError = value; }
        }
    }
}
