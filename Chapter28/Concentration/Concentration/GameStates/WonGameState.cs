using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;

namespace Concentration
{
    public sealed class WonGameState : BaseGameState, IWonGameState
    {
        private string gamertag;

        public WonGameState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IWonGameState), this);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 viewport = new Vector2(GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            Vector2 fontLength = OurGame.Font.MeasureString(Gamertag + " Won!!!");
            Vector2 pos = (viewport - fontLength * 3) / 2;

            OurGame.SpriteBatch.Begin();
            OurGame.SpriteBatch.DrawString(OurGame.Font,
                Gamertag + " Won!!!", pos,
                Color.White, 0, Vector2.Zero, 3.0f,
                SpriteEffects.None, 0);
            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            if (GameManager.State == this.Value)
            {
                OurGame.FadingState.Color = Color.Black;
                GameManager.PushState(OurGame.FadingState.Value, PlayerIndexInControl);

                foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                {
                    //since the gamertag in this state, we check with contains
                    if (Gamertag.Contains(signedInGamer.Gamertag))
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.WonTheGame;
                    else //could be other local gamers didn't win
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.GameOver;
                }
            }
        }

        public string Gamertag
        {
            get
            {
                if (string.IsNullOrEmpty(gamertag))
                    return ("You");
                else
                    return (gamertag);
            }

            set { gamertag = value; }
        }

    }
}
