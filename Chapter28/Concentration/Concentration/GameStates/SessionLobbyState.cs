using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;


namespace Concentration
{
    public sealed class SessionLobbyState : BaseMenuState, ISessionLobbyState
    {
        private string[] statuses;

        public SessionLobbyState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(ISessionLobbyState), this);
        }

        protected override void LoadContent()
        {
            texture = Content.Load<Texture2D>(@"Textures\SessionLobby");

            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            texture.Dispose();
            texture = null;

            base.UnloadContent();
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            base.StateChanged(sender, e);

            if (GameManager.State == this.Value)
            {
                if (OurGame.NetworkSession == null)
                    return;

                HookSessionEvents();

                foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                    signedInGamer.Presence.PresenceMode =
                        GamerPresenceMode.WaitingInLobby;
            }
        }

        public override void Update(GameTime gameTime)
        {
            PlayerIndex playerIndex;

            //Press X to set yourself as Ready
            if (Input.WasPressed(null, Buttons.X, Keys.X, out playerIndex))
            {
                PlayerIndexInControl = playerIndex;
                foreach (LocalNetworkGamer gamer in OurGame.NetworkSession.LocalGamers)
                {
                    if (gamer.SignedInGamer.PlayerIndex == playerIndex)
                        gamer.IsReady = !gamer.IsReady;
                }
            }

            if (OurGame.NetworkSession == null)
                return;

            // The host checks if everyone is ready, and moves to game play if true.
            if (OurGame.NetworkSession.IsHost)
            {
                if (OurGame.NetworkSession.IsEveryoneReady &&
                    OurGame.NetworkSession.SessionState == NetworkSessionState.Lobby)
                {
                    //Now that all people have come and gone & we are starting our game,
                    //we reset playerindex & associate a single id to each gamer object
                    byte gamerIndex = 0;
                    foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                        gamer.Tag = gamerIndex++;

                    OurGame.NetworkSession.StartGame();
                }
            }

            //Update the session object
            OurGame.NetworkSession.Update();

            //Reset entries to get most recent ready state
            int gamerCount = OurGame.NetworkSession.AllGamers.Count;
            Entries = new string[gamerCount + 1];
            statuses = new string[gamerCount];

            for (int i = 0; i < gamerCount; i++)
            {
                Entries[i] = OurGame.NetworkSession.AllGamers[i].Gamertag;
                statuses[i] = OurGame.NetworkSession.AllGamers[i].IsReady.ToString();
            }

            Entries[gamerCount] = "Cancel";

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {

            base.Draw(gameTime);

            if ((statuses == null) ||
                (statuses.Length != OurGame.NetworkSession.AllGamers.Count))
                return;

            Vector2 pos = new Vector2(
                (GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);
            //offset the ready status further to the right
            Vector2 position = new Vector2(pos.X + 780, pos.Y + 150);

            OurGame.SpriteBatch.Begin();
            for (int i = 0; i < OurGame.NetworkSession.AllGamers.Count; i++)
            {
                base.DrawMenuItem(gameTime, ref position, i, statuses[i]);
            }

            OurGame.SpriteBatch.End();
        }

        public override void MenuSelected(PlayerIndex playerIndex, int selected)
        {
            PlayerIndexInControl = playerIndex;

            //Could bring up gamer info on selecting the name

            if (selected == OurGame.NetworkSession.AllGamers.Count)
                CancelMenu();
        }

        protected override void CancelMenu()
        {
            GameManager.ChangeState(OurGame.StartMenuState.Value, PlayerIndexInControl);
            GameManager.PushState(OurGame.MultiplayerMenuState.Value,
                PlayerIndexInControl);
            GameManager.PushState(OurGame.NetworkMenuState.Value, PlayerIndexInControl);
        }

        private void HookSessionEvents()
        {
            if (OurGame.NetworkSession != null)
            {
                OurGame.NetworkSession.GameStarted += new
                    EventHandler<GameStartedEventArgs>(GameStartedEventHandler);
                OurGame.NetworkSession.GamerJoined += new
                    EventHandler<GamerJoinedEventArgs>(GamerJoinedEventHandler);
                OurGame.NetworkSession.GamerLeft += new
                    EventHandler<GamerLeftEventArgs>(GamerLeftEventHandler);
                OurGame.NetworkSession.SessionEnded += new
                    EventHandler<NetworkSessionEndedEventArgs>(
                        OurGame.SessionEndedEventHandler);
            }
        }

        public void InviteAcceptedEventHandler(object sender, InviteAcceptedEventArgs e)
        {
            // Leave the current network session.
            if (OurGame.NetworkSession != null)
            {
                OurGame.NetworkSession.Dispose();
                OurGame.NetworkSession = null;
            }

            try
            {
                // Join a new session in response to the invite.
                OurGame.NetworkSession =
                    NetworkSession.JoinInvited(OurGame.MaxLocalGamers);

                HookSessionEvents();
            }
            catch (Exception error)
            {
                OurGame.MessageDialogState.Message = error.Message;
                OurGame.MessageDialogState.IsError = true;
                GameManager.PushState(OurGame.MessageDialogState.Value,
                    PlayerIndexInControl);
            }
        }

        private void GamerLeftEventHandler(object sender, GamerLeftEventArgs e)
        {
            OurGame.PlayingState.PlayerLeft(e.Gamer.Id);
        }

        private void GameStartedEventHandler(object sender, GameStartedEventArgs e)
        {
            GameManager.ChangeState(OurGame.PlayingState.Value, PlayerIndexInControl);
            OurGame.PlayingState.StartGame(OurGame.NetworkSession.AllGamers.Count);
        }

        private void GamerJoinedEventHandler(object sender, GamerJoinedEventArgs e)
        {
            //grab index and associate it to the gamer tag
            int gamerIndex = OurGame.NetworkSession.AllGamers.IndexOf(e.Gamer);
            e.Gamer.Tag = (byte)gamerIndex;

            //if we are playing (if we are still in the lobby, then no need for this)
            if (OurGame.NetworkSession.SessionState == NetworkSessionState.Playing)
            {
                //if we are already playing the game,
                //we don't want to change our state again
                if (!GameManager.ContainsState(OurGame.PlayingState.Value))
                {
                    GameManager.ChangeState(OurGame.PlayingState.Value,
                        PlayerIndexInControl);
                }

                //All games need to be notified of the new player
                OurGame.PlayingState.JoinInProgressGame(e.Gamer,
                    OurGame.NetworkSession.AllGamers.Count);
            }
        }
    }
}
