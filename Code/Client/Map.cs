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
    }
    #endregion

    #region class Car
    public class Car
    {
        public Vector2 Pos;
        public float Velocity;
        public float Time;
        public bool Finished;

        private Queue<Tuple<Vector2, Vector2>> Route;
        private Vector2 Versor;
        private float CurrentDistance;
        private float DesiredDistance;

        public Car(Queue<Tuple<Vector2, Vector2>> route, float v)
        {
            Pos = new Vector2();
            Velocity = v;
            Time = 0;
            Finished = false;
            Route = new Queue<Tuple<Vector2, Vector2>>(route);
            CurrentDistance = 0;
            DesiredDistance = -1;
        }

        public void AddRoute(Queue<Tuple<Vector2, Vector2>> route)
        {
            Route = new Queue<Tuple<Vector2, Vector2>>(route);
        }

        public void AddLine(Tuple<Vector2, Vector2> line)
        {
            Route.Enqueue(new Tuple<Vector2, Vector2>(line.Item1, line.Item2));
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
                Pos += Velocity * Versor * (t - Time);
                CurrentDistance += Velocity * (t - Time);
            }
            else if (Route.Count > 0)
            {
                Tuple<Vector2, Vector2> tmp = Route.Dequeue();
                InitLine(tmp.Item1, tmp.Item2);
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
        //mapa
        View CurrentView;
        const int MapBuffer = 6;
        LinkedList<LinkedList<Tile>> CurrentTiles;
        Texture2D EmptyTexture;
        Camera2d Camera;

        //linie ulic
        private LinkedList<Line> Lines;

        ///zoom
        private int ZoomState;
        private Vector2 ZoomRange;
        private int FirstAssetName;

        //klawiatura
        KeyboardState KeyboardState;
        Vector2 KeyboardMovement;

        //myszka
        Vector2 originalMouseState;

        //inne
        Queue<Tuple<Vector2, Vector2>> route;
        Car Car1;
        double Timer;

        
        
        //przesuwa kamerę
        private void CameraUpdate()
        {
            KeyboardState = Keyboard.GetState();
            KeyboardMovement = Vector2.Zero;
            const int precision = 10;

            if (KeyboardState.IsKeyDown(Keys.Left)) KeyboardMovement.X--;
            if (KeyboardState.IsKeyDown(Keys.Right)) KeyboardMovement.X++;
            if (KeyboardState.IsKeyDown(Keys.Up)) KeyboardMovement.Y--;
            if (KeyboardState.IsKeyDown(Keys.Down)) KeyboardMovement.Y++;

            Camera.Pos += KeyboardMovement * precision;
        }

        //wiadomo
        private void MouseUpdate()
        {
            Vector2 currentMouseState = new Vector2(Mouse.GetState().X + Window.ClientBounds.Left, Mouse.GetState().Y + Window.ClientBounds.Top);
            if (currentMouseState != originalMouseState)
                Debug.Text = (currentMouseState.X - originalMouseState.X) + " " + (currentMouseState.Y - originalMouseState.Y);
        }

        //ładuje kafelki mapy
        private void LoadTiles()
        {
            CurrentTiles.Clear();
            for (int i = CurrentView.Top; i < MapBuffer + CurrentView.Top; ++i)
            {
                CurrentTiles.AddLast(new LinkedList<Tile>());
                for (int j = CurrentView.Left; j < MapBuffer + CurrentView.Left; ++j)
                {
                    try
                    {
                        CurrentTiles.Last.Value.AddLast(new Tile(i * 256, j * 256, Content.Load<Texture2D>(@"Tiles\L" + ZoomState + @"\" + i + @"\" + (j + Math.Pow(2, ZoomState - 1) * 1346))));
                    }
                    catch (ContentLoadException e)
                    {
                        CurrentTiles.Last.Value.AddLast(new Tile(i * 256, j * 256, EmptyTexture));
                    }
                }
            }
        }

        //aktualizuje kafelki mapy
        private void TilesUpdate()
        {
            const int precision = 200;  //255 max
            Texture2D texture;
            if (CurrentView.Right + 1 < 10 && Camera.Pos.X + Window.ClientBounds.Width > (CurrentView.Right + 1) * 256 - precision)   //prawo
            {
                CurrentTiles.AddLast(new LinkedList<Tile>());
                for (int i = CurrentView.Top; i < MapBuffer + CurrentView.Top; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\L" + ZoomState + @"\" + (CurrentView.Right + 1).ToString() + @"\" + (i + Math.Pow(2, ZoomState - 1) * 1346).ToString());
                        CurrentTiles.Last.Value.AddLast(new Tile(((int)CurrentView.Right + 1) * 256, i * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        CurrentTiles.Last.Value.AddLast(new Tile(((int)CurrentView.Right + 1) * 256, i * 256, EmptyTexture));
                    }
                }

                CurrentTiles.RemoveFirst();

                ++CurrentView.Left;
                ++CurrentView.Right;
            }
            if (CurrentView.Left * 256 > 0 && Camera.Pos.X < CurrentView.Left * 256 + precision)     //lewo
            {
                CurrentTiles.AddFirst(new LinkedList<Tile>());
                for (int i = CurrentView.Top; i < MapBuffer + CurrentView.Top; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\L" + ZoomState + @"\" + (CurrentView.Left - 1).ToString() + @"\" + (i + Math.Pow(2, ZoomState - 1) * 1346).ToString());
                        CurrentTiles.First.Value.AddFirst(new Tile(((int)CurrentView.Left - 1) * 256, i * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        CurrentTiles.First.Value.AddFirst(new Tile(((int)CurrentView.Left - 1) * 256, i * 256, EmptyTexture));
                    }
                }

                CurrentTiles.RemoveLast();

                --CurrentView.Left;
                --CurrentView.Right;
            }
            if (CurrentView.Bottom + 1 < 10 && Camera.Pos.Y + Window.ClientBounds.Height > (CurrentView.Bottom + 1) * 256 - precision)   //dół
            {
                LinkedListNode<LinkedList<Tile>> list = CurrentTiles.First;
                for (int i = CurrentView.Left; i < MapBuffer + CurrentView.Left; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\L" + ZoomState + @"\" + i.ToString() + @"\" + (CurrentView.Bottom + 1 + Math.Pow(2, ZoomState - 1) * 1346).ToString());
                        list.Value.AddLast(new Tile(i * 256, ((int)CurrentView.Bottom + 1) * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        list.Value.AddLast(new Tile(i * 256, ((int)CurrentView.Bottom + 1) * 256, EmptyTexture));
                    }

                    list.Value.RemoveFirst();
                    list = list.Next;
                }

                ++CurrentView.Top;
                ++CurrentView.Bottom;
            }
            if (CurrentView.Top * 256 > 0 && Camera.Pos.Y < CurrentView.Top * 256 + precision)   //góra
            {
                LinkedListNode<LinkedList<Tile>> list = CurrentTiles.First;
                for (int i = CurrentView.Left; i < MapBuffer + CurrentView.Left; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\L" + ZoomState + @"\" + i.ToString() + @"\" + (CurrentView.Top - 1 + Math.Pow(2, ZoomState - 1) * 1346).ToString());
                        list.Value.AddFirst(new Tile(i * 256, ((int)CurrentView.Top - 1) * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        list.Value.AddFirst(new Tile(i * 256, ((int)CurrentView.Top - 1) * 256, EmptyTexture));
                    }

                    list.Value.RemoveLast();
                    list = list.Next;
                }

                --CurrentView.Top;
                --CurrentView.Bottom;
            }
        }

        //dodaj linię do listy
        private void AddLine(Vector2 x, Vector2 y, Color color)
        {
            Lines.AddLast(new Line(x, y, color));
        }
    }
}
