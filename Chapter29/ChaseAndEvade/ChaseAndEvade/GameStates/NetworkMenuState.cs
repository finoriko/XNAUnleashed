using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Net;

namespace ChaseAndEvade
{
    public sealed class NetworkMenuState : BaseMenuState, INetworkMenuState
    {
        private NetworkSessionType networkSessionType;

        private string[] entries = 
            {
                "Create a Session",
                "Join a Session",
                "Cancel"
            };


        public NetworkMenuState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(INetworkMenuState), this);

            base.Entries = entries;
        }

        public override void MenuSelected(PlayerIndex playerIndex, int selected)
        {
            PlayerIndexInControl = playerIndex;

            switch (selected)
            {
                case 0: //Create a Session
                    {
                        try
                        {
                            OurGame.NetworkSession = NetworkSession.Create(NetworkSessionType, OurGame.MaxLocalGamers, OurGame.MaxGamers, 0, null);
                            OurGame.NetworkSession.AllowJoinInProgress = true;
                            OurGame.NetworkSession.AllowHostMigration = true;

                            OurGame.SetSimulatedValues();
                        }
                        catch (GamerPrivilegeException exc)
                        {
                            //Change the game states to the previous state
                            CancelMenu();

                            OurGame.MessageDialogState.Message = exc.Message;
                            OurGame.MessageDialogState.IsError = true;
                            GameManager.PushState(OurGame.MessageDialogState.Value, PlayerIndexInControl);

                            return;
                        }

                        //Go to lobby
                        GameManager.ChangeState(OurGame.SessionLobbyState.Value, PlayerIndexInControl);
                        break;
                    }
                case 1: //Join a Session
                    {
                        OurGame.SessionListState.NetworkSessionType = NetworkSessionType;
                        GameManager.ChangeState(OurGame.SessionListState.Value, PlayerIndexInControl);

                        break;
                    }
                case 2: //Cancel
                    {
                        CancelMenu();

                        break;
                    }
            }
        }

        protected override void CancelMenu()
        {
            GameManager.PopState();

            if (OurGame.NetworkSession != null)
            {
                OurGame.NetworkSession.Dispose();
                OurGame.NetworkSession = null;
            }
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            base.StateChanged(sender, e);

            if (GameManager.State != this.Value)
                Visible = true;
        }

        public NetworkSessionType NetworkSessionType
        {
            get { return(networkSessionType); }
            set { networkSessionType = value; }
        }
    }
}
