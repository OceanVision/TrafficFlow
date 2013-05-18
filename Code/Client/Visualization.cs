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
        private GraphicsDeviceManager Graphics;
        private SpriteBatch SpriteBatch;

        public Visualization()
        {
            Graphics = new GraphicsDeviceManager(this);
            Graphics.PreferMultiSampling = true;
            Content.RootDirectory = "Content";
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
            InitControls();

            ZoomState = 3;
            ZoomRange = new Vector2(1, 3);
            FirstAssetName = 1346;

            Timer = 0;

            Camera = new Camera2d(new Viewport(0, 0, 0, 0), ZoomState * 5 * 256 - Window.ClientBounds.Width, ZoomState * 5 * 256 - Window.ClientBounds.Height, 1.0f);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            Graphics.GraphicsDevice.PresentationParameters.MultiSampleCount = 6;
            SpriteBatch = new SpriteBatch(Graphics.GraphicsDevice);
            CurrentTiles = new LinkedList<LinkedList<Tile>>();
            EmptyTexture = Content.Load<Texture2D>(@"Tiles\empty");

            //aktualny zakres kafelek
            CurrentView = new View(0, MapBuffer - 1, MapBuffer - 1, 0);

            //³adujê kafelki
            LoadTiles();

            //mycha
            Mouse.SetPosition(0, 0);
            originalMouseState = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);

            //samochód - test
            route = new Queue<Tuple<Vector2, Vector2>>();
            route.Enqueue(new Tuple<Vector2, Vector2>(new Vector2(2, 310), new Vector2(309, 441)));
            //route.Enqueue(new Tuple<Vector2, Vector2>(new Vector2(ZoomState * 152, ZoomState * 234), new Vector2(ZoomState * 164, ZoomState * 243)));
            //route.Enqueue(new Tuple<Vector2, Vector2>(new Vector2(ZoomState * 164, ZoomState * 243), new Vector2(ZoomState * 173, ZoomState * 253)));
            //route.Enqueue(new Tuple<Vector2, Vector2>(new Vector2(ZoomState * 173, ZoomState * 253), new Vector2(ZoomState * 210, ZoomState * 317)));
            
            Car1 = new Car(route, 20);
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
            Timer += gameTime.ElapsedGameTime.TotalSeconds;

            CameraUpdate();
            MouseUpdate();
            TilesUpdate(); //TODO: Poprawiæ wczytywanie kafelek

            if (Car1.Driving())
                Car1.Update((float)Timer);
            else
                Car1.AddRoute(route);
            
            //Debug.Text = Timer + " " + Car1.Time + " " + Car1.Finished;
            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Graphics.GraphicsDevice.Clear(new Color(242, 239, 233));
            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Camera.GetTransformation());
            /// -------------------------------------

            //prze³adowanie kafelek
            foreach (LinkedList<Tile> tmp in CurrentTiles)
                foreach (Tile tile in tmp)
                {
                    SpriteBatch.Draw(tile.texture, new Vector2(tile.X, tile.Y), Color.White);
                }

            //rysowanie linii - test
            /*foreach (Line line in Lines)
            {
                SpriteBatch.DrawLine(new Vector2(ZoomState * line.Start.X, ZoomState * line.Start.Y), new Vector2(ZoomState * line.End.Y, ZoomState * line.End.Y), line.Color, 2);
            }*/
            SpriteBatch.DrawLine(new Vector2(ZoomState * 3, ZoomState * 155), new Vector2(ZoomState * 152, ZoomState * 234), new Color(100, 100, 100, 255), 2);
            SpriteBatch.DrawLine(new Vector2(ZoomState * 152, ZoomState * 234), new Vector2(ZoomState * 164, ZoomState * 243), new Color(100, 100, 100, 255), 2);
            SpriteBatch.DrawLine(new Vector2(ZoomState * 164, ZoomState * 243), new Vector2(ZoomState * 173, ZoomState * 253), new Color(100, 100, 100, 255), 2);
            SpriteBatch.DrawLine(new Vector2(ZoomState * 173, ZoomState * 253), new Vector2(ZoomState * 210, ZoomState * 317), new Color(178, 34, 34, 255), 2);

            //samochód - test
            SpriteBatch.DrawCircle(Car1.Pos, 4, 10, Color.DimGray, 4);

            /// -------------------------------------
            SpriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
