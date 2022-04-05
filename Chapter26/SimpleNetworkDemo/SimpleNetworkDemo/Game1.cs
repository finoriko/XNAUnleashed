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

namespace SimpleNetworkDemo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D blankProfilePicture;
        private SpriteFont font;

        private NetworkSession networkSession;

        private PacketReader packetReader = new PacketReader();
        private PacketWriter packetWriter = new PacketWriter();

#if !ZUNE
        const int maxGamers = 16;
#else
        const int maxGamers = 8;
#endif

        const int maxLocalGamers = 4;
        
        private InputHandler input;

#if !ZUNE
        const int screenWidth = 1024;
        const int screenHeight = 768;
#else
        const int screenWidth = 320;
        const int screenHeight = 240;
#endif

        private string errorMessage;

#if ZUNE
        private RenderTarget2D zuneRenderTarget;
#endif

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferWidth = screenWidth;
            graphics.PreferredBackBufferHeight = screenHeight;

            Content.RootDirectory = "Content";

            input = new InputHandler(this, true);
            Components.Add(input);

            Components.Add(new GamerServicesComponent(this));

#if ZUNE
            graphics.ApplyChanges();

            zuneRenderTarget = new RenderTarget2D(
                GraphicsDevice,
                screenWidth,
                screenHeight,
                0,
                SurfaceFormat.Color);
#endif
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            blankProfilePicture = Content.Load<Texture2D>(@"Textures\noprofile");
            font = Content.Load<SpriteFont>(@"Fonts\arial");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (networkSession == null)
            {
                // If we are not in a network session, update the
                // menu screen that will let us create or join one.
                UpdateMenuScreen();
            }
            else
            {
                // If we are in a network session, update it.
                UpdateNetworkSession();
            }

            base.Update(gameTime);
        }

        private void UpdateMenuScreen()
        {
            if (IsActive)
            {
#if !ZUNE
                if (Gamer.SignedInGamers.Count == 0)
                {
                    // If there are no profiles signed in, we cannot proceed.
                    // Show the Guide so the user can sign in.
                    Guide.ShowSignIn(maxLocalGamers, false);
                }
                else
#endif                    
                if (input.WasPressed(0, Buttons.A, Keys.A))
                {
                    // Create a new session?
                    CreateSession();
                }
                else if (input.WasPressed(0, Buttons.B, Keys.B))
                {
                    // Join an existing session?
                    JoinSession();
                }
            }
        }

        private void CreateSession()
        {
            DrawMessage("Creating session...");

            try
            {
                networkSession = NetworkSession.Create(NetworkSessionType.SystemLink,
                    maxLocalGamers, maxGamers);

                HookSessionEvents();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                if (networkSession != null)
                {
                    networkSession.Dispose();
                    networkSession = null;
                }
            }
        }


        /// <summary>
        /// Joins an existing network session.
        /// </summary>
        private void JoinSession()
        {
            DrawMessage("Joining session...");

            try
            {
                // Search for sessions.
                using (AvailableNetworkSessionCollection availableSessions =
                            NetworkSession.Find(NetworkSessionType.SystemLink,
                                                maxLocalGamers, null))
                {
                    if (availableSessions.Count == 0)
                    {
                        errorMessage = "No network sessions found.";
                        return;
                    }

                    // Join the first session we found.
                    networkSession = NetworkSession.Join(availableSessions[0]);

                    HookSessionEvents();
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                if (networkSession != null)
                {
                    networkSession.Dispose();
                    networkSession = null;
                }
            }
        }

        /// <summary>
        /// Updates the state of the network session, moving the player
        /// around and synchronizing their state over the network.
        /// </summary>
        private void UpdateNetworkSession()
        {
            //Update our locally controlled player, and 
            //send their latest position to everyone else
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                UpdateLocalGamer(gamer);
            }

            //We need to call Update on every frame
            networkSession.Update();

            //Make sure the session has not ended
            if (networkSession == null)
                return;

            //Get packets that contain positions of remote players
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                ReadIncomingPackets(gamer);
            }
        }


        /// <summary>
        /// Helper for updating a locally controlled gamer.
        /// </summary>
        private void UpdateLocalGamer(LocalNetworkGamer gamer)
        {
            //Look up what gamerObject is associated with this local player.
            //could be a car, ship, person object, anything we wanted
            //we are generically calling it a gamerObject
            GamerObject gamerObject = gamer.Tag as GamerObject;

            //Update the object
            ReadInputs(gamerObject, gamer.SignedInGamer.PlayerIndex);

            gamerObject.Update();

            //Write the player state into a network packet
            packetWriter.Write(gamerObject.Position);

            //Send the data to everyone in the session
            gamer.SendData(packetWriter, SendDataOptions.InOrder);
        }

        private void ReadInputs(GamerObject gamerObject, PlayerIndex playerIndex)
        {
            //Handle the gamepad
            Vector2 gamerObjectInput = input.GamePads[(int)playerIndex].ThumbSticks.Left;

           //Handle the keyboard
            if (input.KeyboardState.IsKeyDown(Keys.Left))
                gamerObjectInput.X = -1;
            else if (input.KeyboardState.IsKeyDown(Keys.Right))
                gamerObjectInput.X = 1;

            if (input.KeyboardState.IsKeyDown(Keys.Up))
                gamerObjectInput.Y = 1;
            else if (input.KeyboardState.IsKeyDown(Keys.Down))
                gamerObjectInput.Y = -1;

#if !ZUNE
            gamerObjectInput.Y *= -1;
#else
            float tmp = -gamerObjectInput.X;
            gamerObjectInput.X = -gamerObjectInput.Y;
            gamerObjectInput.Y = tmp;

            if (input.GamePads[(int)playerIndex].DPad.Up == ButtonState.Pressed)
                gamerObjectInput.X = -1;
            else if (input.GamePads[(int)playerIndex].DPad.Down == ButtonState.Pressed)
                gamerObjectInput.X = 1;

            if (input.GamePads[(int)playerIndex].DPad.Left == ButtonState.Pressed)
                gamerObjectInput.Y = 1;
            else if (input.GamePads[(int)playerIndex].DPad.Right == ButtonState.Pressed)
                gamerObjectInput.Y = -1;
#endif

            //Normalize
            if (gamerObjectInput.Length() > 1)
                gamerObjectInput.Normalize();

            //Store the input values into the gamer object
            gamerObject.Input = gamerObjectInput;
        }

        /// <summary>
        /// After creating or joining a network session, we must subscribe to
        /// some events so we will be notified when the session changes state.
        /// </summary>
        private void HookSessionEvents()
        {
            networkSession.GamerJoined += GamerJoinedEventHandler;
            networkSession.SessionEnded += SessionEndedEventHandler;
        }


        /// <summary>
        /// This event handler will be called whenever a new gamer joins the session.
        /// We use it to allocate a gamer object, and associate it with the new gamer.
        /// </summary>
        private void GamerJoinedEventHandler(object sender, GamerJoinedEventArgs e)
        {
            int gamerIndex = networkSession.AllGamers.IndexOf(e.Gamer);

            Texture2D gamerProfilePic = blankProfilePicture;

    foreach(SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
    {
        if (signedInGamer.Gamertag == e.Gamer.Gamertag && signedInGamer.IsSignedInToLive)
        {
            GamerProfile gp = e.Gamer.GetProfile();
            gamerProfilePic = gp.GamerPicture;
        }
    }

            e.Gamer.Tag = new GamerObject(gamerIndex, gamerProfilePic, screenWidth, screenHeight);
        }

        /// <summary>
        /// Event handler notifies us when the network session has ended.
        /// </summary>
        private void SessionEndedEventHandler(object sender, NetworkSessionEndedEventArgs e)
        {
            errorMessage = e.EndReason.ToString();

            networkSession.Dispose();
            networkSession = null;
        }

        /// <summary>
        /// Helper for reading incoming network packets.
        /// </summary>
        private void ReadIncomingPackets(LocalNetworkGamer gamer)
        {
            //As long as incoming packets are available
            //keep reading them
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;

                //Read a single network packet
                gamer.ReceiveData(packetReader, out sender);

                //Ignore packets sent by local gamers
                //since we already know their state
                if (sender.IsLocal)
                    continue;

                //Look up the player associated with whoever sent this packet
                GamerObject remoteGamerObject = sender.Tag as GamerObject;

                //Read the state of this gamer object from the network
                remoteGamerObject.Position = packetReader.ReadVector2();
            }
        }

        private void DrawMessage(string message)
        {
            if (!BeginDraw())
                return;

#if ZUNE
            GraphicsDevice.SetRenderTarget(0, zuneRenderTarget);
#endif

            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(6, 6), Color.Black);
            spriteBatch.DrawString(font, message, new Vector2(5, 5), Color.White);

            spriteBatch.End();

            DrawRenderTarget();

            EndDraw();
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

#if ZUNE
            GraphicsDevice.SetRenderTarget(0, zuneRenderTarget);
#endif

            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (networkSession == null)
            {
                // If we are not in a network session, draw the
                // menu screen that will let us create or join one.
                DrawMenuScreen();
            }
            else
            {
                // If we are in a network session, draw it.
                DrawNetworkSession();
            }

            base.Draw(gameTime);

            DrawRenderTarget();
        }

        private void DrawRenderTarget()
        {
#if ZUNE
            //resolve the target
            GraphicsDevice.SetRenderTarget(0, null);

            //draw the texture rotated
            spriteBatch.Begin();
            spriteBatch.Draw(
                 zuneRenderTarget.GetTexture(),
                 new Vector2(120, 160),
                 null,
                 Color.White,
                 MathHelper.PiOver2,
                 new Vector2(160, 120),
                 1f,
                 SpriteEffects.None,
                 0);
            spriteBatch.End();
#endif
        }

        /// <summary>
        /// Draws the startup screen used to create and join network sessions.
        /// </summary>
        private void DrawMenuScreen()
        {
            string message = string.Empty;

            if (!string.IsNullOrEmpty(errorMessage))
                message += "Error:\n" + errorMessage.Replace(". ", ".\n") + "\n\n";

            message += "A = create session\n" +
                       "B = join session";

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(6, 6), Color.Black);
            spriteBatch.DrawString(font, message, new Vector2(5, 5), Color.White);

            spriteBatch.End();
        }

        private void DrawNetworkSession()
        {
            spriteBatch.Begin();

            // For each person in the session...
            foreach (NetworkGamer gamer in networkSession.AllGamers)
            {
                //Look up the gamer object associated to this network gamer
                GamerObject gamerObject = gamer.Tag as GamerObject;

                //Draw the gamer object
                gamerObject.Draw(spriteBatch);

                //Draw a gamertag label
                string label = gamer.Gamertag;
                Color labelColor = Color.Black;
                Vector2 labelOffset = new Vector2(75, 40);

                if (gamer.IsHost)
                    label += " (host)";

                //Flash the gamertag to yellow when the player is talking.
                if (gamer.IsTalking)
                    labelColor = Color.Yellow;

                spriteBatch.DrawString(font, label, gamerObject.Position, labelColor, 0,
                                       labelOffset, 0.5f, SpriteEffects.None, 0);
            }

            spriteBatch.End();
        }

    }
}
