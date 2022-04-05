using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ChaseAndEvade
{
    public sealed class HelpState : BaseGameState, IHelpState
    {
        private Texture2D texture;

        public HelpState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IHelpState), this);
        }

        public override void Update(GameTime gameTime)
        {
            PlayerIndex playerIndex;

            if (Input.WasPressed(PlayerIndexInControl, Buttons.B, Keys.Escape, out playerIndex))
                GameManager.PopState();

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 pos = new Vector2((GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.Draw(texture, pos, Color.White);
            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void LoadContent()
        {
            texture = Content.Load<Texture2D>(
                @"Textures\help");
        }
    }
}
