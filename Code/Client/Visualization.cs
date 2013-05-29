using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Graph;

namespace TrafficFlow
{
    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        //graphics
        GraphicsDeviceManager Graphics;
        SpriteBatch SpriteBatch;
        bool IsProgress;
        bool RouteMode;
        bool NetworkLoaded;
        int ServerStartTime;

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
            //TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 60.0f); //set up 60 fps
            IsFixedTimeStep = false;    //set up executing Update() without interval (right?)

            SelectedPoints = new Dictionary<Vector2, int>();
            Lines = new LinkedList<Line>();
            Nodes = new Dictionary<Vector2, int>();
            Route = new LinkedList<Line>();
            NotMapAreas = new LinkedList<Rectangle>();
            LinesLock = new Object();
            RouteLock = new Object();
            DataLock = new Object();
            NetworkLoaded = false;

            IsProgress = false;
            RouteMode = false;
            
            InitControls();

            SphericalStart = new Vector2(495900.31940731f, 5787874.09092728f);
            CartesianCoef = new Vector2(5.83817174232375f, 5.869556540361367f);

            ZoomState = 1;
            ZoomRange = new Vector2(1, 3);

            MapBuffer = 5;

            Timer = 0;

            int worldWidth = 5 * (int)Math.Pow(2, ZoomState - 1) * 256 - Window.ClientBounds.Width;
            int worldHeight = 3 * (int)Math.Pow(2, ZoomState - 1) * 256 - Window.ClientBounds.Height;
            Camera = new Camera2d(new Viewport(0, 0, 0, 0), worldWidth, worldHeight, 1.0f);
            CameraPrecision = 20;

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
            EmptyTexture = Content.Load<Texture2D>(@"Tiles\empty");
            Font = Content.Load<SpriteFont>("PointsNumbers");
            
            //loads graphics - about 8MB
            Tiles = new Texture2D[4][][];
            for(int i = 1; i <= 3; ++i)
            {
                Tiles[i] = new Texture2D[5 * (int)Math.Pow(2, i - 1)][];
                for (int j = 0; j < 5 * (int)Math.Pow(2, i - 1); ++j)
                {
                    Tiles[i][j] = new Texture2D[3 * (int)Math.Pow(2, i - 1)];
                    for (int k = 0; k < 3 * (int)Math.Pow(2, i - 1); ++k)
                        try
                        {
                            Tiles[i][j][k] = Content.Load<Texture2D>(@"Tiles\" + (i + 13) + @"\" + (j + (int)Math.Pow(2, i - 1) * 9145) + @"\" + (k + Math.Pow(2, i - 1) * 5394));
                        }
                        catch (Exception e)
                        {
                            Tiles[i][j][k] = EmptyTexture;
                        }
                }
            }

            //initializes current view
            CurrentView = new View(0, MapBuffer - 1, MapBuffer - 1, 0);

            //initializes mouse
            Mouse.SetPosition(0, 0);
            InitialMousePos = new Vector2(Mouse.GetState().X + Camera.Pos.X, Mouse.GetState().Y + Camera.Pos.Y);
            CurrentMousePos = new Vector2(InitialMousePos.X, InitialMousePos.Y);
            InitialMouseState = Mouse.GetState();

            //initializes car
            Car = new Car();
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

            UpdateControls();
            UpdateProgress();

            if (!NetworkLoaded)
            {
                base.Update(gameTime);
                return;
            }

            UpdateCamera();
            UpdateMouse();
            UpdateView();
            
            Car.Update((float)Timer);
            base.Update(gameTime);
        }



        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Graphics.GraphicsDevice.Clear(Color.White);
            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Camera.GetTransformation());
            /// -------------------------------------

            //intro screen
            if (!NetworkLoaded)
            {
                SpriteBatch.Draw(Content.Load<Texture2D>("logo"), new Vector2(Window.ClientBounds.Width / 2 - 206, Window.ClientBounds.Height / 2 - 50), Color.White);
                SpriteBatch.End();
                base.Draw(gameTime);
                return;
            }
            
            //refreshing tiles in current range (MapBuffer x MapBuffer)
            for (int i = CurrentView.Left; i < 5 * (int)Math.Pow(2, ZoomState - 1) && i < MapBuffer + CurrentView.Left; ++i)
                for (int j = CurrentView.Top; j < 3 * (int)Math.Pow(2, ZoomState - 1) && j < MapBuffer + CurrentView.Top; ++j)
                    SpriteBatch.Draw(Tiles[ZoomState][i][j], new Vector2(i * 256, j * 256), Color.White);

            //coefficients
            float coefX = 0.9969f * (float)Math.Pow(2, ZoomState - 1);
            float coefY = 1.0034f * (float)Math.Pow(2, ZoomState - 1);

            //drawing lines
            lock (LinesLock)
            {
                foreach (Line line in Lines)
                    SpriteBatch.DrawLine(new Vector2(coefX * line.Start.X, coefY * line.Start.Y), new Vector2(coefX * line.End.X, coefY * line.End.Y), line.Color, ZoomState + 1);
            }

            //drawing route
            lock (RouteLock)
            {
                foreach (Line line in Route)
                    SpriteBatch.DrawLine(new Vector2(coefX * line.Start.X, coefY * line.Start.Y), new Vector2(coefX * line.End.X, coefY * line.End.Y), new Color(80, 80, 80, 190), ZoomState + 3);
            }

            //drawing points
            if (SelectedPoints.Count >= 1)
            {
                Vector2 pointPos = new Vector2(coefX * SelectedPoints.ElementAt(0).Key.X, coefY * SelectedPoints.ElementAt(0).Key.Y);
                SpriteBatch.DrawString(Font, "Start", new Vector2(pointPos.X - 18, pointPos.Y - 30), new Color(0, 102, 204, 255));
                SpriteBatch.DrawCircle(pointPos, 8, 10, new Color(0, 102, 204, 255), 8);
            }
            
            if (SelectedPoints.Count > 1)
            {
                Vector2 pointPos;
                for (int i = 1; i < SelectedPoints.Count - 1; ++i)
                {
                    pointPos = new Vector2(coefX * SelectedPoints.ElementAt(i).Key.X, coefY * SelectedPoints.ElementAt(i).Key.Y);
                    SpriteBatch.DrawCircle(pointPos, 5, 10, new Color(100, 100, 100, 255), 5);
                }

                pointPos = new Vector2(coefX * SelectedPoints.ElementAt(SelectedPoints.Count - 1).Key.X, coefY * SelectedPoints.ElementAt(SelectedPoints.Count - 1).Key.Y);
                SpriteBatch.DrawString(Font, "Koniec", new Vector2(pointPos.X - 26, pointPos.Y - 30), new Color(51, 153, 0, 255));
                SpriteBatch.DrawCircle(pointPos, 8, 10, new Color(51, 153, 0, 255), 8);
            }
            
            //drawing car
            SpriteBatch.DrawCircle(new Vector2(coefX * Car.Pos.X, coefY * Car.Pos.Y), 4, 10, Color.Red, 4);
            
            
            
            
            /// -------------------------------------
            SpriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
