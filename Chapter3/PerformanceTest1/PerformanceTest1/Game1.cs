using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace PerformanceTest1
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private float fps;
        private float updateInterval = 1.0f;
        private float timeSinceLastUpdate = 0.0f;
        private float framecount = 0;


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            //Do not synch our Draw method with the Vertical Retrace of our monitor
            graphics.SynchronizeWithVerticalRetrace = false;
            //Do not Call our Update method at the default rate of 1/60 of a second.
            IsFixedTimeStep = false;

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

            // TODO: use this.Content to load your game content here
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

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            //bad code that shoud not be replicated
            Matrix m = Matrix.Identity;
            Vector3 v2;
            for (int i = 0; i < 100000; i++)
            {
                m = Matrix.CreateRotationX(MathHelper.PiOver4);
                m *= Matrix.CreateTranslation(new Vector3(5.0f));

                Vector3 v = m.Translation - Vector3.One;
                v2 = v + Vector3.One;
            }

            //FPS
            float elapsed = (float)gameTime.ElapsedRealTime.TotalSeconds;
            framecount++;
            timeSinceLastUpdate += elapsed;
            if (timeSinceLastUpdate > updateInterval)
            {
                fps = framecount / timeSinceLastUpdate; //mean fps over updateIntrval
#if XBOX360
                System.Diagnostics.Debug.WriteLine("FPS: " + fps.ToString() + " - RT: " +
                gameTime.ElapsedRealTime.TotalSeconds.ToString() + " - GT: " +
                gameTime.ElapsedGameTime.TotalSeconds.ToString());
#else
                Window.Title = "FPS: " + fps.ToString() + " - RT: " +
                    gameTime.ElapsedRealTime.TotalSeconds.ToString() + " - GT: " +
                    gameTime.ElapsedGameTime.TotalSeconds.ToString();
#endif
                framecount = 0;
                timeSinceLastUpdate -= updateInterval;
            }

            base.Draw(gameTime);
        }
    }
}
