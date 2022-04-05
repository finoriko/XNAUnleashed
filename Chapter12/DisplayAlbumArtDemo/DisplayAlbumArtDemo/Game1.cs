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

namespace DisplayAlbumArtDemo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private SpriteFont font; 
        private Texture2D art;
        private ICollection<MediaSource> mediaSources;
        private MediaLibrary mediaLib;
        private AlbumCollection albumCollection;
        private int numAlbumArts;
        private int currentAlbum = -1;
        private int prevAlbum;

        private InputHandler input;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Frame rate is 30 fps by default for Zune.
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 30.0);

            input = new InputHandler(this);
            Components.Add(input);
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

            font = Content.Load<SpriteFont>(@"Fonts\Arial");

            mediaSources = MediaSource.GetAvailableMediaSources();
            foreach (MediaSource ms in mediaSources)
            {
                mediaLib = new MediaLibrary(ms);
                break;
            }
            albumCollection = mediaLib.Albums;
            numAlbumArts = albumCollection.Count - 1;

            do
            {
                currentAlbum++;

                if (currentAlbum >= albumCollection.Count)
                    break;
            } while (!albumCollection[currentAlbum].HasArt);

            if (currentAlbum >= albumCollection.Count)
            {
                //went through all albums and none had art.
                //nothing for the program to do but exit.
                Exit();
            }

            prevAlbum = -1;

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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (input.ButtonHandler.WasButtonPressed(0, InputHandler.ButtonType.DPadRight))
            {
                do
                {
                    if (currentAlbum == numAlbumArts)
                    {
                        currentAlbum = 0;
                    }
                    else
                    {
                        currentAlbum++;
                    }
                } while (!albumCollection[currentAlbum].HasArt);
            }
            else if (input.ButtonHandler.WasButtonPressed(0, InputHandler.ButtonType.DPadLeft))
            {
                do
                {
                    if (currentAlbum == 0)
                    {
                        currentAlbum = numAlbumArts;
                    }
                    else
                    {
                        currentAlbum--;
                    }
                } while (!albumCollection[currentAlbum].HasArt);
            }
            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            DisplayArt();

            base.Draw(gameTime);
        }

        protected void DisplayArt()
        {
            spriteBatch.Begin();

            //only get new pic if needed
            if (currentAlbum != prevAlbum)
            {
                if (art != null)
                    art.Dispose();

                art = albumCollection[currentAlbum].GetAlbumArt(this.Services);
            }

            spriteBatch.Draw(art, Vector2.Zero, Color.White);

            spriteBatch.DrawString(font, currentAlbum.ToString(),
                new Vector2(110, 300), Color.Black);
            spriteBatch.DrawString(font, currentAlbum.ToString(),
                new Vector2(111, 301), Color.White);

            spriteBatch.End();

            prevAlbum = currentAlbum;
        }


    }
}
