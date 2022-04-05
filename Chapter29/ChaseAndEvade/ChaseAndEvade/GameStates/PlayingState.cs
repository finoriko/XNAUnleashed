using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;

using XELibrary;
using System.Collections.Generic;

namespace ChaseAndEvade
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

        private int numberOfPlayers;

        //Copied from AI Demo 
        private Model sphere;
        private Skybox skybox;

        private Camera camera;

        private const int MaxEnemies = 10;
        private Enemy[] enemies = new Enemy[MaxEnemies];

        private const float ArenaSize = 500;

        public bool RestrictToXY = true;

        private float moveUnit = 20;

        private Random rand = new Random();

        private const float SeekRadius = 75.0f;
        private const float EvadeRadius = 75.0f;

        private Player singlePlayer;

        //Multiplayer member fields
        private int framesBetweenPackets = 6;
        private int framesSinceLastSend;

        private PacketWriter packetWriter = new PacketWriter();
        private PacketReader packetReader = new PacketReader();

        private Dictionary<byte, Color> colors;
        private int colorIndex = 0;
        private Color[] defaultColors;


        //Local Mulitplayer
        private Viewport leftViewport;
        private Viewport rightViewport;
        private Camera camera2;

        private enum MessageType : byte { RequestToJoinInProgressGame, 
            JoinedInProgressGame, PlayerMove, EnemyState, Color, CapturedEnemy, WasCaptured };


        public PlayingState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IPlayingState), this);

            camera = new Camera(game);
            game.Components.Add(camera);

        }

        public void KeepWithinBounds(ref Vector3 position, ref Vector3 velocity)
        {
            if ((position.X < -OurGame.ArenaSize) || (position.X > OurGame.ArenaSize))
                velocity.X = -velocity.X;
            if ((position.Y < -OurGame.ArenaSize) || (position.Y > OurGame.ArenaSize))
                velocity.Y = -velocity.Y;
            if ((position.Z < -OurGame.ArenaSize) || (position.Z > OurGame.ArenaSize))
                velocity.Z = -velocity.Z;

            position += velocity;
        }

        public override void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (OurGame.NetworkSession == null)
                UpdateSinglePlayer(gameTime);
            else
                UpdateNetworkSession(gameTime);

            currentTime = currentStopTime.Subtract(DateTime.Now);
            //we could force time to never go negative
            if (currentTime.Seconds < 0)
            {
                GameManager.ChangeState(OurGame.WonGameState.Value, null);
                currentTime = TimeSpan.Zero;
            }

            PlayerIndex newPlayerIndex;

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Back, Keys.Escape, out newPlayerIndex))
            {
                if (OurGame.NetworkSession == null ||
                    OurGame.NetworkSession.AllGamers.Count == OurGame.NetworkSession.LocalGamers.Count)
                {
                    storedTime = currentTime;
                }

                PlayerIndexInControl = newPlayerIndex;
                GameManager.PushState(OurGame.StartMenuState.Value, PlayerIndexInControl);
            }

            if (Input.WasPressed(PlayerIndexInControl, Buttons.Start, Keys.Enter, out newPlayerIndex))
            {
                if (OurGame.NetworkSession == null || 
                    OurGame.NetworkSession.AllGamers.Count == OurGame.NetworkSession.LocalGamers.Count)
                {

                    PlayerIndexInControl = newPlayerIndex;
                    storedTime = currentTime;
                    GameManager.PushState(OurGame.PausedState.Value, PlayerIndexInControl);
                }
            }

            base.Update(gameTime);

        }
        
        private void UpdateSinglePlayer(GameTime gameTime)
        {
            Vector3 playerInput = singlePlayer.HandleInput(PlayerIndexInControl.Value);

            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            playerInput *= elapsedTime;

            singlePlayer.UpdateLocal(playerInput);

            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemy = enemies[i];

                UpdateEnemyPosition(i, gameTime, elapsedTime, enemy);

                //reset player or enemy if collided
                if ((enemy.Position - singlePlayer.Position).Length() <
                    (sphere.Meshes[0].BoundingSphere.Radius * 2))
                {
                    if (enemy.State == Enemy.AIState.Attack)
                    {
                        singlePlayer.ResetPosition();
                        singlePlayer.Score -= 50;
                    }
                    else
                    {
                        //enemy.Position.X = enemy.Position.Y = 0;
                        enemy.Position = new Vector3((i * 50) + 50, (i * 25) - 50, -300);
                        singlePlayer.Score += 100;
                    }
                }
            }
        }

        private void UpdateNetworkSession(GameTime gameTime)
        {
            bool sendPacketThisFrame = false;

            framesSinceLastSend++;
            if (framesSinceLastSend >= framesBetweenPackets)
            {
                sendPacketThisFrame = true;
                framesSinceLastSend = 0;
            }

            //Update our locally controlled player, and 
            //send their latest position to everyone else
            foreach (LocalNetworkGamer gamer in OurGame.NetworkSession.LocalGamers)
            {
                UpdateLocalGamer(gamer, gameTime, sendPacketThisFrame);
            }

            //We Do actual sending and receiving of network packets
            try
            {
                OurGame.NetworkSession.Update();
            }
            catch (Exception e)
            {
                OurGame.MessageDialogState.Message = e.Message;
                OurGame.MessageDialogState.IsError = true;
                GameManager.PushState(OurGame.MessageDialogState.Value, PlayerIndexInControl);

                OurGame.NetworkSession.Dispose();
                OurGame.NetworkSession = null;
            }

            //Make sure the session has not ended
            if (OurGame.NetworkSession == null)
                return;

            //Get packets that contain data of remote players
            foreach (LocalNetworkGamer gamer in OurGame.NetworkSession.LocalGamers)
            {
                ReadIncomingPackets(gamer, gameTime);
            }

            //Apply prediction and smoothing to the remotely controlled players.
            foreach (NetworkGamer gamer in OurGame.NetworkSession.RemoteGamers)
            {
                Player player = gamer.Tag as Player;

                player.UpdateRemote(framesBetweenPackets);
            }
        }

        private void UpdateLocalGamer(LocalNetworkGamer gamer, GameTime gameTime, bool sendPacketThisFrame)
        {
            //Get player from local network gamer
            Player player = gamer.Tag as Player;

            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //get player index
            PlayerIndex playerIndex = gamer.SignedInGamer.PlayerIndex;

            Vector3 playerInput = player.HandleInput(playerIndex);

            playerInput *= elapsedTime;

            player.UpdateLocal(playerInput);

            for (int i = 0; i < enemies.Length; i++)
            {
                if (gamer.IsHost)
                {
                    //Host Updates the Enemy States
                    UpdateEnemy(i, gameTime);

                    //if we are to send a packet, do so
                    if (sendPacketThisFrame)
                    {
                        SendEnemyData(i, gamer);
                    }
                }

                //Only check collisions against local gamers
                //even though as the host we are processing the enemies
                //we are not specifying when we think an enemy collided with someone else
                //it is up to the individual players to let us know when that happens
                //CheckForCollisions(i, gamer);
            }
            

            // Periodically send our state to everyone in the session.
            if (sendPacketThisFrame)
            {
                packetWriter.Write((byte)MessageType.PlayerMove);
                player.WriteNetworkPacket(packetWriter, gameTime);
                gamer.SendData(packetWriter, SendDataOptions.InOrder);

                //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " sent " + MessageType.PlayerMove.ToString());
            }
        }

        private void CheckForCollisions(int enemyIndex, LocalNetworkGamer gamer)
        {
            Enemy enemy = enemies[enemyIndex];

            //Enemy initialized yet?
            if (enemy == null)
                return;

            Player player = gamer.Tag as Player;

            //reset player or enemy if collided
            if ((enemy.Position - player.Position).Length() <
                (sphere.Meshes[0].BoundingSphere.Radius * 2))
            {
                if (enemy.State == Enemy.AIState.Attack)
                {
                    player.ResetPosition();
                    player.Score -= 50;

                    //Send player position
                    //send player score
                    packetWriter.Write((byte)MessageType.WasCaptured);
                    packetWriter.Write(player.Score);
                    packetWriter.Write(player.Position);

                    gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);
                }
                else
                {
                    enemy.Position = new Vector3(-rand.Next(300), -rand.Next(300), -300);
                    player.Score += 100;

                    //send enemy position
                    //send player score
                    packetWriter.Write((byte)MessageType.CapturedEnemy);
                    packetWriter.Write(player.Score);
                    packetWriter.Write((byte)enemyIndex);
                    packetWriter.Write(enemy.Position);

                    gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);
                }
            }
        }

        private void SendEnemyData(int enemyIndex, LocalNetworkGamer gamer)
        {
            Enemy enemy = enemies[enemyIndex];

            packetWriter.Write((byte)MessageType.EnemyState);

            packetWriter.Write((byte)enemyIndex); //set index

            packetWriter.Write((byte)enemy.State);
            packetWriter.Write(enemy.Position);
            packetWriter.Write(enemy.Velocity);
            packetWriter.Write((byte)enemy.Health);

            gamer.SendData(packetWriter, SendDataOptions.InOrder);

            CheckForCollisions(enemyIndex, gamer);
        }

        private Enemy ReadEnemyData(out byte enemyIndex)
        {
            enemyIndex = packetReader.ReadByte();
            Enemy enemy = enemies[enemyIndex]; //read index
            if (enemy == null)
                enemy = new Enemy();

            //enemy.PlayerIdInFocus = packetReader.ReadByte();
            enemy.State = (Enemy.AIState)packetReader.ReadByte();
            enemy.Position = packetReader.ReadVector3();
            enemy.Velocity = packetReader.ReadVector3();
            enemy.Health = packetReader.ReadByte();

            //No need to pass around color because we know what color we should be based on the state we are in
            if (enemy.State == Enemy.AIState.Search)
            {
                if (enemy.Health < 5)
                    enemy.Color = Color.LightBlue;
                else
                    enemy.Color = Color.Pink;
            }
            else if (enemy.State == Enemy.AIState.Attack)
                enemy.Color = Color.Red;
            else if (enemy.State == Enemy.AIState.Retreat)
                enemy.Color = Color.Blue;
            else
                enemy.Color = Color.Black; //shouldn't get here

            return (enemy);
        }

        private void UpdateEnemy(int enemyIndex, GameTime gameTime)
        {
            //loop through all gamers to determine what we should do
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Enemy enemy = enemies[enemyIndex];

            UpdateEnemyPosition(enemyIndex, gameTime, elapsedTime, enemy);
        }

        private void UpdateEnemyPosition(int enemyIndex, GameTime gameTime, float elapsedTime, Enemy enemy)
        {
            switch (enemy.State)
            {
                case Enemy.AIState.Search:
                    {
                        MoveRandomly(enemyIndex, gameTime);
                        break;
                    }
                case Enemy.AIState.Attack:
                    {
                        TrackPlayer(enemyIndex);
                        break;
                    }
                case Enemy.AIState.Retreat:
                    {
                        EvadePlayer(enemyIndex);
                        break;
                    }
                default:
                    {
                        throw (new ApplicationException("Unknown State: " +
                            enemy.State.ToString()));
                    }
            }

            enemy.Velocity *= elapsedTime;
            enemy.Position += enemy.Velocity;
            KeepWithinBounds(ref enemy.Position, ref enemy.Velocity);
        }

        private void ReadIncomingPackets(LocalNetworkGamer gamer, GameTime gameTime)
        {
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;
                gamer.ReceiveData(packetReader, out sender);

                //Ignore packets sent by local gamers
                //since we already know their state
                if (sender.IsLocal)
                    continue;

                //Get up the player index
                Player player = gamer.Tag as Player;

                //Determine the type of packet this is
                byte header = packetReader.ReadByte();

                //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " received " + ((MessageType)header).ToString());

                if (header == (byte)MessageType.RequestToJoinInProgressGame) //Sent by person joining game in progress
                {
                    if (gamer.IsHost)
                    {
                        Player p;

                        packetWriter.Write((byte)MessageType.JoinedInProgressGame);

                        //pass in number of ticks left in the game
                        packetWriter.Write(currentTime.Ticks);

                        //Now write all all the player's colors
                        foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                        {
                            packetWriter.Write(g.Id);
                            p = g.Tag as Player;

                            //it's possible that a group of local players
                            //joined. but only one at a time is called
                            //so there are gamers that don't have a color yet
                            //if this happense, just set it.
                            if (p.Color == Color.TransparentBlack)
                                SetPlayerColor(p, g);

                            packetWriter.Write(GetIndexFromColor(p.Color));
                        }

                        gamer.SendData(packetWriter, SendDataOptions.Reliable, sender);

                        //Time sensative info - send now.
                        OurGame.NetworkSession.Update();
                    }
                }
                else if (header == (byte)MessageType.JoinedInProgressGame)
                {   //received by person joining game in progress

                    //retrieve amount of time left in play
                    //read in number of ticks left in the game
                    currentTime = new TimeSpan(packetReader.ReadInt64());
                    //calculate our stop time based on how much time is left
                    //we do not pass this in since different machines can have
                    //different date times. (Not just time zones, but actually 
                    //have a few seconds or minutes difference)
                    currentStopTime = DateTime.Now.Add(currentTime);

                    //Set each player's color
                    //loops through each enemy in the packet
                    while (packetReader.Position < packetReader.Length)
                    {
                        NetworkGamer g = OurGame.NetworkSession.FindGamerById(packetReader.ReadByte());
                        Player p = g.Tag as Player;
                        p.Color = defaultColors[packetReader.ReadByte()];
                    }
                }
                //Add in other conditions for Game specific MessageTypes
                else if (header == (byte)MessageType.EnemyState)
                {
                    byte enemyIndex;

                    //loops through each enemy in the packet
                    while (packetReader.Position < packetReader.Length)
                    {
                        //Read this enemy's info
                        Enemy enemy = ReadEnemyData(out enemyIndex);

                        //store the enemy in our list
                        enemies[enemyIndex] = enemy;

                        //see if we collided with this enemy
                        CheckForCollisions(enemyIndex, gamer);
                    }
                }
                else if (header == (byte)MessageType.Color)
                {
                    NetworkGamer g = OurGame.NetworkSession.FindGamerById(packetReader.ReadByte());
                    Player p = g.Tag as Player;
                    p.Color = defaultColors[packetReader.ReadByte()];
                }
                else if (header == (byte)MessageType.PlayerMove)
                {
                    TimeSpan latency = OurGame.NetworkSession.SimulatedLatency +
                        TimeSpan.FromTicks(sender.RoundtripTime.Ticks / 2);

                    //The player object we want to update is the sender
                    player = sender.Tag as Player;

                    player.ReadNetworkPacket(packetReader, gameTime, latency);
                }
                else if (header == (byte)MessageType.CapturedEnemy)
                {
                    //This player just told us they captured an enemy
                    player = sender.Tag as Player;

                    player.Score = packetReader.ReadInt32();
                    byte enemyIndex = packetReader.ReadByte();
                    enemies[enemyIndex].Position = packetReader.ReadVector3();
                }
                else if (header == (byte)MessageType.WasCaptured)
                {
                    //This player just told us they were captured
                    player = sender.Tag as Player;

                    player.Score = packetReader.ReadInt32();
                    player.SetPosition(packetReader.ReadVector3());
                }
            }
        }

        private void MoveRandomly(int enemyIndex, GameTime gameTime)
        {
            Enemy enemy = enemies[enemyIndex];

            if (enemy.ChangeDirectionTimer == 0)
                enemy.ChangeDirectionTimer =
                    (float)gameTime.TotalGameTime.TotalMilliseconds;

            //has the appropriate amount of time passed?
            if (gameTime.TotalGameTime.TotalMilliseconds >
                enemy.ChangeDirectionTimer + enemy.RandomSeconds * 1000)
            {
                enemy.RandomVelocity = Vector3.Zero;
                enemy.RandomVelocity.X = rand.Next(-1, 2);
                enemy.RandomVelocity.Y = rand.Next(-1, 2);
                //restrict to 2D?
                if (!RestrictToXY)
                    enemy.RandomVelocity.Z = rand.Next(-1, 2);

                enemy.ChangeDirectionTimer = 0;
            }

            enemy.Velocity = enemy.RandomVelocity;

            enemy.Velocity *= moveUnit;

            if (enemy.Health < 5)
                enemy.Color = Color.LightBlue;
            else
                enemy.Color = Color.Pink;

            float distance = 0;
            Vector3 tv = Vector3.Zero;
            if (OurGame.NetworkSession == null)
            {
                tv = singlePlayer.Position - enemy.Position;
                distance = tv.Length();
            }
            else
            {
                //Handle Multiple Players
                Vector3 closestTrackingVector = Vector3.Zero;
                foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                {
                    Player player = g.Tag as Player;

                    tv = player.Position - enemy.Position;

                    if (closestTrackingVector == Vector3.Zero)
                        closestTrackingVector = tv;
                    else
                    {
                        if (tv.Length() < closestTrackingVector.Length())
                            closestTrackingVector = tv;
                    }
                }

                tv = closestTrackingVector;
                distance = tv.Length();
            }
            
            if (distance < EvadeRadius)
                if (enemy.Health < 5)
                    enemy.State = Enemy.AIState.Retreat;

            if (distance < SeekRadius)
                if (enemy.Health >= 5)
                    enemy.State = Enemy.AIState.Attack;
        }

        private void TrackPlayer(int enemyIndex)
        {
            Enemy enemy = enemies[enemyIndex];

            Vector3 tv = Vector3.Zero;
            float distance = 0;

            if (OurGame.NetworkSession == null)
            {
                tv = singlePlayer.Position - enemy.Position;
                distance = tv.Length();
            }
            else
            {
                //Handle Multiple Players
                Vector3 closestTrackingVector = Vector3.Zero;
                foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                {
                    Player player = g.Tag as Player;

                    tv = player.Position - enemy.Position;

                    if (closestTrackingVector == Vector3.Zero)
                        closestTrackingVector = tv;
                    else
                    {
                        if (tv.Length() < closestTrackingVector.Length())
                            closestTrackingVector = tv;
                    }

                }
                tv = closestTrackingVector;
                distance = tv.Length();
            }

            //after all vectors are checked, we need to normalize for velocity
            tv.Normalize();
            enemy.Velocity = tv * moveUnit;

            enemy.Color = Color.Red;

            if (distance > SeekRadius * 1.25f)
                enemy.State = Enemy.AIState.Search;
        }

        private void EvadePlayer(int enemyIndex)
        {
            Enemy enemy = enemies[enemyIndex];

            float distance = 0;
            Vector3 tv = Vector3.Zero;

            if (OurGame.NetworkSession == null)
            {
                tv = enemy.Position - singlePlayer.Position;
                distance = tv.Length();
            }
            else
            {
                //Handle Multiple Players
                Vector3 closestTrackingVector = Vector3.Zero;
                foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                {
                    Player player = g.Tag as Player;

                    tv = enemy.Position - player.Position;

                    if (closestTrackingVector == Vector3.Zero)
                        closestTrackingVector = tv;
                    else
                    {
                        if (tv.Length() < closestTrackingVector.Length())
                            closestTrackingVector = tv;
                    }

                }
                tv = closestTrackingVector;
                distance = tv.Length();
            }

            //after all vectors are checked, we need to normalize for velocity
            tv.Normalize();
            enemy.Velocity = tv * moveUnit;

            enemy.Color = Color.Navy;

            if (distance > EvadeRadius * 1.25f)
                enemy.State = Enemy.AIState.Search;
        }

        private void CreateEnemies()
        {
            for (int i = 0; i < MaxEnemies; i++)
            {
                enemies[i] = new Enemy();
                enemies[i].Position = new Vector3((i * 50) + 50, (i * 25) - 50, -300);
                enemies[i].Velocity = Vector3.Zero;
                enemies[i].ChangeDirectionTimer = 0;
                enemies[i].RandomVelocity = Vector3.Left;
                enemies[i].RandomSeconds = (i + 2) / 2;
                enemies[i].State = Enemy.AIState.Search;
                //alternate every other enemy to either attack or evade (based on health)
                if (i % 2 == 0)
                    enemies[i].Health = 1;
                else
                    enemies[i].Health = 10;
            }
        }
        
        public void StartGame(int numberOfPlayers)
        {
            this.numberOfPlayers = numberOfPlayers;

            SetupGame();

            if (numberOfPlayers == 1)
            {
                //Do Game Initization tasks 
                //i.e. Shuffling cards, setting up enemies

                CreateEnemies();
            }
            else
            {
                if (OurGame.NetworkSession.IsHost)
                    CreateEnemies();
            }

            SetPresenceInformation();
        }

        private void SetupGame()
        {
            storedTime = null;

            colorIndex = 0;
            colors = new Dictionary<byte, Color>(4);

            defaultColors = new Color[4];
            defaultColors[0] = Color.Green;
            defaultColors[1] = Color.Purple;
            defaultColors[2] = Color.OliveDrab;
            defaultColors[3] = Color.Black;

            if (OurGame.NetworkSession == null) //single player mode
            {
                singlePlayer = new Player(Game);
                singlePlayer.Color = Color.Black;
            }
            else
            {
                //Handle Multiplayer
                foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                {
                    Player player = new Player(Game);

                    if (gamer.IsLocal)
                        player.PlayerIndex = ((LocalNetworkGamer)gamer).SignedInGamer.PlayerIndex;

                    gamer.Tag = player;
                }

                //Find host and tell players their color
                if (OurGame.NetworkSession.IsHost)
                {
                    foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                    {
                        Player player = gamer.Tag as Player;

                        byte assignedColorIndex = SetPlayerColor(player, gamer);

                        packetWriter.Write((byte)MessageType.Color);
                        packetWriter.Write(gamer.Id);
                        packetWriter.Write(assignedColorIndex);

                        ((LocalNetworkGamer)OurGame.NetworkSession.Host).SendData(
                            packetWriter, SendDataOptions.Reliable);
                    }
                }
            }
        }

        private byte SetPlayerColor(Player player, NetworkGamer gamer)
        {
            //color already set?
            if (colors.ContainsKey(gamer.Id))
            {
                Color c = colors[gamer.Id];

                byte index = GetIndexFromColor(c);

                return (index);
            }

            Color color;

            do
            {
                color = defaultColors[(byte)++colorIndex];
                
                if (colorIndex > 3)
                    colorIndex = 0;

            } while (colors.ContainsValue(color));

            player.Color = color;

            colors.Add(gamer.Id, player.Color);

            return ((byte)colorIndex);
        }

        private byte GetIndexFromColor(Color c)
        {
            for (byte i = 0; i < defaultColors.Length; i++)
            {
                if (defaultColors[i] == c)
                    return (i);
            }

            throw new ApplicationException("Color " + c.ToString() + " is not a valid default color.");
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            
            if ((OurGame.NetworkSession != null) && 
                (OurGame.NetworkSession.LocalGamers.Count > 1))
            {
                GraphicsDevice.Viewport = camera2.Viewport;
                DrawScene(gameTime, camera2);

                //prepare viewport for main camera
                GraphicsDevice.Viewport = camera.Viewport;
            }
            

            DrawScene(gameTime, camera);

            GraphicsDevice.Viewport = OurGame.FullscreenViewport;

            //Uncomment to see bytes per second sent and received
            //if (OurGame.NetworkSession != null)
            //    OurGame.SpriteBatch.DrawString(OurGame.Font, "Received: " + OurGame.NetworkSession.BytesPerSecondReceived + "\n" +
            //        "Sent: " + OurGame.NetworkSession.BytesPerSecondSent, new Vector2(TitleSafeArea.X, 400), Color.Red, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);

            base.Draw(gameTime);
        }

        private void DrawScene(GameTime gameTime, Camera cam)
        {
            OurGame.SpriteBatch.Begin();

            skybox.Draw(cam.View, cam.Projection, Matrix.CreateScale(OurGame.ArenaSize));

            //Draw enemies
            for (int i = 0; i < MaxEnemies; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy != null)
                    DrawModel(ref sphere, cam, enemy.Color, enemy.Position);
            }

            if (OurGame.NetworkSession == null)
            {
                //Draw single player
                if (singlePlayer != null)
                {
                    SetCameraProperties(cam, singlePlayer);

                    DrawModel(ref sphere, cam, singlePlayer.Color, singlePlayer.Position);

                    string gamertag = SignedInGamer.SignedInGamers[PlayerIndexInControl.Value].Gamertag;
                    OurGame.SpriteBatch.DrawString(OurGame.Font, gamertag + ": " + singlePlayer.Score.ToString(), new Vector2(151, 201), Color.Black);
                    OurGame.SpriteBatch.DrawString(OurGame.Font, gamertag + ": " + singlePlayer.Score.ToString(), new Vector2(150, 200), singlePlayer.Color);
                }
            }
            else
            {
                //Handle Multiple Players
                int y = 0;
                //draw all players
                foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                {
                    Player player = gamer.Tag as Player;

                    // Draw the player.
                    if (gamer.IsLocal)
                        if (((LocalNetworkGamer)gamer).SignedInGamer.PlayerIndex == cam.PlayerIndex)
                            SetCameraProperties(cam, player);

                    DrawModel(ref sphere, cam, player.Color, player.Position);

                    OurGame.SpriteBatch.DrawString(OurGame.Font, gamer.Gamertag + ": " + player.Score.ToString(), new Vector2(151, 201 + (y * 60)), Color.Black);
                    OurGame.SpriteBatch.DrawString(OurGame.Font, gamer.Gamertag + ": " + player.Score.ToString(), new Vector2(150, 200 + (y * 60)), player.Color);
                    y++;
                }
            }
            //Show the timer
            string timeText;
            if (currentTime.Seconds < 0)
                timeText = "Time: " + currentTime.TotalSeconds.ToString("00"); //show negative total seconds
            else  //show time in minute : seconds when positive
                timeText = "Time: " + currentTime.Minutes.ToString("0") + ":" + currentTime.Seconds.ToString("00");


            timeTextShadowPosition = new Vector2(TitleSafeArea.X, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 2);
            timeTextPosition = new Vector2(TitleSafeArea.X + 1.0f, TitleSafeArea.Y +
                OurGame.Font.LineSpacing * 2 + 1.0f);

            OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                timeTextShadowPosition, Color.Black);
            OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                timeTextPosition, Color.Firebrick);

            OurGame.SpriteBatch.End();
        }
        
        private void SetCameraProperties(Camera cam, Player player)
        {
            cam.Position.X = player.Position.X;
            cam.Position.Y = player.Position.Y + 10;
            cam.Position.Z = player.CurrentZ;

            if (cam.Position.X >= OurGame.ArenaSize - 20)
                cam.Position.X = OurGame.ArenaSize - 20;
            if (cam.Position.X <= -OurGame.ArenaSize + 20)
                cam.Position.X = -OurGame.ArenaSize + 20;

            if (cam.Position.Y >= OurGame.ArenaSize - 20)
                cam.Position.Y = OurGame.ArenaSize - 20;
            if (cam.Position.Y <= -OurGame.ArenaSize + 20)
                cam.Position.Y = -OurGame.ArenaSize + 20;

            if (cam.Position.Z >= OurGame.ArenaSize - 20)
                cam.Position.Z = OurGame.ArenaSize - 20;
            if (cam.Position.Z <= player.Position.Z + 20)
                cam.Position.Z = player.Position.Z + 20;

            player.CurrentZ = cam.Position.Z;

            cam.Target = player.Position;
        }

        public void DrawModel(ref Model m, Camera cam, Color color, Vector3 position)
        {
            Matrix world = Matrix.CreateTranslation(position);

            Matrix[] transforms = new Matrix[m.Bones.Count];
            m.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in m.Meshes)
            {
                foreach (BasicEffect be in mesh.Effects)
                {
                    be.EnableDefaultLighting();

                    be.AmbientLightColor = Color.Silver.ToVector3();
                    be.DiffuseColor = color.ToVector3();
                    be.Projection = cam.Projection;
                    be.View = cam.View;
                    be.World = world * mesh.ParentBone.Transform;
                }

                mesh.Draw();
            }
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

                if (camera2 != null)
                {
                    Game.Components.Remove(camera2);
                    camera2 = null;
                }

                if (OurGame.NetworkSession != null)
                {
                    if (OurGame.NetworkSession.LocalGamers.Count > 1)
                    {
                        leftViewport = OurGame.FullscreenViewport;
                        rightViewport = OurGame.FullscreenViewport;

                        leftViewport.Width = leftViewport.Width / 2;

                        rightViewport.X = leftViewport.Width + 1;
                        rightViewport.Width = (rightViewport.Width / 2) - 1;

                        GraphicsDevice.Viewport = leftViewport;
                        camera.Viewport = leftViewport;
                        camera.PlayerIndex = SignedInGamer.SignedInGamers[0].PlayerIndex;
                        
                        camera2 = new Camera(Game);
                        GraphicsDevice.Viewport = rightViewport;
                        camera2.Viewport = rightViewport;
                        camera2.Position = camera.Position;
                        camera2.Orientation = camera.Orientation;
                        camera2.PlayerIndex = SignedInGamer.SignedInGamers[1].PlayerIndex;
                        Game.Components.Add(camera2);
                    }
                }
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

            sphere = Content.Load<Model>(@"Models\sphere0");
            skybox = Content.Load<Skybox>(@"Skyboxes\skybox2");

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
                Player player = gamer.Tag as Player;
                packetWriter.Write((byte)MessageType.RequestToJoinInProgressGame);
                ((LocalNetworkGamer)gamer).SendData(packetWriter, SendDataOptions.Reliable);
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
            get { return (singlePlayer.Score); }
        }
    }
}
