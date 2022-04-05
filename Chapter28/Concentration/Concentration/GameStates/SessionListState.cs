using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace Concentration
{
    public sealed class SessionListState : BaseMenuState, ISessionListState
    {
        private NetworkSessionType networkSessionType;
        private AvailableNetworkSessionCollection availableSessions;

        public SessionListState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(ISessionListState), this);
        }

        protected override void LoadContent()
        {
            texture = Content.Load<Texture2D>(@"Textures\SessionList");
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
                //set presence to looking for games since we are picking a session
                foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                    signedInGamer.Presence.PresenceMode = GamerPresenceMode.LookingForGames;

                string errorMessage = string.Empty;

                try
                {
                    availableSessions =
                                NetworkSession.Find(networkSessionType,
                                                    OurGame.MaxLocalGamers, null);
                } //it is possible that someone tried to join a LIVE game without enough privs
                catch (GamerPrivilegeException exc)
                {
                    errorMessage = exc.Message;
                }

                if (errorMessage != string.Empty || availableSessions.Count == 0)
                {
                    //Change the game states to the previous state
                    CancelMenu();

                    if (errorMessage == string.Empty)
                        errorMessage = "No network sessions found.";

                    OurGame.MessageDialogState.Message = errorMessage;
                    OurGame.MessageDialogState.IsError = true;
                    GameManager.PushState(OurGame.MessageDialogState.Value, PlayerIndexInControl);

                    if (availableSessions != null)
                    {
                        availableSessions.Dispose();
                        availableSessions = null;
                    }

                    return;
                }
                else
                {
                    int numberOfSessions = Math.Min(7, availableSessions.Count);
                    Entries = new string[numberOfSessions + 1];
                    for (int i = 0; i < numberOfSessions; i++)
                    {
                        if (availableSessions[i].HostGamertag.ToLower().EndsWith("s"))
                            Entries[i] = "Join " + availableSessions[i].HostGamertag + "' Game";
                        else
                            Entries[i] = "Join " + availableSessions[i].HostGamertag + "'s Game";
                    }

                    Entries[numberOfSessions] = "Cancel";
                }
            }
        }

        public override void MenuSelected(PlayerIndex playerIndex, int selected)
        {
            PlayerIndexInControl = playerIndex;

            if (selected < availableSessions.Count)
            {
                //pick correct session
                try
                {
                    OurGame.NetworkSession = NetworkSession.Join(availableSessions[selected]);

                    OurGame.SetSimulatedValues();

                    GameManager.ChangeState(OurGame.SessionLobbyState.Value, PlayerIndexInControl);
                }
                catch(Exception e)
                {
                    OurGame.MessageDialogState.Message = e.Message;
                    OurGame.MessageDialogState.IsError = true;
                    GameManager.PushState(OurGame.MessageDialogState.Value, PlayerIndexInControl);
                }
            }
            else //Cancel
                CancelMenu();
        }

        protected override void CancelMenu()
        {
            GameManager.ChangeState(OurGame.StartMenuState.Value, PlayerIndexInControl);
            GameManager.PushState(OurGame.MultiplayerMenuState.Value, PlayerIndexInControl);
            GameManager.PushState(OurGame.NetworkMenuState.Value, PlayerIndexInControl);

            //set presence to looking for games since we are picking a session
            foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                signedInGamer.Presence.PresenceMode = GamerPresenceMode.None;
        }

        public NetworkSessionType NetworkSessionType
        {
            get { return (networkSessionType); }
            set { networkSessionType = value; }
        }
    }
}
