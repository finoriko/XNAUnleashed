using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using XELibrary;

namespace Concentration
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

        //Game Specific
        private const int NumberOfCards = 20;
        private const int MaxCardsAcross = 5;
        private const int HorizontalCardSpacing = 100;
        private const int VerticalCardSpacing = 170;
        private const int HorizontalCardOffset = 400;
        private const int VerticalCardOffset = 30;

        public const int cardWidth = 96;
        public const int cardHeight = 136;

        public Card[] Cards = new Card[NumberOfCards];
        private byte[] selectedCards;
        private bool selectionChanged = false;
        private byte prevHighlightedCard;
        private double waitTime;
        private int millisecondsToShowCards = 2000;

        private List<Player> players;
        private byte prevPlayer;
        private byte currentPlayer;

        private CelAnimationManager cam;

        private PacketWriter packetWriter = new PacketWriter();
        private PacketReader packetReader = new PacketReader();

        private Texture2D background;

        private int framesBetweenPackets = 6;
        private int framesSinceLastSend;

        private enum MessageType : byte { StartGame, RequestToJoinInProgressGame,
            JoinedInProgressGame, HighlightedCard, SelectedCards, StartCheckCardsTimer,            
            Go, DeclareWinner };

        public PlayingState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(IPlayingState), this);

            cam = new CelAnimationManager(game, @"Textures\");
            game.Components.Add(cam);
        }

        public override void Initialize()
        {
            for (int i = 0; i < NumberOfCards; i++)
            {
                Cards[i] = new Card();
                Cards[i].Value = (byte)((i + 2) / 2);
                Cards[i].IsFaceUp = false;

                Cards[i].Location = new HalfVector2(
                    HorizontalCardOffset + (i % MaxCardsAcross) * HorizontalCardSpacing,
                    VerticalCardOffset + (i / MaxCardsAcross) * VerticalCardSpacing);
            }

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (OurGame.NetworkSession == null)
            {
                if (waitTime == 0 && ReadyToCheckCards)
                    waitTime = gameTime.TotalGameTime.TotalMilliseconds;

                if (ReadyToCheckCards && gameTime.TotalGameTime.TotalMilliseconds > waitTime + millisecondsToShowCards)
                    CheckCards(true); //single player acts like host

                if (players[currentPlayer].Enabled)
                {
                    players[currentPlayer].PlayerIndex = PlayerIndexInControl.Value;
                    players[currentPlayer].HandleInput();
                }
            }
            else
                UpdateNetworkSession(gameTime);


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
                UpdateLocalGamer(gamer, gameTime);
            }

            //Do actual sending and receiving of network packets
            try
            {
                if (sendPacketThisFrame)
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
        }

        private void UpdateLocalGamer(LocalNetworkGamer gamer, GameTime gameTime)
        {
            //Look up what player is associated with this local player.
            byte playerIndex = (byte)gamer.Tag;

            if (prevPlayer != currentPlayer)
            {
                if (gamer.IsHost) //Go is only sent by host
                {
                    packetWriter.Write((byte)MessageType.Go);
                    packetWriter.Write(currentPlayer);

                    //Send the data to everyone in the session
                    gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);
                    //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " sent " + MessageType.Go.ToString());
                }

                prevPlayer = currentPlayer;
            }

            //check to see if we should have each player check their card
            if (waitTime == 0 && ReadyToCheckCards)
            {
                waitTime = gameTime.TotalGameTime.TotalMilliseconds;
            }

            //still waiting to check cards and enough time has elapsed?
            if (ReadyToCheckCards && gameTime.TotalGameTime.TotalMilliseconds > 
                                                      waitTime + millisecondsToShowCards)
            {
                CheckCards(gamer.IsHost);
            }

            if (selectionChanged) //selection of cards is sent by each peer
            {
                packetWriter.Write((byte)MessageType.SelectedCards);
                packetWriter.Write(selectedCards);

                gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);

                //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " sent " + MessageType.SelectedCards.ToString());

                selectionChanged = false;
            }


            //For concentration we disable input if not our turn
            if (players[playerIndex].Enabled)
            {
                //Only send data if something actually changed
                byte currHighlightedCard = players[playerIndex].HandleInput();
                
                if (prevHighlightedCard != currHighlightedCard) //highlighted changes are sent by each peer
                {
                    //Write the player state into a network packet
                    packetWriter.Write((byte)MessageType.HighlightedCard);
                    packetWriter.Write(currHighlightedCard);

                    //Send the data to everyone in the session
                    gamer.SendData(packetWriter, SendDataOptions.InOrder); //highlighting is not important
                    //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " sent " + MessageType.HighlightedCard.ToString());

                    prevHighlightedCard = currHighlightedCard;
                }
            }
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

                //Get the player index
                byte playerIndex = (byte)gamer.Tag;

                //Determine the type of message this is
                byte header = packetReader.ReadByte();

                //System.Diagnostics.Debug.WriteLine(gamer.Gamertag + " received " + ((MessageType)header).ToString());

                if (header == (byte)MessageType.StartGame)
                {
                    for (int p = 0; p < NumberOfCards; p++)
                    {
                        Cards[p].Value = packetReader.ReadByte();
                        Cards[p].Location.PackedValue = packetReader.ReadUInt32();
                    }

                    //Make sure everyone has the right starting player
                    currentPlayer = packetReader.ReadByte();
                    prevPlayer = currentPlayer;
                }
                else if (header == (byte)MessageType.DeclareWinner)
                {
                    byte numberOfHighScores = packetReader.ReadByte();
                    List<int> highScoreIndices = new List<int>(numberOfHighScores);
                    for (byte i = 0; i < numberOfHighScores; i++)
                        highScoreIndices.Add(packetReader.ReadByte());

                    bool localIsAWinner = false;
                    foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                    {
                        if (g.IsLocal)
                        {
                            //Did this local machine win?
                            //check to see if this players index is in the high score index list
                            if (highScoreIndices.Contains((byte)gamer.Tag))
                            {
                                localIsAWinner = true;

                                OurGame.WonGameState.Gamertag = gamer.Gamertag;
                                break;
                            }
                        }
                    }

                    if (localIsAWinner)
                        GameManager.ChangeState(OurGame.WonGameState.Value, null);
                    else
                        GameManager.ChangeState(OurGame.LostGameState.Value, null);
                }
                else if (header == (byte)MessageType.RequestToJoinInProgressGame)
                {   //Sent by person joining game in progress
                    if (gamer.IsHost)
                    {
                        packetWriter.Write((byte)MessageType.JoinedInProgressGame);

                        for (int p = 0; p < NumberOfCards; p++)
                        {
                            packetWriter.Write(Cards[p].Value);
                            packetWriter.Write(Cards[p].Location.PackedValue);
                            packetWriter.Write(Cards[p].IsFaceUp);
                            packetWriter.Write(Cards[p].IsVisible);
                        }

                        for (int i = 0; i < players.Count; i++)
                        {
                            packetWriter.Write((byte)players[i].Score);
                        }

                        packetWriter.Write(selectedCards);

                        packetWriter.Write(currentPlayer);

                        gamer.SendData(packetWriter, SendDataOptions.Reliable, sender);
                    }
                }
                else if (header == (byte)MessageType.JoinedInProgressGame)
                {   //received by person joining game in progress
                    for (int p = 0; p < NumberOfCards; p++)
                    {
                        Cards[p].Value = packetReader.ReadByte();
                        Cards[p].Location.PackedValue = packetReader.ReadUInt32();
                        Cards[p].IsFaceUp = packetReader.ReadBoolean();
                        Cards[p].IsVisible = packetReader.ReadBoolean();
                    }

                    for (int i = 0; i < players.Count; i++)
                    {
                        players[i].Score = packetReader.ReadByte();
                    }

                    selectedCards = packetReader.ReadBytes(2);
                    waitTime = 0;

                    currentPlayer = packetReader.ReadByte();
                }
                else if (header == (byte)MessageType.HighlightedCard)
                {
                    players[currentPlayer].HighlightedCard = packetReader.ReadByte();
                    players[currentPlayer].Visible = true;
                }
                else if (header == (byte)MessageType.SelectedCards)
                {
                    byte[] selected = new byte[2];
                    selected = packetReader.ReadBytes(2);

                    if (selectedCards[0] != 255 && selected[0] != selectedCards[0])
                    {
                        //We haven't finished the first selection yet
                        if (selectedCards[1] != 255 && selected[1] != selectedCards[1])
                        {
                            //below should only happen on really bad network connections
                            //timer hasn't ran out yet, force a call to Check Cards
                            // after waiting up to 2 seconds
                            while (gameTime.TotalGameTime.TotalMilliseconds > waitTime + millisecondsToShowCards)
                                waitTime = gameTime.TotalGameTime.TotalMilliseconds;

                            CheckCards(false); //we are not the host since we were just told to Go
                        }
                    }

                    selectedCards = selected;

                    if (selectedCards[0] != 255)
                        Cards[selectedCards[0]].IsFaceUp = true;
                    if (selectedCards[1] != 255)
                    {
                        Cards[selectedCards[1]].IsFaceUp = true;
                        players[currentPlayer].Visible = false;

                        //both cards selected, reset timer
                        waitTime = 0;
                    }

                    //don't do selection Change logic in UpdateLocalGamer
                    selectionChanged = false;
                }
                else if (header == (byte)MessageType.Go)
                {
                    //below should only happen on really bad network connections
                    //timer hasn't ran out yet, force a call to Check Cards
                    if (ReadyToCheckCards && gameTime.TotalGameTime.TotalMilliseconds < waitTime + millisecondsToShowCards)
                    {
                        // just wait up to 2 seconds
                        while (gameTime.TotalGameTime.TotalMilliseconds > waitTime + millisecondsToShowCards)
                            waitTime = gameTime.TotalGameTime.TotalMilliseconds;

                        CheckCards(false); //we are not the host since we were just told to Go
                    }
                    
                    //make currentPlayer (before we change it) to not be enabled or drawn
                    players[currentPlayer].Enabled = false;
                    players[currentPlayer].Visible = false;

                    //highlight the player who is now going
                    currentPlayer = packetReader.ReadByte();
                    
                    //make prevPlayer the same so we don't kick off our own Go event!
                    prevPlayer = currentPlayer;

                    players[currentPlayer].Enabled = true;
                    players[currentPlayer].Visible = true;
                }
            }
        }

        private void FindWinner()
        {
            int highScore = players[0].Score;

            //Find most pairs
            for (int i = 1; i < numberOfPlayers; i++)
            {
                if (players[i].Score > highScore)
                    highScore = players[i].Score;
            }

            //check for any players that have same as highest player
            List<int> highScoreIndices = new List<int>();
            for (int i = 0; i < numberOfPlayers; i++)
            {
                if (players[i].Score == highScore)
                    highScoreIndices.Add(i);
            }

            //Make sure our gamertag is set to empty so it will have 
            //the right value ("You") for check
            OurGame.WonGameState.Gamertag = string.Empty;

            bool localIsAWinner = false;
            foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
            {
                if (gamer.IsLocal)
                {
                    //Did this local machine win?
                    //check to see if this players index is in the high score index list
                    if (highScoreIndices.Contains((byte)gamer.Tag))
                    {
                        localIsAWinner = true;

                        //It is possible to have more than one winner on a local multiplayer
                        //for network multiplayer, each screen will get own gamer tag "won"
                        //but with a shared screen, we need to display ".. and .." 
                        if (OurGame.WonGameState.Gamertag == "You")
                            OurGame.WonGameState.Gamertag = gamer.Gamertag;
                        else
                            OurGame.WonGameState.Gamertag += " and " + gamer.Gamertag;
                    }
                }
            }

            foreach (LocalNetworkGamer gamer in OurGame.NetworkSession.LocalGamers)
            {
                packetWriter.Write((byte)MessageType.DeclareWinner);
                packetWriter.Write((byte)highScoreIndices.Count);
                foreach (int i in highScoreIndices)
                    packetWriter.Write((byte)i);

                //Send the data to everyone in the session
                gamer.SendData(packetWriter, SendDataOptions.Reliable);
            }

            //Force Network Update to send winning information packets
            OurGame.NetworkSession.Update();

            if (localIsAWinner)
                GameManager.PushState(OurGame.WonGameState.Value, null);
            else
                GameManager.PushState(OurGame.LostGameState.Value, null);
        }

        public void StartGame(int numberOfPlayers)
        {
            this.numberOfPlayers = numberOfPlayers;

            SetupGame();

            if (numberOfPlayers == 1)
            {
                ShuffleCards();

                players[0].Enabled = true;
                players[0].Visible = true;
            }
            else
            {
                foreach (LocalNetworkGamer gamer in OurGame.NetworkSession.LocalGamers)
                {
                    if (gamer.IsHost)
                    {
                        ShuffleCards();

                        packetWriter.Write((byte)MessageType.StartGame);
                        for (int p = 0; p < NumberOfCards; p++)
                        {
                            packetWriter.Write(Cards[p].Value);
                            packetWriter.Write(Cards[p].Location.PackedValue);
                        }

                        //Determine who goes first
                        //For now, Host always goes first
                        prevPlayer = 0;
                        currentPlayer = 0;
                        packetWriter.Write(currentPlayer);

                        gamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);

                        players[currentPlayer].Enabled = true;
                        players[currentPlayer].Visible = true;

                        break;
                    }
                }
            }

            SetPresenceInformation();
        }

        private void ShuffleCards()
        {
            Random r = new Random();

            byte tmp; //place holder
            int ri; //random index
            for (int t = 0; t < 10; t++) //shuffle 10 times
            {
                for (int i = 0; i < NumberOfCards; i++)
                {
                    ri = r.Next(NumberOfCards);
                    tmp = Cards[ri].Value; //store value
                    Cards[ri].Value = Cards[i].Value; //swap
                    Cards[i].Value = tmp; //get stored value
                }
            }
        }

        private void SetupGame()
        {
            singlePlayerScore = 0;
            storedTime = null;

            currentPlayer = 0;
            selectionChanged = false;
            selectedCards = new byte[2] { 255, 255 };

            if (OurGame.NetworkSession == null) //single player mode
            {
                players = new List<Player>(1);
                players.Add(new Player(Game));
                players[0].PlayerIndex = PlayerIndexInControl.Value;
            }
            else
            {
                //set capacity to the max gamers we can possibly have
                players = new List<Player>(OurGame.NetworkSession.MaxGamers);

                byte playerIndex = 255;
                //Initialize the players collection
                foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                {
                    if (gamer.Tag != null)
                    {
                        playerIndex = (byte)gamer.Tag;
                    }
                    else
                    {
                        playerIndex++;
                        gamer.Tag = playerIndex;
                    }

                    players.Add(new Player(Game));
                    players[playerIndex].Enabled = false;
                    players[playerIndex].Visible = false;
                    //We need to set the player's controller index if they are local
                    if (gamer.IsLocal)
                    {
                        players[playerIndex].PlayerIndex =
                            ((LocalNetworkGamer)gamer).SignedInGamer.PlayerIndex;
                    }
                }
            }
        }

        public void SelectCard(Player p, byte cardIndex)
        {
            if (Cards[cardIndex].IsFaceUp)
                return; //don't do anything

            if (selectedCards[0] == 255)
            {
                selectedCards[0] = cardIndex;
                Cards[cardIndex].IsFaceUp = true;
            }
            else
            {
                selectedCards[1] = cardIndex;
                Cards[cardIndex].IsFaceUp = true;

                //don't let the player do anything while cards 
                //are being displayed
                p.Enabled = false;
                //also remove highlighted texture
                p.Visible = false;

                waitTime = 0;
            }

            selectionChanged = true;
        }

        private bool IsGameFinished
        {
            get
            {
                bool gameIsFinished = true;
                for (int i = 0; i < Cards.Length; i++)
                {
                    //auto skip if the cards we are checking are still visible
                    if (selectedCards[0] == i || selectedCards[1] == i)
                        continue;

                    //see if this card is visible
                    if (Cards[i].IsVisible)
                    {
                        //if any card is visible then we
                        //are still playing the game.
                        gameIsFinished = false;
                        break;
                    }
                }
                return gameIsFinished;
            }
        }

        private void CheckCards(bool isHost)
        {
            bool gameIsFinished = false;

            if (Cards[selectedCards[0]].Value == Cards[selectedCards[1]].Value)
            {
                //match
                Cards[selectedCards[0]].IsVisible = false;
                Cards[selectedCards[1]].IsVisible = false;

                //Score based off single player or multiplayer
                if (OurGame.NetworkSession == null)
                {
                    singlePlayerScore += 100;

                    //Set the single player's score
                    foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
                    {
                        if (signedInGamer.PlayerIndex == players[currentPlayer].PlayerIndex)
                        {
                            signedInGamer.Presence.PresenceMode = GamerPresenceMode.Score;
                            signedInGamer.Presence.PresenceValue = singlePlayerScore;
                        }
                    }
                }
                else
                {
                    players[currentPlayer].Score += 1;
                }

                //input was disabled while the cards were checked ...
                //re-enable it if we aren’t done with the game
                gameIsFinished = IsGameFinished;
                if (!gameIsFinished)
                    players[currentPlayer].Enabled = true;
            }
            else
            {
                //No match
                Cards[selectedCards[0]].IsFaceUp = false;
                Cards[selectedCards[1]].IsFaceUp = false;

                if (isHost)
                    GetNextPlayer();
            }

            selectedCards[0] = selectedCards[1] = 255;

            //Make sure highlight texture is visible
            if (!gameIsFinished)
                players[currentPlayer].Visible = true;

            if (OurGame.NetworkSession == null && gameIsFinished)
            {
                if (currentTime.Seconds > 0) //add 100 for every positive second
                    singlePlayerScore += ((int)currentTime.TotalSeconds * 100);
                else //subtract 10 for every negative second
                    singlePlayerScore += ((int)currentTime.TotalSeconds * 10);

                GameManager.PushState(OurGame.WonGameState.Value, PlayerIndexInControl);
            }
            else if (gameIsFinished) //network session isn't null, and game is over
            {
                //Find Winner and End Game
                FindWinner();
            }
        }

        private void GetNextPlayer()
        {
            if (currentPlayer <= players.Count)
            {
                players[currentPlayer].Enabled = false;
                players[currentPlayer].Visible = false;
            }

            if (numberOfPlayers > 1)
            {
                //End this players Turn                
                currentPlayer++;
                if (currentPlayer >= OurGame.NetworkSession.AllGamers.Count)
                    currentPlayer = 0;
            }

            //Start next players Turn
            players[currentPlayer].Enabled = true;
            players[currentPlayer].Visible = true;
        }

        public override void Draw(GameTime gameTime)
        {
            //Draw Cards
            OurGame.SpriteBatch.Begin();

            OurGame.SpriteBatch.Draw(background, Vector2.Zero, Color.White);

            for (int i = 0; i < NumberOfCards; i++)
            {
                if (Cards[i].IsVisible)
                {
                    if (Cards[i].IsFaceUp)
                    {
                        cam.Draw(gameTime,
                            Cards[i].Value.ToString(), //get texture value from card
                            OurGame.SpriteBatch,
                            //position the cards in center of screen, 5 across
                            Cards[i].Location.ToVector2());
                    }
                    else
                    {
                        cam.Draw(gameTime,
                            "d3", //deck card texture
                            OurGame.SpriteBatch,
                            //position the cards in center of screen, 5 across
                            Cards[i].Location.ToVector2());
                    }
                }
            }

            players[currentPlayer].DrawHighlightedCard();

            int y = 0;
            if (OurGame.NetworkSession == null)
            {
                //Single player, show individual score
                OurGame.SpriteBatch.DrawString(OurGame.Font, "Score: " + Score.ToString(), new Vector2(TitleSafeArea.X, TitleSafeArea.Y), Color.White);

                //And show the timer
                string timeText;
                if (currentTime.Seconds < 0)
                    timeText = "Time: " + currentTime.TotalSeconds.ToString("00"); //show negative total seconds
                else  //show time in minute : seconds when positive
                    timeText = "Time: " + currentTime.Minutes.ToString("0") + ":" +
                        currentTime.Seconds.ToString("00");

                OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                    timeTextShadowPosition, Color.Black);
                OurGame.SpriteBatch.DrawString(OurGame.Font, timeText,
                    timeTextPosition, Color.Firebrick);
            }
            else
            {
                foreach (NetworkGamer gamer in OurGame.NetworkSession.AllGamers)
                {
                    Player p = players[(byte)gamer.Tag];
                    Color color = Color.White;
                    if ((byte)gamer.Tag == currentPlayer)
                        color = Color.Yellow;
                    OurGame.SpriteBatch.DrawString(OurGame.Font, gamer.Gamertag + ": " + 
                        p.Score.ToString(), new Vector2(TitleSafeArea.X, 200 + (y * 60)),
                        color, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);
                    y++;
                }
            }

            //Uncomment to see the current presence information
            //foreach (SignedInGamer signedInGamer in SignedInGamer.SignedInGamers)
            //    OurGame.SpriteBatch.DrawString(OurGame.Font, signedInGamer.Gamertag + ": " + signedInGamer.Presence.PresenceMode + " " + signedInGamer.Presence.PresenceValue, new Vector2(120, 200 + (y * 60)), Color.White, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);

            //Uncomment to see bytes per second sent and received
            //if (OurGame.NetworkSession != null)
            //    OurGame.SpriteBatch.DrawString(OurGame.Font, "Received: " + OurGame.NetworkSession.BytesPerSecondReceived + "\n" +
            //        "Sent: " + OurGame.NetworkSession.BytesPerSecondSent, new Vector2(TitleSafeArea.X, 400), Color.Red, 0, Vector2.Zero, 2.0f, SpriteEffects.None, 0);

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
            background = Content.Load<Texture2D>(@"Textures\PlayingBackground");

            cam.AddAnimation("1", "ConcentrationCards", new CelRange(1, 1, 1, 1),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("2", "ConcentrationCards", new CelRange(2, 1, 2, 1),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("3", "ConcentrationCards", new CelRange(3, 1, 3, 1),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("4", "ConcentrationCards", new CelRange(4, 1, 4, 1),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("5", "ConcentrationCards", new CelRange(5, 1, 5, 1),
                cardWidth, cardHeight, 1, 1);

            cam.AddAnimation("6", "ConcentrationCards", new CelRange(1, 2, 1, 2),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("7", "ConcentrationCards", new CelRange(2, 2, 2, 2),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("8", "ConcentrationCards", new CelRange(3, 2, 3, 2),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("9", "ConcentrationCards", new CelRange(4, 2, 4, 2), 
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("10", "ConcentrationCards", new CelRange(5, 2, 5, 2),
                cardWidth, cardHeight, 1, 1);

            cam.AddAnimation("d1", "ConcentrationCards", new CelRange(1, 3, 1, 3),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("d2", "ConcentrationCards", new CelRange(2, 3, 2, 3),
                cardWidth, cardHeight, 1, 1);
            cam.AddAnimation("d3", "ConcentrationCards", new CelRange(3, 3, 3, 3),
                cardWidth, cardHeight, 1, 1);

            //No real animations so turn off Update method
            cam.Enabled = false;

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

                int playerThatLeft = 0;
                //possible to return null if gamer was there, but isn't by the time we call this
                if (gamer != null)
                {
                    //Handle the player leaving our game
                    playerThatLeft = (int)gamer.Tag;
                }
                else
                {
                    //We need to determine which gamer left
                    byte[] playersRemaining = new byte[players.Count + 1];

                    //populate a list of all players we currently have
                    for (int i = 0; i < players.Count; i++)
                        playersRemaining[i] = (byte)i;

                    //now loop through and remove all people that still exists

                    //We don't have the actual gamer that left, so let's find it
                    foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                    {
                        for (int i = 0; i < playersRemaining.Length; i++)
                        {
                            //this player still exists so "clear" out the value
                            if (playersRemaining[i] == (byte)g.Tag)
                                playersRemaining[i] = 255;
                        }                        
                    }

                    //now only one non 255 value exists, find it and set that as playerThatLeft
                    for (int i = 0; i < playersRemaining.Length; i++)
                    {
                        if (playersRemaining[i] != 255)
                        {
                            //we found the gamer that left
                            playerThatLeft = playersRemaining[i];
                            break;
                        }
                    }                        
                }

                //Finally remove the player from our list
                players.RemoveAt(playerThatLeft);

                //Reset each gamer's player Index
                byte playerIndex = 0;
                foreach (NetworkGamer g in OurGame.NetworkSession.AllGamers)
                {
                    g.Tag = playerIndex;
                    playerIndex++;
                }

                //If the player that left was the current player, then 
                //change the selected cards to no values and pick next player
                if (currentPlayer == playerThatLeft)
                {
                    //selectedCards[0] = selectedCards[1] = 255;
                    //selectionChanged = true;

                    //Let the host determine who should go next
                    foreach (LocalNetworkGamer localGamer in OurGame.NetworkSession.LocalGamers)
                    {
                        if (localGamer.IsHost)
                        {
                            packetWriter.Write((byte)MessageType.StartGame);
                            for (int p = 0; p < NumberOfCards; p++)
                            {
                                packetWriter.Write(Cards[p].Value);
                                packetWriter.Write(Cards[p].Location.PackedValue);
                            }

                            //Determine who goes next
                            GetNextPlayer();

                            packetWriter.Write(currentPlayer);

                            localGamer.SendData(packetWriter, SendDataOptions.ReliableInOrder);

                            players[currentPlayer].Enabled = true;
                            players[currentPlayer].Visible = true;

                            break;
                        }
                    }
                }

                //set number of players 
                numberOfPlayers = players.Count;
            }
        }
        
        public void JoinInProgressGame(NetworkGamer gamer, int numberOfPlayers)
        {
            this.numberOfPlayers = numberOfPlayers;

            if (gamer.IsLocal)
            {
                SetupGame();

                //Handle local player joining
                //We need to set the player's controller index if they are local
                players[(byte)gamer.Tag].PlayerIndex =
                    ((LocalNetworkGamer)gamer).SignedInGamer.PlayerIndex;

                packetWriter.Write((byte)MessageType.RequestToJoinInProgressGame);

                ((LocalNetworkGamer)gamer).SendData(packetWriter, SendDataOptions.Reliable);
            }
            else
            {
                //Handle remote player joining
                players.Add(new Player(Game));
                players[(byte)gamer.Tag].Enabled = false;
                players[(byte)gamer.Tag].Visible = false;
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

        private bool ReadyToCheckCards
        {
            get { return (selectedCards[0] != 255 && selectedCards[1] != 255); }
        }
    }
}
