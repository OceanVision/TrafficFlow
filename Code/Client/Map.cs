using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjNet.Converters;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace TrafficFlow
{
    #region class View
    public class View
    {
        public int Top, Right, Bottom, Left;

        public View(int top, int right, int bottom, int left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }
    }
    #endregion

    #region class Tile
    public class Tile
    {
        public int X;
        public int Y;
        public Texture2D texture;

        public Tile(int x, int y, Texture2D tex)
        {
            X = x;
            Y = y;
            texture = tex;
        }
    }
    #endregion

    #region class Line
    public class Line
    {
        public Vector2 Start;
        public Vector2 End;
        public Color Color;

        public Line(Vector2 x, Vector2 y, Color color)
        {
            Start = x;
            End = y;
            Color = color;
        }

        public Line(Line line)
        {
            Start = new Vector2(line.Start.X, line.Start.Y);
            End = new Vector2(line.End.X, line.End.Y);
            Color = new Color(line.Color.R, line.Color.G, line.Color.B, line.Color.A);
        }
    }
    #endregion

    #region class Car
    public class Car
    {
        public Vector2 Pos;
        public float Velocity;
        public float Time;
        public int ZoomCoef;

        private Queue<Line> Route;
        private Vector2 Versor;
        private float CurrentDistance;
        private float DesiredDistance;

        public Car(float v, int zoomCoef)
        {
            Pos = new Vector2();
            Velocity = v;
            Time = 0;
            ZoomCoef = zoomCoef;
            Route = new Queue<Line>();
            CurrentDistance = 0;
            DesiredDistance = -1;
        }

        public Car(Queue<Line> route, float v, int zoomCoef)
        {
            Pos = new Vector2();
            Velocity = v;
            Time = 0;
            ZoomCoef = zoomCoef;
            Route = new Queue<Line>(route);
            CurrentDistance = 0;
            DesiredDistance = -1;
        }

        //dodaje całą trasę
        public void AddRoute(Queue<Line> route)
        {
            Route = new Queue<Line>(route);
        }

        //dodaje linię do trasy
        public void AddLine(Line line)
        {
            Route.Enqueue(line);
        }

        public void InitLine(Vector2 start, Vector2 end)
        {
            Pos.X = start.X; Pos.Y = start.Y;
            CurrentDistance = 0;
            DesiredDistance = (float)Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));

            Versor = (end - start) / DesiredDistance;
        }
        
        public void Update(float t)
        {
            if (CurrentDistance < DesiredDistance)
            {
                Pos += Velocity * Versor * (t - Time) * ZoomCoef;
                CurrentDistance += Velocity * (t - Time) * ZoomCoef;
            }
            else if (Route.Count > 0)
            {
                Line tmp = Route.Dequeue();
                InitLine(tmp.Start, tmp.End);
            }
            
            Time = t;
        }

        public bool Driving()
        {
            if (Route.Count > 0)
                return true;
            return false;
        }
    }
    #endregion

    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        // MAPA W KAFELKACH
        Texture2D[][][] Tiles;  //wszystkie kafelki
        View CurrentView;   //obszar odświeżany
        int MapBuffer;  //wielkość bufora obszaru
        Texture2D EmptyTexture; //pusta tekstura (czasami w mapie są puste kafelki)
        Camera2d Camera;
        int CameraPrecision;    //szybkość przesuwania kamery klawiaturą

        // ZOOM
        int ZoomState;  //stan
        Vector2 ZoomRange;  //zakres stanu (1-3)
        int FirstAssetName;


        // GRAF ULIC
        CityGraph CityGraph;    //graf wejściowy
        Vector2 SphericalStart; //punkt startowy dla rysowania w układzie sferycznym
        Vector2 CartesianCoef;  //współczynnik do konwersji z układu kartezjańskiego na piksele
        LinkedList<Line> Lines; //zbiór linii ulic

        

        //klawiatura
        KeyboardState KeyboardState;
        Vector2 KeyboardMovement;

        //myszka
        Vector2 originalMouseState;

        //inne
        Car Car1;
        double Timer;

        
        
        //przesuwa kamerę
        private void CameraUpdate()
        {
            KeyboardState = Keyboard.GetState();
            KeyboardMovement = Vector2.Zero;

            if (KeyboardState.IsKeyDown(Keys.Left)) KeyboardMovement.X--;
            if (KeyboardState.IsKeyDown(Keys.Right)) KeyboardMovement.X++;
            if (KeyboardState.IsKeyDown(Keys.Up)) KeyboardMovement.Y--;
            if (KeyboardState.IsKeyDown(Keys.Down)) KeyboardMovement.Y++;

            Camera.Pos += KeyboardMovement * CameraPrecision;
            Debug.Text = CurrentView.Top + " " + CurrentView.Bottom;
        }

        //wiadomo
        private void MouseUpdate()
        {
            Vector2 currentMouseState = new Vector2(Mouse.GetState().X + Camera.Pos.X, Mouse.GetState().Y + Camera.Pos.Y);
            //if (currentMouseState != originalMouseState)
            //    Debug.Text = (currentMouseState.X - originalMouseState.X) + " " + (currentMouseState.Y - originalMouseState.Y);
        }

        //aktualizuje granice obszaru dla widocznych kafelek
        private void ViewUpdate()
        {
            const int precision = 100;  //255 max
            if (CurrentView.Right + 1 < 5 * (int)Math.Pow(2, ZoomState - 1) && Camera.Pos.X + Window.ClientBounds.Width > (CurrentView.Right + 1) * 256 - precision)   //prawo
            {
                ++CurrentView.Left;
                ++CurrentView.Right;
            }
            if (CurrentView.Left > 0 && Camera.Pos.X < CurrentView.Left * 256 + precision)     //lewo
            {
                --CurrentView.Left;
                --CurrentView.Right;
            }
            if (CurrentView.Bottom + 1 < 3 * (int)Math.Pow(2, ZoomState - 1) && Camera.Pos.Y + Window.ClientBounds.Height > (CurrentView.Bottom + 1) * 256 - precision)   //dół
            {
                ++CurrentView.Top;
                ++CurrentView.Bottom;
            }
            if (CurrentView.Top > 0 && Camera.Pos.Y < CurrentView.Top * 256 + precision)   //góra
            {
                --CurrentView.Top;
                --CurrentView.Bottom;
            }
        }

        //ogarnia stan zoomu
        private void ZoomUpdate(bool change)
        {
            if (change && ZoomState < ZoomRange.Y)
                ++ZoomState;
            else if (!change && ZoomState > ZoomRange.X)
                --ZoomState;
            else
                return;

            MapBuffer = 5;
            int worldWidth = 5 * (int)Math.Pow(2, ZoomState - 1) * 256 - Window.ClientBounds.Width;
            int worldHeight = 3 * (int)Math.Pow(2, ZoomState - 1) * 256 - Window.ClientBounds.Height;
            Camera.Reinitialize(worldWidth, worldHeight);
        }

        //dodaje linię do listy
        private void AddLine(Line l)
        {
            Line line = new Line(l);
            Lines.AddLast(line);
        }

        //przyjmuje współrzędne geograficzne i zwraca kartezjańskie
        private Vector2 CoordinateTransformation(double[] spherical)
        {
            CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
            ICoordinateTransformation trans = ctfac.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WGS84_UTM(34, true));

            double[] point = new double[] { spherical[0], spherical[1] };
            double[] cartesian = trans.MathTransform.Transform(point);
            double[] result = new double[2] { cartesian[0] - SphericalStart.X, SphericalStart.Y - cartesian[1] };
            return new Vector2((float)result[0] / CartesianCoef.X, (float)result[1] / CartesianCoef.Y);
        }

        //tworzy sieć ulic
        private void BuildNetwork()
        {
            CityGraph = new CityGraph("./warsaw_graph.xml");
            Vector2 start, end;
            int count = 0;
            foreach (MyEdge e in CityGraph.graph.Edges)
            {
                start = CoordinateTransformation(new double[2]{e.startNode.longitude, e.startNode.latitude});
                end = CoordinateTransformation(new double[2]{e.endNode.longitude, e.endNode.latitude});
                if(start.X >= 0 && start.Y >= 0 && end.X <= 1024 && end.Y <= 1024)
                    AddLine(new Line(new Vector2(start.X, start.Y), new Vector2(end.X, end.Y), new Color(100, 100, 100, 255)));
            }
        }
    }
}
