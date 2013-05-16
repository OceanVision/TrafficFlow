using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TrafficFlow
{
    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        //grafika
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        //mapa
        private View currentView;
        private const int mapBuffer = 6;
        private LinkedList<LinkedList<Tile>> currentTiles;
        private Texture2D emptyTexture;
        private Camera2d camera;
        private const float zoomInc = 0.2f;

        public Visualization()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferMultiSampling = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;
            //TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 40.0f); //ustawia 40 fps
            IsFixedTimeStep = false;    //ustawia wywo³anie Update() bez interwa³u (chyba tak?)
            camera = new Camera2d(new Viewport(0, 0, 0, 0), 10 * 256 - Window.ClientBounds.Width, 10 * 256 - Window.ClientBounds.Height, 1.0f);
            initControls();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            graphics.GraphicsDevice.PresentationParameters.MultiSampleCount = 6;
            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            currentTiles = new LinkedList<LinkedList<Tile>>();
            emptyTexture = Content.Load<Texture2D>(@"Tiles\empty");

            for (int i = 0; i < mapBuffer; ++i)
            {
                currentTiles.AddLast(new LinkedList<Tile>());
                for (int j = 0; j < mapBuffer; ++j)
                    currentTiles.Last.Value.AddLast(new Tile(i * 256, j * 256, Content.Load<Texture2D>(@"Tiles\" + i.ToString() + @"\" + (j + 2692).ToString())));
            }

            currentView = new View(0, mapBuffer - 1, mapBuffer - 1, 0);
            Mouse.SetPosition(0, 0);
            originalMouseState = Mouse.GetState();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            Content.Unload();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            cameraUpdate();
            mouseUpdate();

            //TODO: Poprawiæ wczytywanie kafelek
            tilesUpdate();
            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.LightGray);
            spriteBatch.Begin(SpriteSortMode.Deferred,
                    null, null, null, null, null,
                    camera.GetTransformation());
            /// -------------------------------------



            foreach (LinkedList<Tile> tmp in currentTiles)
                foreach (Tile tile in tmp)
                {
                    spriteBatch.Draw(tile.texture, new Vector2(tile.X, tile.Y), Color.White);
                }

            //spriteBatch.DrawCircle(new Vector2(152.0f - 1.3f, 234.0f + 4.0f), 4.0f, 100, new Color(100, 100, 100, 255), 4);
            spriteBatch.DrawLine(new Vector2(3, 155), new Vector2(152, 234), new Color(100, 100, 100, 255), 2);
            spriteBatch.DrawLine(new Vector2(152, 234), new Vector2(164, 243), new Color(100, 100, 100, 255), 2);
            spriteBatch.DrawLine(new Vector2(164, 243), new Vector2(173, 253), new Color(100, 100, 100, 255), 2);
            spriteBatch.DrawLine(new Vector2(173, 253), new Vector2(210, 317), new Color(178, 34, 34, 255), 2);
            /// -------------------------------------
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
