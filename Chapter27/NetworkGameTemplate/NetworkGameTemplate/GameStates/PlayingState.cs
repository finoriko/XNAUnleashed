using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;

namespace NetworkGameTemplate
{
    public sealed class PlayingState : BaseGameState, IPlayingState
    {
        private const int CountdownTimer = 120;

        private TimeSpan? storedTime;
        private TimeSpan currentTime;
        private DateTime currentStopTime = DateTime.Now;

        private string timeText = string.Empty;
        private Vector2 timeTextShadowPosition;
        private Vector2 timeTextPosition;

        private string scoreText = string.Empty;
        private Vector2 scoreTextShadowPosition;
        private Vector2 scoreTextPosition;
        public int singlePlayerScore;

        private int numberOfPlayers;

        public PlayingState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IPlayingState), this);
        }

        public override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            currentTime = currentStopTime.Subtract(DateTime.Now);
            if (currentTime.Seconds < 0)
                currentTime = TimeSpan.Zero;

            PlayerIndex newPlayerIndex;

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Back, Keys.Escape, out newPlayerIndex))
            {
                PlayerIndexInControl = newPlayerIndex;
                storedTime = currentTime;
                GameManager.PushState(OurGame.StartMenuState.Value, PlayerIndexInControl);
            }

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Start, Keys.Enter, out newPlayerIndex))
            {
                PlayerIndexInControl = newPlayerIndex;
                storedTime = currentTime;
                GameManager.PushState(OurGame.PausedState.Value, PlayerIndexInControl);
            }

            base.Update(gameTime);

        }

        public void StartGame(int numberOfPlayers)
        {
            this.numberOfPlayers = numberOfPlayers;

            SetupGame();
        }

        private void SetupGame()
        {
            singlePlayerScore = 0;
            storedTime = null;
        }

        public override void Draw(GameTime gameTime)
        {
            OurGame.SpriteBatch.Begin();

            OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                timeTextShadowPosition, Color.Black);
            OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                timeTextPosition, Color.Firebrick);

            OurGame.SpriteBatch.DrawString(OurGame.Font, scoreText,
                scoreTextShadowPosition, Color.Black);
            OurGame.SpriteBatch.DrawString(OurGame.Font, scoreText,
                scoreTextPosition, Color.Firebrick);

            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            base.StateChanged(sender, e);

            if (GameManager.State != this.Value)
            {
                Visible = true;
                Enabled = false;
            }
            else
            {
                if (storedTime != null)
                    currentStopTime = DateTime.Now + (TimeSpan)storedTime;
                else
                    currentStopTime = DateTime.Now +
                        new TimeSpan(0, 0, CountdownTimer);
            }
        }

        protected override void LoadContent()
        {
            timeTextShadowPosition = new Vector2(TitleSafeArea.X, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 2);
            timeTextPosition = new Vector2(TitleSafeArea.X + 1.0f, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 2 + 1.0f);

            scoreTextShadowPosition = new Vector2(TitleSafeArea.X, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 3);
            scoreTextPosition = new Vector2(TitleSafeArea.X + 2.0f, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 3 + 2.0f);

            base.LoadContent();
        }

        public void PlayerLeft(byte gamerId)
        {
            if (OurGame.NetworkSession != null)
            {
                NetworkGamer gamer = OurGame.NetworkSession.FindGamerById(gamerId);
                //possible to return null if gamer was there, but isn't by the time we call this
                if (gamer == null)
                    return;

                //Handle the player leaving our game
                //TODO: Handle player leaving the game
            }
        }

        public void JoinInProgressGame(NetworkGamer gamer, int numberOfPlayers)
        {
            this.numberOfPlayers = numberOfPlayers;

            if (gamer.IsLocal)
            {
                SetupGame();

                //Handle local player joining
                //TODO: Handle local player joining
            }
            else
            {
                //Handle remote player joining
                //TODO: Handle remote player joining
            }


            SetPresenceInformation();
        }

        private void SetPresenceInformation()
        {
            //Determine what to set out online presence to
            foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
            {
                if (OurGame.NetworkSession != null)
                {
                    if (OurGame.NetworkSession.SessionType == NetworkSessionType.Local ||
                        OurGame.NetworkSession.SessionType == NetworkSessionType.SystemLink)
                    {   //Local Multiplayer or System Link Multiplayer
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.LocalVersus;
                    }
                    else if (numberOfPlayers > 1)
                    {   //Network Multiplayer
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.Multiplayer;
                    }
                    else
                    {   // network session is not null, and single player
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.WaitingForPlayers;
                    }
                }
                else if (numberOfPlayers == 1)
                {   //network session is null, so single player
                    signedInGamer.Presence.PresenceMode = GamerPresenceMode.SinglePlayer;
                }
                else
                {   //should never get here, because non single player game always has network session object
                    signedInGamer.Presence.PresenceMode = GamerPresenceMode.CornflowerBlue; //.None
                }

            }
        }

        public int Score
        {
            get { return (singlePlayerScore); }
        }
    }
}
