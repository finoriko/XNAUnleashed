using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using XELibrary;

namespace NetworkGameTemplate
{
    public class NetworkGameTemplate : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        public SpriteBatch SpriteBatch;
        public SpriteFont Font;

        public ITitleIntroState TitleIntroState;
        public IStartMenuState StartMenuState;
        public IOptionsMenuState OptionsMenuState;
        public IPlayingState PlayingState;
        public ILostGameState LostGameState;
        public IWonGameState WonGameState;
        public IFadingState FadingState;
        public IPausedState PausedState;
        public IHighScoresState HighScoresState;
        public IHelpState HelpState;
        public ICreditsState CreditsState;
        public IMultiplayerMenuState MultiplayerMenuState;
        public IMessageDialogState MessageDialogState;
        public INetworkMenuState NetworkMenuState;
        public ISessionListState SessionListState;
        public ISessionLobbyState SessionLobbyState;

        private const int screenWidth = 1280;
        private const int screenHeight = 720;

        private InputHandler input;
        private GameStateManager gameManager;

        public readonly int MaxGamers = 4;
        public readonly int MaxLocalGamers = 2;

        public NetworkSession NetworkSession;

        public NetworkGameTemplate()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferHeight = screenHeight;
            graphics.PreferredBackBufferWidth = screenWidth;

            Content.RootDirectory = "Content";

            input = new InputHandler(this);
            Components.Add(input);

            gameManager = new GameStateManager(this);
            Components.Add(gameManager);

            Components.Add(new GamerServicesComponent(this));

            TitleIntroState = new TitleIntroState(this);
            StartMenuState = new StartMenuState(this);
            OptionsMenuState = new OptionsMenuState(this);
            HighScoresState = new HighScoresState(this);
            PlayingState = new PlayingState(this);
            FadingState = new FadingState(this);
            LostGameState = new LostGameState(this);
            WonGameState = new WonGameState(this);
            PausedState = new PausedState(this);
            HelpState = new HelpState(this);
            CreditsState = new CreditsState(this);
            MultiplayerMenuState = new MultiplayerMenuState(this);
            MessageDialogState = new MessageDialogState(this);
            NetworkMenuState = new NetworkMenuState(this);
            SessionListState = new SessionListState(this);
            SessionLobbyState = new SessionLobbyState(this);

            gameManager.ChangeState(TitleIntroState.Value, null);

            NetworkSession.InviteAccepted += SessionLobbyState.InviteAcceptedEventHandler;

            foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                signedInGamer.Presence.PresenceMode = GamerPresenceMode.WastingTime;
        }

        protected override void Initialize()
        {

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<SpriteFont>(@"Fonts\Arial");
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }

        public void SessionEndedEventHandler(object sender, NetworkSessionEndedEventArgs e)
        {
            NetworkSession.Dispose();
            NetworkSession = null;

            gameManager.ChangeState(TitleIntroState.Value, null);

            MessageDialogState.Message = e.EndReason.ToString();
            MessageDialogState.IsError = true;
            gameManager.PushState(MessageDialogState.Value, null);
        }

        internal void SetSimulatedValues()
        {
            NetworkSession.SimulatedLatency = TimeSpan.FromMilliseconds(200);
            NetworkSession.SimulatedPacketLoss = .2f;
        }
    }
}
