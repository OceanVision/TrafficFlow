using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ProjNet.Converters;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using Graph;
using PredictionServer;

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
        public float Velocity;

        public Line(Vector2 x, Vector2 y, float velocity)
        {
            Start = x;
            End = y;
            Velocity = velocity;
        }

        public Line(Line line)
        {
            Start = new Vector2(line.Start.X, line.Start.Y);
            End = new Vector2(line.End.X, line.End.Y);
            Velocity = line.Velocity;
        }

        public Color Color
        {
            get
            {
                float v = Velocity;
                if (v < 5)
                    return new Color(Color.Firebrick.R, Color.Firebrick.G, Color.Firebrick.B, 140);   //red
                else if (v < 10)
                    return new Color(Color.OrangeRed.R, Color.OrangeRed.G, Color.OrangeRed.B, 140);   //orangered
                else if (v < 15)
                    return new Color(Color.Orange.R, Color.Orange.G, Color.Orange.B, 140);   //orange
                else if (v < 20)
                    return new Color(Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, 140);   //yellow
                else if (v < 30)
                    return new Color(Color.YellowGreen.R, Color.YellowGreen.G, Color.YellowGreen.B, 140);  //yellowgreen
                return new Color(Color.DarkGreen.R, Color.DarkGreen.G, Color.DarkGreen.B, 140);   //green
            }
        }
    }
    #endregion

    #region class Car
    public class Car
    {
        public Vector2 Pos;
        public float Time;

        private int State;
        private float Velocity;
        private Queue<Line> Route;
        private Vector2 Versor;
        private float CurrentDistance;
        private float DesiredDistance;
        private int Passed;

        public Car()
        {
            State = 0;
            Pos = new Vector2(-10.0f, -10.0f);
            Time = 0;
            Route = new Queue<Line>();
            CurrentDistance = 0;
            DesiredDistance = 0;
            Passed = -1;
        }

        public Car(Queue<Line> route, float v)
        {
            State = 0;
            Pos = new Vector2(-10.0f, -10.0f);
            Time = 0;
            Route = new Queue<Line>(route);
            CurrentDistance = 0;
            DesiredDistance = 0;
            Passed = -1;
        }

        //adds line to the route
        public void AddLine(Line line)
        {
            Route.Enqueue(line);
        }

        //cleans route with no impact on current state 
        public void DropRoute()
        {
            Route.Clear();
        }

        public void InitLine(Line line)
        {
            ++Passed;
            Pos.X = line.Start.X;
            Pos.Y = line.Start.Y;
            CurrentDistance = 0;
            DesiredDistance = (float)Math.Sqrt(Math.Pow(line.End.X - line.Start.X, 2) + Math.Pow(line.End.Y - line.Start.Y, 2));

            Versor = (line.End - line.Start) / DesiredDistance;
            Velocity = line.Velocity;
        }

        public void Start()
        {
            State = 2;
        }

        public void Pause()
        {
            State = 1;
        }

        public void Stop()
        {
            State = 0;
            Pos = new Vector2(-10.0f, -10.0f);
            Time = 0;
            Route.Clear();
            CurrentDistance = 0;
            DesiredDistance = 0;
            Passed = -1;
        }
        
        public void Update(float t)
        {
            if (State != 2)
            {
                Time = t;
                return;
            }

            if (CurrentDistance < DesiredDistance)
            {
                Pos += Velocity * Versor * (t - Time) * Visualization.ZoomState;
                CurrentDistance += Velocity * (t - Time) * Visualization.ZoomState;
            }
            else if (Route.Count > 0)
            {
                Line tmp = Route.Dequeue();
                InitLine(tmp);
            }
            else
            {
                State = 0;
                Passed = -1;
            }
            
            Time = t;
        }

        public int GetState
        {
            get
            {
                return State;
            }
        }

        public int Count
        {
            get
            {
                return Route.Count;
            }
        }

        public int StreetsPassed
        {
            get
            {
                return Passed;
            }
        }
    }
    #endregion

    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        // TILES MAP
        Texture2D[][][] Tiles;  //all tiles
        View CurrentView;   //current view in window
        int MapBuffer;  //size of refreshed area in a one moment
        Texture2D EmptyTexture; //plain texture (sometimes there are empty tiles in the map)
        Dictionary<Vector2, int> SelectedPoints;    //points selected to the route of car
        Camera2d Camera;    //instance of camera
        int CameraPrecision;    //speed of navigating camera using keyboard
        SpriteFont Font;
        LinkedList<Rectangle> NotMapAreas;

        
        // ZOOM
        public static int ZoomState;  //state of zoom
        Vector2 ZoomRange;  //range of ZoomState (1-3)

        
        // TRAFFIC AND STREETS NETWORK
        CityGraph CityGraph;    //input graph
        Vector2 SphericalStart; //starting point in spherical system to be drawn
        Vector2 CartesianCoef;  //coefficient used to converting from cartesian system to interior pixel system
        LinkedList<Line> Lines; //list of street lines
        Dictionary<Vector2, int> Nodes; //list of street nodes
        LinkedList<Line> Route; //list of route lines
        Object LinesLock;    //lock
        Object RouteLock;
        Object DataLock;
        

        // KEYBOARD SUPPORT
        KeyboardState KeyboardState;
        Vector2 KeyboardMovement;

        // MOUSE SUPPORT
        Vector2 InitialMousePos;
        Vector2 CurrentMousePos;
        MouseState InitialMouseState;
        MouseState LastMouseState;

        // CAR SUPPORT
        Car Car;
        double Timer;

        
        
        //moves camera
        private void UpdateCamera()
        {
            KeyboardState = Keyboard.GetState();
            KeyboardMovement = Vector2.Zero;

            if (KeyboardState.IsKeyDown(Keys.Left)) KeyboardMovement.X--;
            if (KeyboardState.IsKeyDown(Keys.Right)) KeyboardMovement.X++;
            if (KeyboardState.IsKeyDown(Keys.Up)) KeyboardMovement.Y--;
            if (KeyboardState.IsKeyDown(Keys.Down)) KeyboardMovement.Y++;

            Camera.Pos += KeyboardMovement * CameraPrecision;
            //Debug.Text = CurrentView.Top + " " + CurrentView.Bottom;
        }

        //considers mouse actions
        private void UpdateMouse()
        {
            MouseState mouseState = Mouse.GetState();
            if (!IsActive || mouseState.X < 0 || mouseState.Y < 0 || mouseState.X > Window.ClientBounds.Width || mouseState.Y > Window.ClientBounds.Height)
                return;

            Vector2 mousePos = new Vector2(mouseState.X + Camera.Pos.X, mouseState.Y + Camera.Pos.Y);
            if (mousePos != InitialMousePos)
            {   
                CurrentMousePos.X = mousePos.X - InitialMousePos.X;
                CurrentMousePos.Y = mousePos.Y - InitialMousePos.Y;
            }

            if (ZoomState < 3 && LastMouseState.ScrollWheelValue < mouseState.ScrollWheelValue)
            {
                Camera.Zoom += 0.05f;
                if (Math.Abs(Camera.Zoom - 2.0f) < float.Epsilon)
                {
                    UpdateZoom(true);
                    Camera.Zoom = 1;
                }
            }
            else if ((ZoomState > 1 || Camera.Zoom > 1) && LastMouseState.ScrollWheelValue > mouseState.ScrollWheelValue)
            {
                Camera.Zoom -= 0.05f;
                if (Math.Abs(Camera.Zoom - 0.5f) < float.Epsilon)
                {
                    UpdateZoom(false);
                    Camera.Zoom = 1;
                }
            }

            //clicking on map
            if (Car.GetState != 2 && Nodes.Count > 0 && mouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released)
            {
                Point pos = new Point(mouseState.X, mouseState.Y);
                bool ok = true;
                foreach (var rect in NotMapAreas)
                    if (rect.Contains(pos))
                    {
                        ok = false;
                        break;
                    }

                if (ok && RouteMode)
                    MatchPoint();
            }
            
            LastMouseState = mouseState;
        }

        //updates limits of refreshed area
        private void UpdateView()
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

        //updates zoom state (change=true -> state++ and vice versa)
        private void UpdateZoom(bool change)
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

        //adds line to streets network lines
        private void AddNetworkLine(Line l)
        {
            Line line = new Line(l);
            Lines.AddLast(line);
        }

        //adds line to route lines
        private void AddRouteLine(Line l)
        {
            Line line = new Line(l);
            Route.AddLast(line);
        }

        //gets geographical coordinates and returns coordinates in cartesian system
        private double[] SphericalToCartesian(double[] spherical)
        {
            CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
            ICoordinateTransformation trans = ctfac.CreateFromCoordinateSystems(GeographicCoordinateSystem.WGS84, ProjectedCoordinateSystem.WGS84_UTM(34, true));

            double[] point = new double[] { spherical[0], spherical[1] };
            double[] cartesian = trans.MathTransform.Transform(point);
            double[] result = new double[2] { cartesian[0] - SphericalStart.X, SphericalStart.Y - cartesian[1] };
            return new double[2] {result[0] / CartesianCoef.X, result[1] / CartesianCoef.Y};
        }

        //adds single point to route list
        private void MatchPoint()
        {
            float coefX = (float)Math.Pow(2, (ZoomState - 1) + (Camera.Zoom - 1.0f) / 1.0f) * 0.9969f;
            float coefY = (float)Math.Pow(2, (ZoomState - 1) + (Camera.Zoom - 1.0f) / 1.0f) * 1.0034f;
            float minDist = float.MaxValue;
            KeyValuePair<Vector2, int> min = new KeyValuePair<Vector2, int>();
            Vector2 tmp;
            foreach (var point in Nodes)
            {
                tmp = new Vector2(coefX * point.Key.X, coefY * point.Key.Y); 
                if (Vector2.Distance(tmp, CurrentMousePos) < minDist)
                {
                    min = point;
                    minDist = Vector2.Distance(tmp, CurrentMousePos);
                }
            }

            int id;
            if (!SelectedPoints.TryGetValue(min.Key, out id))
                SelectedPoints.Add(min.Key, min.Value);

            if (Route.Count > 0) //if route exists
                Route.Clear();
        }

        //updates LoadingText
        private void UpdateProgress()
        {
            if (!IsProgress || ServerCore.Instance.serverConfiguration.max_progress == 0)
                return;

            int percentage = (int)((float)ServerCore.Instance.progress * 100 / (float)ServerCore.Instance.serverConfiguration.max_progress);
            if (percentage < 100)
                LoadingText.Text = "The server is starting... " + percentage + "%";
        }

        //build route
        private void BuildRoute()
        {
            int[] points = new int[SelectedPoints.Count];
            int i = 0;
            foreach (var point in SelectedPoints)
                points[i++] = point.Value;

            Route.Clear();
            if(Car.GetState == 2)
                Car.Pause();
            
            LinkedList<MyEdge> edges = CityGraph.shortestPath(points);
            Dictionary<MyEdge, double> velocities = CityGraph.getVelocities();
            Line tmp;
            double[] start, end;
            List<Line> tmpRoute = new List<Line>();
            foreach (MyEdge e in edges)
            {
                start = SphericalToCartesian(new double[2] { e.startNode.longitude, e.startNode.latitude });
                end = SphericalToCartesian(new double[2] { e.endNode.longitude, e.endNode.latitude });
                tmp = new Line(new Vector2((float)start[0], (float)start[1]), new Vector2((float)end[0], (float)end[1]), (float)velocities[e] / 4);

                AddRouteLine(tmp);
                tmpRoute.Add(tmp);
            }

            for (i = Car.StreetsPassed + 1; i < tmpRoute.Count; ++i)
                Car.AddLine(tmpRoute[i]);

            if (Car.GetState == 1)
                Car.Start();
        }

        //creates traffic network
        private void UpdateNetwork()
        {
            if (Lines.Count == 0)   //if first occurence of UpdateNetwork()
            {
                CityGraph = new CityGraph(@".\warsaw_graph.xml", @".\abstract_links.csv");  //loades network data
                CityGraph.startSimulation(@".\simple_congestion.csv", ServerStartTime);  //starts simulating traffic
            }

            IsProgress = true;

            CityGraph.updateSimulationData();
            Dictionary<MyEdge, double> velocities = CityGraph.getVelocities();

            double[] start, end;
            LinkedListNode<Line> tmp = Lines.First;
            foreach (var e in velocities)
            {
                start = SphericalToCartesian(new double[2] { e.Key.startNode.longitude, e.Key.startNode.latitude });
                end = SphericalToCartesian(new double[2] { e.Key.endNode.longitude, e.Key.endNode.latitude });

                if (start[0] > -16 && start[0] < 1280 && 
                end[0] > -16 && end[0] < 1280 && 
                start[1] > -16 && start[1] < 768 && 
                end[1] > -16 && end[1] < 768)   //considers edges only in the specified range
                {
                    Vector2 point = new Vector2((float)start[0], (float)start[1]);
                    int id;
                    if (!Nodes.TryGetValue(point, out id))
                        Nodes.Add(point, e.Key.startNode.id);

                    if (!Nodes.TryGetValue(point, out id))
                        Nodes.Add(point, e.Key.endNode.id);
                    
                    lock (LinesLock)
                    {
                        if (!NetworkLoaded)    //if no lines are created
                            AddNetworkLine(new Line(new Vector2((float)start[0], (float)start[1]), new Vector2((float)end[0], (float)end[1]), (float)e.Value));
                        else
                        {
                            tmp.Value.Velocity = (float)e.Value;
                            tmp = tmp.Next;
                        }
                    }
                }
            }

            if (!NetworkLoaded)
                LoadingText.Invoke(new Action(delegate
                {
                    LoadingText.Text = "The server started at " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
                }));
            else
                LoadingText.Invoke(new Action(delegate
                {
                    LoadingText.Text = "The server updated prediction at " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second;
                }));

            RouteModeSwitch.Invoke(new Action(delegate { RouteModeSwitch.Enabled = true; }));
            NetworkLoaded = true;

            if (SelectedPoints.Count > 1 && Route.Count == 0)
            {
                lock (RouteLock)
                {
                    BuildRoute();
                }
            }

            Thread.Sleep(60 * 1000);
            UpdateNetwork();
        }
    }
}
