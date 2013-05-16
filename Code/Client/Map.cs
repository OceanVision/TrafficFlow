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
    public class View
    {
        public int top, right, bottom, left;

        public View(int t, int r, int b, int l)
        {
            top = t; right = r; bottom = b; left = l;
        }
    }

    public class Tile
    {
        public int X;
        public int Y;
        public Texture2D texture;

        public Tile(int x, int y, Texture2D tex)
        {
            X = x; Y = y; texture = tex;
        }
    }

    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        //ustawia zoom
        private void zoomUpdate(float inc)
        {
            camera.Zoom += inc;
        }

        //przesuwa camere
        KeyboardState keyboardState;
        Vector2 keyboardMovement;
        private void cameraUpdate()
        {
            keyboardState = Keyboard.GetState();
            keyboardMovement = Vector2.Zero;
            const int precision = 10;

            if (keyboardState.IsKeyDown(Keys.Left)) keyboardMovement.X--;
            if (keyboardState.IsKeyDown(Keys.Right)) keyboardMovement.X++;
            if (keyboardState.IsKeyDown(Keys.Up)) keyboardMovement.Y--;
            if (keyboardState.IsKeyDown(Keys.Down)) keyboardMovement.Y++;

            camera.Pos += keyboardMovement * precision;
        }

        //wiadomo
        MouseState originalMouseState;
        private void mouseUpdate()
        {
            MouseState currentMouseState = Mouse.GetState();
            if (currentMouseState != originalMouseState)
                debug.Text = (currentMouseState.X - originalMouseState.X).ToString() + " " + (currentMouseState.Y - originalMouseState.Y);
        }

        //aktualizuje kafelki mapy
        private void tilesUpdate()
        {
            const int precision = 200;  //255 max
            Texture2D texture;
            if (currentView.right + 1 < 10 && camera.Pos.X + Window.ClientBounds.Width > (currentView.right + 1) * 256 - precision)   //prawo
            {
                currentTiles.AddLast(new LinkedList<Tile>());
                for (int i = currentView.top; i < mapBuffer + currentView.top; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\" + (currentView.right + 1).ToString() + @"\" + (i + 2692).ToString());
                        currentTiles.Last.Value.AddLast(new Tile(((int)currentView.right + 1) * 256, i * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        currentTiles.Last.Value.AddLast(new Tile(((int)currentView.right + 1) * 256, i * 256, emptyTexture));
                    }
                }

                currentTiles.RemoveFirst();

                ++currentView.left;
                ++currentView.right;
            }
            if (currentView.left * 256 > 0 && camera.Pos.X < currentView.left * 256 + precision)     //lewo
            {
                currentTiles.AddFirst(new LinkedList<Tile>());
                for (int i = currentView.top; i < mapBuffer + currentView.top; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\" + (currentView.left - 1).ToString() + @"\" + (i + 2692).ToString());
                        currentTiles.First.Value.AddFirst(new Tile(((int)currentView.left - 1) * 256, i * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        currentTiles.First.Value.AddFirst(new Tile(((int)currentView.left - 1) * 256, i * 256, emptyTexture));
                    }
                }

                currentTiles.RemoveLast();

                --currentView.left;
                --currentView.right;
            }
            if (currentView.bottom + 1 < 10 && camera.Pos.Y + Window.ClientBounds.Height > (currentView.bottom + 1) * 256 - precision)   //dół
            {
                LinkedListNode<LinkedList<Tile>> list = currentTiles.First;
                for (int i = currentView.left; i < mapBuffer + currentView.left; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\" + i.ToString() + @"\" + (currentView.bottom + 1 + 2692).ToString());
                        list.Value.AddLast(new Tile(i * 256, ((int)currentView.bottom + 1) * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        list.Value.AddLast(new Tile(i * 256, ((int)currentView.bottom + 1) * 256, emptyTexture));
                    }

                    list.Value.RemoveFirst();
                    list = list.Next;
                }

                ++currentView.top;
                ++currentView.bottom;
            }
            if (currentView.top * 256 > 0 && camera.Pos.Y < currentView.top * 256 + precision)   //góra
            {
                LinkedListNode<LinkedList<Tile>> list = currentTiles.First;
                for (int i = currentView.left; i < mapBuffer + currentView.left; ++i)
                {
                    try
                    {
                        texture = Content.Load<Texture2D>(@"Tiles\" + i.ToString() + @"\" + (currentView.top - 1 + 2692).ToString());
                        list.Value.AddFirst(new Tile(i * 256, ((int)currentView.top - 1) * 256, texture));
                    }
                    catch (ContentLoadException e)
                    {
                        list.Value.AddFirst(new Tile(i * 256, ((int)currentView.top - 1) * 256, emptyTexture));
                    }

                    list.Value.RemoveLast();
                    list = list.Next;
                }

                --currentView.top;
                --currentView.bottom;
            }
        }
    }
}
