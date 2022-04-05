using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Concentration
{
    public sealed class CreditsState : BaseGameState, ICreditsState
    {
        public CreditsState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(ICreditsState), this);
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
            base.Draw(gameTime);
        }

        protected override void LoadContent()
        {
        }
    }
}
