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
            //TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 40.0f); //ustawia 60 fps
            IsFixedTimeStep = false;    //ustawia wywo³anie Update() bez interwa³u (chyba tak?)
            
            InitControls();

            //20.9399, 52.24126
            SphericalStart = new Vector2(495900.31940731f, 5787874.09092728f);
            CartesianCoef = new Vector2(5.83817174232375f, 5.869556540361367f);

            ZoomState = 1;
            ZoomRange = new Vector2(1, 3);
            FirstAssetName = 1346;

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
            //CurrentTiles = new LinkedList<LinkedList<Tile>>();
            EmptyTexture = Content.Load<Texture2D>(@"Tiles\empty");
            Lines = new LinkedList<Line>();

            //³adujê grafiki - oko³o 8MB
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
                        catch (ContentLoadException e)
                        {
                            Tiles[i][j][k] = EmptyTexture;
                        }
                }
            }

            //aktualny zakres kafelek
            CurrentView = new View(0, MapBuffer - 1, MapBuffer - 1, 0);

            //³adujê kafelki
            //LoadTiles();

            //mycha
            Mouse.SetPosition(0, 0);
            originalMouseState = new Vector2(Mouse.GetState().X + Camera.Pos.X, Mouse.GetState().Y + Camera.Pos.Y);

            //linie - test
            //AddLine(new Line(new Vector2(1, 35), new Vector2(177, 64), new Color(100, 100, 100, 255)));
            //AddLine(new Line(new Vector2(0, 0), new Vector2(XCoef * (20.9618f - 20.9399f), YCoef * (52.2412f - 52.2277f)), new Color(100, 100, 100, 255)));
            
            //Message(Lines.Last.Value.End.X + " " + Lines.Last.Value.End.Y);
            //samochód - test
            Car1 = new Car(20, ZoomState);
            //Car1.AddLine(Lines.First.Value);
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
            ViewUpdate(); //TODO: Poprawiæ wczytywanie kafelek

            if (Car1.Driving())
                Car1.Update((float)Timer);
            //else
                //Car1.AddLine(Lines.Last.Value);
            
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

            //odœwie¿enie obszaru kafelek (MapBuffer x MapBuffer)
            for (int i = CurrentView.Left; i < MapBuffer + CurrentView.Left; ++i)
                for (int j = CurrentView.Top; j < MapBuffer + CurrentView.Top; ++j)
                    try
                    {
                        SpriteBatch.Draw(Tiles[ZoomState][i][j], new Vector2(i * 256, j * 256), Color.White);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        break;
                    }

            //rysowanie linii
            foreach (Line line in Lines)
            {
                float coefX = (float)Math.Pow(2, ZoomState - 1) * 0.9969f;
                float coefY = (float)Math.Pow(2, ZoomState - 1) * 1.0034f;
                SpriteBatch.DrawLine(new Vector2(coefX * line.Start.X, coefY * line.Start.Y), new Vector2(coefX * line.End.X, coefY * line.End.Y), line.Color, ZoomState);
            }
            
            //samochód - test
            SpriteBatch.DrawCircle(Car1.Pos * ZoomState, 4, 10, new Color(51, 153, 0, 255), 4);

            /// -------------------------------------
            SpriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
