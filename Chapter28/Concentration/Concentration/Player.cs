using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using XELibrary;

namespace Concentration
{
    public class Player
    {
        private PlayerIndex playerIndex;
        private bool visible = true;
        private bool enabled = true;

        private Concentration ourGame;
        private InputHandler input;
        private PlayingState playingState;

        private Texture2D highlightCard;
        private byte highlightedCard = 0;

        public int Score = 0;

        public byte HighlightedCard
        {
            get { return (highlightedCard); }
            set
            {
                highlightedCard = value;
            }
        }

        public Player(Game game)
            : base()
        {
            input = (InputHandler)game.Services.GetService(
               typeof(IInputHandler));

            playingState = (PlayingState)game.Services.GetService(
                typeof(IPlayingState));

            ourGame = (Concentration)game;

            LoadContent();
            Visible = false;
            Enabled = false;
        }

        public byte HandleInput()
        {
            PlayerIndex throwAway;
            if (input.ButtonHandler.WasButtonPressed(playerIndex,
                    Buttons.LeftThumbstickLeft, out throwAway))
                HighlightLeft();

            if (input.GamePads[(int)playerIndex].Triggers.Left > 0)
                HighlightLeft();

            if (input.ButtonHandler.WasButtonPressed(playerIndex,
                    Buttons.LeftThumbstickRight, out throwAway))
                HighlightRight();

            if (input.GamePads[(int)playerIndex].Triggers.Right > 0)
                HighlightRight();

            if (input.ButtonHandler.WasButtonPressed(playerIndex, Buttons.A,
                out throwAway))
            {
                playingState.SelectCard(this, highlightedCard);
            }

            return (highlightedCard);
        }

        private void HighlightRight()
        {
            do
            {
                highlightedCard++;

                if (highlightedCard >= playingState.Cards.Length)
                    highlightedCard = 0;

            } while (!playingState.Cards[highlightedCard].IsVisible);
        }

        private void HighlightLeft()
        {
            do
            {
                highlightedCard--;

                if (highlightedCard == 255)
                    highlightedCard = (byte)(playingState.Cards.Length - 1);

            } while (!playingState.Cards[highlightedCard].IsVisible);
        }

        public void DrawHighlightedCard()
        {
            if (visible)
            {
                ourGame.SpriteBatch.Draw(highlightCard, playingState.Cards[highlightedCard].Location.ToVector2(), Color.Yellow);
            }
        }

        public void LoadContent()
        {
            highlightCard = ourGame.Content.Load<Texture2D>(@"Textures\highlightCard");
        }

        public PlayerIndex PlayerIndex
        {
            get { return (playerIndex); }
            set { playerIndex = value; }
        }

        public bool Visible
        {
            get { return (visible); }
            set { visible = value; }
        }

        public bool Enabled
        {
            get { return (enabled); }
            set { enabled = value; }
        }
    }
}