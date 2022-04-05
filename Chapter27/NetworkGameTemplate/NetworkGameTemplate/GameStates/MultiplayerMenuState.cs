using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;

namespace NetworkGameTemplate
{
    public sealed class MultiplayerMenuState : BaseMenuState, IMultiplayerMenuState
    {
        private string[] entries = 
        {
            "Play Local Multiplayer Game",
            "Play System Link Game",
            "Play LIVE Game",
            "Back"
        };

        public MultiplayerMenuState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IMultiplayerMenuState), this);

            base.Entries = entries;
        }
        private bool startingLocalMultiplayer = false;

        public override void MenuSelected(PlayerIndex playerIndex, int selected)
        {
            PlayerIndexInControl = playerIndex;

            switch (selected)
            {
                case 0: //Play Local Multiplayer Game
                    {
                        //more than one controller active?
                        int connectedControllers = 0;
                        if (Input.GamePads[0].IsConnected)
                            connectedControllers++;
                        if (Input.GamePads[1].IsConnected)
                            connectedControllers++;
                        if (Input.GamePads[2].IsConnected)
                            connectedControllers++;
                        if (Input.GamePads[3].IsConnected)
                            connectedControllers++;

                        if (connectedControllers < 2)
                        {
                            OurGame.MessageDialogState.Message =
                               "In order to play a multiplayer game " +
                               "locally you must have more than one " +
                               "controller connected.";
                            OurGame.MessageDialogState.IsError = true;
                            GameManager.PushState(OurGame.MessageDialogState.Value,
                               PlayerIndexInControl);
                        }
                        else
                        {
                            if (LocalNetworkGamer.SignedInGamers.Count < 2)
                            {
                                //Bring up "Press Start" screen with mulitple panes?
                                if (connectedControllers > 2)
                                    Guide.ShowSignIn(4, false); //valid values are 1,2 and 4 so can't pass in 3
                                else
                                    Guide.ShowSignIn(2, false);

                                startingLocalMultiplayer = true;
                                SignedInGamer.SignedIn += new EventHandler<SignedInEventArgs>(SignedInGamer_SignedIn);
                            }

                            StartLocalMultiplayerGame();
                        }
                        break;
                    }
                case 1: //Play System Link Game
                    {
                        OurGame.NetworkMenuState.NetworkSessionType = NetworkSessionType.SystemLink;
                        GameManager.PushState(OurGame.NetworkMenuState.Value, PlayerIndexInControl);

                        break;
                    }
                case 2: //Play LIVE Game
                    {
                        OurGame.NetworkMenuState.NetworkSessionType = NetworkSessionType.PlayerMatch;
                        GameManager.PushState(OurGame.NetworkMenuState.Value, PlayerIndexInControl);

                        break;
                    }
                case 3: //Back
                    {
                        CancelMenu();
                        break;
                    }
            }
        }

        private void StartLocalMultiplayerGame()
        {
            if (LocalNetworkGamer.SignedInGamers.Count > 1)
            {
                OurGame.NetworkSession = NetworkSession.Create(NetworkSessionType.Local, OurGame.MaxLocalGamers, OurGame.MaxGamers);

                if (GameManager.ContainsState(OurGame.PlayingState.Value))
                {
                    GameManager.PopState();
                }
                else
                {
                    GameManager.ChangeState(OurGame.PlayingState.Value, PlayerIndexInControl);
                    OurGame.PlayingState.StartGame(LocalNetworkGamer.SignedInGamers.Count);
                }
            }
        }
        
        private void SignedInGamer_SignedIn(object sender, SignedInEventArgs e)
        {
            if (startingLocalMultiplayer)
            {
                StartLocalMultiplayerGame();

                //unregister for event
                SignedInGamer.SignedIn -= new EventHandler<SignedInEventArgs>(SignedInGamer_SignedIn);
            }
        }

        protected override void CancelMenu()
        {
            GameManager.PopState();
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            base.StateChanged(sender, e);

            if (GameManager.State != this.Value)
                Visible = true;
            else
            {
                if (OurGame.NetworkSession != null)
                {
                    OurGame.NetworkSession.Dispose();
                    OurGame.NetworkSession = null;
                }
            }
        }
    }
}
