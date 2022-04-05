using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace NetworkGameTemplate
{
    public sealed class StartMenuState : BaseMenuState, IStartMenuState
    {
        private string[] entries = 
        {
            "Play Single Player Game",
            "Multiplayer",
            "Options",
            "High Scores",
            "Help",
            "Credits",
            "Exit Game"
        };

        public StartMenuState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IStartMenuState), this);

            base.Entries = entries;
        }

        public override void MenuSelected(PlayerIndex playerIndex, int selected)
        {
            PlayerIndexInControl = playerIndex;

            switch (selected)
            {
                case 0: //Play Single Player Game
                    {
                        if (GameManager.ContainsState(OurGame.PlayingState.Value))
                            GameManager.PopState();
                        else
                        {
                            //TODO: Clear out network session
                            /*
                            if (OurGame.NetworkSession != null)
                            {
                                OurGame.NetworkSession.Dispose();
                                OurGame.NetworkSession = null;
                            }
                            */

                            GameManager.ChangeState(OurGame.PlayingState.Value, PlayerIndexInControl);
                            OurGame.PlayingState.StartGame(1);
                        }
                        break;
                    }
                case 1: //Multiplayer Menu
                    {
                        GameManager.PushState(OurGame.MultiplayerMenuState.Value, PlayerIndexInControl);
                        break;
                    }
                case 2: //Options Menu
                    {
                        GameManager.PushState(OurGame.OptionsMenuState.Value, PlayerIndexInControl);
                        break;
                    }
                case 3: //High Scores Menu
                    {
                        GameManager.PushState(OurGame.HighScoresState.Value, PlayerIndexInControl);
                        OurGame.HighScoresState.AlwaysDisplay = true;
                        break;
                    }
                case 4: //Help
                    {
                        GameManager.PushState(OurGame.HelpState.Value, PlayerIndexInControl);
                        break;
                    }
                case 5: //Credits
                    {
                        GameManager.PushState(OurGame.CreditsState.Value, PlayerIndexInControl);
                        break;
                    }
                case 6: //Exit
                    {
                        CancelMenu();
                        break;
                    }
            }
        }

        protected override void CancelMenu()
        {
            GameManager.ChangeState(OurGame.TitleIntroState.Value, null);
        }

        protected override void StateChanged(object sender, EventArgs e)
        {
            base.StateChanged(sender, e);

            if (GameManager.State != this.Value)
            {
                Visible = true;
            }
            else
            {
                foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                {
                    if (GameManager.ContainsState(OurGame.PlayingState.Value))
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.Paused;
                    else
                        signedInGamer.Presence.PresenceMode = GamerPresenceMode.AtMenu;
                }
            }
        }
    }
}
