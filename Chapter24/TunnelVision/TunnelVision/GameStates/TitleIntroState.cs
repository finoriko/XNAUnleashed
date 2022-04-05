using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XELibrary;

namespace TunnelVision
{
    public sealed class TitleIntroState : BaseGameState, ITitleIntroState
    {
        private Texture2D texture;

        private Effect fireEffect;
        private Random rand = new Random();
        private Texture2D hotSpotTexture;
        private Texture2D fire; //gets render target’s texture

        private RenderTarget2D renderTarget1;
        private RenderTarget2D renderTarget2;

        private int offset = -128;
        private Color[] colors = {
            Color.Black,
            Color.Yellow,
            Color.White,
            Color.Red,
            Color.Orange,
            new Color(255,255,128) //yellowish white
        };

        public TitleIntroState(Game game)
            : base(game)
        {
            game.Services.AddService(typeof(ITitleIntroState), this);
        }

        public override void Update(GameTime gameTime)
        {
            if (Input.WasPressed(0, Buttons.Back, Keys.Escape))
                OurGame.Exit();

            if (Input.WasPressed(0, Buttons.Start, Keys.Enter))
            {
                // push our start menu onto the stack
                GameManager.PushState(OurGame.StartMenuState.Value);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            Vector2 pos = new Vector2((GraphicsDevice.Viewport.Width - texture.Width) / 2,
                (GraphicsDevice.Viewport.Height - texture.Height) / 2);

            GraphicsDevice device = GraphicsDevice;

            //Draw hotspots on the first Render Target
            device.SetRenderTarget(0, renderTarget1);
            device.Clear(Color.Black);

            OurGame.SpriteBatch.Begin();

            //get last drawn screen — if not first time in
            //fire is null first time in, and when device is lost (LoadGraphicsContent)
            if (fire != null) //render target has valid texture
                OurGame.SpriteBatch.Draw(fire, Vector2.Zero, Color.White);

            //draw hotspots
            for (int i = 0; i < device.Viewport.Width / hotSpotTexture.Width; i++)
                OurGame.SpriteBatch.Draw(hotSpotTexture,
                    new Vector2(i * hotSpotTexture.Width,
                    device.Viewport.Height - hotSpotTexture.Height),
                    colors[rand.Next(colors.Length)]);

            OurGame.SpriteBatch.End();


            //resolve what we just drew to our render target
            //and clear it out
            device.SetRenderTarget(0, null);

            // Transfer from first to second render target
            device.SetRenderTarget(0, renderTarget2);

            fireEffect.Begin();
            OurGame.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate,
                SaveStateMode.None);

            EffectPass pass = fireEffect.CurrentTechnique.Passes[0];
            pass.Begin();
            OurGame.SpriteBatch.Draw(renderTarget1.GetTexture(), new Rectangle(0, offset,
                device.Viewport.Width, device.Viewport.Height - offset), Color.White);
            OurGame.SpriteBatch.End();
            pass.End();
            fireEffect.End();

            //resolve what we just drew to our render target
            //and clear it out
            device.SetRenderTarget(0, null);

            device.Clear(Color.Black);

            //set texture to render
            fire = renderTarget2.GetTexture();

            // Draw second render target onto the screen (back buffer)
            OurGame.SpriteBatch.Begin(SpriteBlendMode.Additive);

            OurGame.SpriteBatch.Draw(texture, pos, Color.White);

            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, Color.White);
            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, Color.White);
            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, Color.White);

            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, null, Color.White, 0,
                Vector2.Zero, 1.0f, SpriteEffects.FlipVertically, 0);
            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, null, Color.White, 0,
                Vector2.Zero, 1.0f, SpriteEffects.FlipVertically, 0);
            OurGame.SpriteBatch.Draw(fire, Vector2.Zero, null, Color.White, 0,
                Vector2.Zero, 1.0f, SpriteEffects.FlipVertically, 0);

            OurGame.SpriteBatch.End();

            base.Draw(gameTime);
        }

        private Texture2D CreateTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height, 1,
                TextureUsage.None, SurfaceFormat.Color);

            int pixelCount = width * height;
            Color[] pixelData = new Color[pixelCount];

            for (int i = 0; i < pixelCount; i++)
                pixelData[i] = Color.White;

            texture.SetData(pixelData);

            return (texture);
        }


        protected override void LoadContent()
        {
            GraphicsDevice device = GraphicsDevice;

            hotSpotTexture = CreateTexture(4, 1);
            OurGame.SpriteBatch = new SpriteBatch(device);
            fireEffect = Content.Load<Effect>(@"Effects\Fire");
            texture = Content.Load<Texture2D>(@"Textures\titleIntro");

            renderTarget1 = new RenderTarget2D(device, device.Viewport.Width,
                device.Viewport.Height, 1, device.DisplayMode.Format,
                device.PresentationParameters.MultiSampleType,
                device.PresentationParameters.MultiSampleQuality);

            renderTarget2 = new RenderTarget2D(device, device.Viewport.Width,
                device.Viewport.Height, 1, device.DisplayMode.Format,
                device.PresentationParameters.MultiSampleType,
                device.PresentationParameters.MultiSampleQuality);

            fire = null;

            base.LoadContent();

        }

    }
}
