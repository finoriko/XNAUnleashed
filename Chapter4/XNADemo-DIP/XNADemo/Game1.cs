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

namespace XNADemo
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Matrix projection;
        private Matrix view;

        private Vector3 cameraPosition = new Vector3(0.0f, 0.0f, 3.0f);
        private Vector3 cameraTarget = Vector3.Zero;
        private Vector3 cameraUpVector = Vector3.Up;

        private VertexPositionNormalTexture[] vertices;

        private Texture2D texture;

        private short[] indices;

        private FPS fps;

        private BasicEffect effect;

        private VertexDeclaration vertexDeclaration;

        private VertexBuffer vertexBuffer;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

#if DEBUG
            fps = new FPS(this);
#else
            fps = new FPS(this, true, true, this.TargetElapsedTime);
#endif
            Components.Add(fps);
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

            InitializeCamera();

            base.Initialize();
        }

        private void InitializeCamera()
        {
            //Projection
            float aspectRatio = (float)graphics.GraphicsDevice.Viewport.Width /
                (float)graphics.GraphicsDevice.Viewport.Height;
            Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio,
                0.0001f, 1000.0f, out projection);

            //View
            Matrix.CreateLookAt(ref cameraPosition, ref cameraTarget,
                ref cameraUpVector, out view);
        }

        private void InitializeVertices()
        {
            Vector3 position;
            Vector2 textureCoordinates;

            vertices = new VertexPositionNormalTexture[4];

            //top left
            position = new Vector3(-1, 1, 0);
            textureCoordinates = new Vector2(0, 0);
            vertices[0] = new VertexPositionNormalTexture(position, Vector3.Forward,
                textureCoordinates);

            //bottom right
            position = new Vector3(1, -1, 0);
            textureCoordinates = new Vector2(1, 1);
            vertices[1] = new VertexPositionNormalTexture(position, Vector3.Forward,
                textureCoordinates);

            //bottom left
            position = new Vector3(-1, -1, 0);
            textureCoordinates = new Vector2(0, 1);
            vertices[2] = new VertexPositionNormalTexture(position, Vector3.Forward,
                textureCoordinates);

            //top right
            position = new Vector3(1, 1, 0);
            textureCoordinates = new Vector2(1, 0);
            vertices[3] = new VertexPositionNormalTexture(position, Vector3.Forward,
                textureCoordinates);

            vertexBuffer = new VertexBuffer(graphics.GraphicsDevice,
                VertexPositionNormalTexture.SizeInBytes * vertices.Length,
                BufferUsage.WriteOnly);

            vertexBuffer.SetData(vertices);

            graphics.GraphicsDevice.Vertices[0].SetSource(vertexBuffer, 0,
                VertexPositionNormalTexture.SizeInBytes);
        }

        private void InitializeIndices()
        {
            //6 vertices make up 2 triangles which make up our rectangle
            indices = new short[6];

            //triangle 1 (bottom portion)
            indices[0] = 0; // top left
            indices[1] = 1; // bottom right
            indices[2] = 2; // bottom left

            //triangle 2 (top portion)
            indices[3] = 0; // top left
            indices[4] = 3; // top right
            indices[5] = 1; // bottom right

            IndexBuffer ib = new IndexBuffer(graphics.GraphicsDevice,
                sizeof(short) * indices.Length, BufferUsage.WriteOnly,
                IndexElementSize.SixteenBits);

            ib.SetData(indices);

            graphics.GraphicsDevice.Indices = ib;
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
            InitializeVertices();

            InitializeIndices();

            texture = Content.Load<Texture2D>("texture");

            effect = new BasicEffect(graphics.GraphicsDevice, null);

            vertexDeclaration = new VertexDeclaration(graphics.GraphicsDevice,
                VertexPositionNormalTexture.VertexElements);
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

            // TODO: Add your drawing code here
            graphics.GraphicsDevice.VertexDeclaration = vertexDeclaration;

            effect.Projection = projection;
            effect.View = view;

            effect.EnableDefaultLighting();

            effect.TextureEnabled = true;
            effect.Texture = texture;

            Matrix world = Matrix.Identity;
            effect.World = world;
            effect.Begin();

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                graphics.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, indices.Length / 3);

                pass.End();
            }

            effect.End();

            base.Draw(gameTime);
        }
    }
}
