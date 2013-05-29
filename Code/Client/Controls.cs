using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace TrafficFlow
{
    public partial class Visualization : Microsoft.Xna.Framework.Game
    {
        private Button StartProgram, RouteModeSwitch, CarControl, FindRoute, ClearRoute;
        public Label LoadingText, InfoLabel;
        public NumericUpDown StartTime;

        //initializes all controls
        public void InitControls()
        {
            //buttons initialization
            CarControl = new Button();
            CarControl.Text = "Start car";
            CarControl.Size = new Size(120, 30);
            CarControl.FlatStyle = FlatStyle.Flat;
            CarControl.BackColor = Color.White;
            CarControl.ForeColor = Color.FromArgb(55, 53, 53);
            CarControl.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            CarControl.Location = new Point(Window.ClientBounds.Width - 150, Window.ClientBounds.Height - 180);
            Control.FromHandle(Window.Handle).Controls.Add(CarControl);
            CarControl.Click += new System.EventHandler(CarControl_Click);
            CarControl.Visible = false;
            NotMapAreas.AddLast(new Microsoft.Xna.Framework.Rectangle(CarControl.Location.X, CarControl.Location.Y, CarControl.Size.Width, CarControl.Size.Height));

            FindRoute = new Button();
            FindRoute.Text = "Find route";
            FindRoute.Size = new Size(120, 30);
            FindRoute.FlatStyle = FlatStyle.Flat;
            FindRoute.BackColor = Color.White;
            FindRoute.ForeColor = Color.FromArgb(55, 53, 53);
            FindRoute.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            FindRoute.Location = new Point(Window.ClientBounds.Width - 150, Window.ClientBounds.Height - 140);
            Control.FromHandle(Window.Handle).Controls.Add(FindRoute);
            FindRoute.Click += new System.EventHandler(FindRoute_Click);
            FindRoute.Visible = false;
            NotMapAreas.AddLast(new Microsoft.Xna.Framework.Rectangle(FindRoute.Location.X, FindRoute.Location.Y, FindRoute.Size.Width, FindRoute.Size.Height));

            ClearRoute = new Button();
            ClearRoute.Text = "Clear route";
            ClearRoute.Size = new Size(120, 30);
            ClearRoute.FlatStyle = FlatStyle.Flat;
            ClearRoute.BackColor = Color.White;
            ClearRoute.ForeColor = Color.FromArgb(55, 53, 53);
            ClearRoute.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            ClearRoute.Location = new Point(Window.ClientBounds.Width - 150, Window.ClientBounds.Height - 100);
            Control.FromHandle(Window.Handle).Controls.Add(ClearRoute);
            ClearRoute.Click += new System.EventHandler(ClearRoute_Click);
            ClearRoute.Visible = false;
            NotMapAreas.AddLast(new Microsoft.Xna.Framework.Rectangle(ClearRoute.Location.X, ClearRoute.Location.Y, ClearRoute.Size.Width, ClearRoute.Size.Height));

            RouteModeSwitch = new Button();
            RouteModeSwitch.Text = "Calculate route";
            RouteModeSwitch.Size = new Size(150, 30);
            RouteModeSwitch.FlatStyle = FlatStyle.Flat;
            RouteModeSwitch.BackColor = Color.White;
            RouteModeSwitch.ForeColor = Color.FromArgb(55, 53, 53);
            RouteModeSwitch.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            RouteModeSwitch.Location = new Point(Window.ClientBounds.Width - 180, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(RouteModeSwitch);
            RouteModeSwitch.Click += new System.EventHandler(RouteModeSwitch_Click);
            RouteModeSwitch.Visible = false;
            RouteModeSwitch.Enabled = false;
            NotMapAreas.AddLast(new Microsoft.Xna.Framework.Rectangle(RouteModeSwitch.Location.X, RouteModeSwitch.Location.Y, RouteModeSwitch.Size.Width, RouteModeSwitch.Size.Height));

            StartProgram = new Button();
            StartProgram.Text = "Start";
            StartProgram.Size = new Size(100, 30);
            StartProgram.FlatStyle = FlatStyle.Flat;
            StartProgram.BackColor = Color.White;
            StartProgram.ForeColor = Color.FromArgb(55, 53, 53);
            StartProgram.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            StartProgram.Location = new Point(Window.ClientBounds.Width - 130, Window.ClientBounds.Height - 60);
            Control.FromHandle(Window.Handle).Controls.Add(StartProgram);
            StartProgram.Click += new System.EventHandler(StartProgram_Click);
            StartProgram.Visible = true;
            
            LoadingText = new Label();
            LoadingText.AutoSize = true;
            LoadingText.BackColor = Color.White;
            LoadingText.ForeColor = Color.Green;
            LoadingText.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            LoadingText.Text = "The server is starting... 0%";
            LoadingText.Location = new Point(30, Window.ClientBounds.Height - 58);
            LoadingText.Visible = false;
            Control.FromHandle(Window.Handle).Controls.Add(LoadingText);
            NotMapAreas.AddLast(new Microsoft.Xna.Framework.Rectangle(LoadingText.Location.X, LoadingText.Location.Y, LoadingText.Size.Width, LoadingText.Size.Height));


            StartTime = new NumericUpDown();
            StartTime.Size = new Size(60, 30);
            StartTime.BorderStyle = BorderStyle.FixedSingle;
            StartTime.BackColor = Color.White;
            StartTime.ForeColor = Color.Green;
            StartTime.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            StartTime.Minimum = 0;
            StartTime.Maximum = 500;
            StartTime.Value = 0;
            StartTime.Location = new Point(Window.ClientBounds.Width - 200, Window.ClientBounds.Height - 60);
            StartTime.Visible = true;
            Control.FromHandle(Window.Handle).Controls.Add(StartTime);


            InfoLabel = new Label();
            InfoLabel.AutoSize = true;
            InfoLabel.BackColor = Color.White;
            InfoLabel.ForeColor = Color.Green;
            InfoLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            InfoLabel.Text = "Time in minutes to start simulation:";
            InfoLabel.Location = new Point(Window.ClientBounds.Width - 480, Window.ClientBounds.Height - 58);
            InfoLabel.Visible = true;
            Control.FromHandle(Window.Handle).Controls.Add(InfoLabel);
        }

        private void UpdateControls()
        {
            if (!NetworkLoaded)
                return;

            RouteModeSwitch.Visible = true;

            if (Car.GetState == 2)
                CarControl.Text = "Pause car";
            else if (Car.GetState != 2)
                CarControl.Text = "Start car";

            if (Car.Count > 0)
                CarControl.Enabled = true;
            else
                CarControl.Enabled = false;

            if (SelectedPoints.Count > 1)
                FindRoute.Enabled = true;
            else
                FindRoute.Enabled = false;
        }

        private void RouteModeSwitch_Click(object sender, EventArgs e)
        {
            if (!RouteMode)
            {
                RouteModeSwitch.BackColor = Color.FromArgb(55, 53, 53);
                RouteModeSwitch.ForeColor = Color.White;
                CarControl.Visible = true;
                FindRoute.Visible = true;
                ClearRoute.Visible = true;
                RouteMode = true;
            }
            else
            {
                RouteModeSwitch.BackColor = Color.White;
                RouteModeSwitch.ForeColor = Color.FromArgb(55, 53, 53);
                CarControl.Visible = false;
                FindRoute.Visible = false;
                ClearRoute.Visible = false;
                RouteMode = false;
            }
        }

        //executes if CarControl is clicked 
        private void CarControl_Click(object sender, EventArgs e)
        {
            if (Car.GetState == 0)
                Car.Start();
            else if (Car.GetState == 1)
                Car.Start();
            else if (Car.GetState == 2)
                Car.Pause();
        }

        //executes if FindRoute is clicked 
        private void FindRoute_Click(object sender, EventArgs e)
        {
            BuildRoute();
        }

        //executes if ClearRoute is clicked 
        private void ClearRoute_Click(object sender, EventArgs e)
        {
            SelectedPoints.Clear();
            Route.Clear();
            Car.Stop();
        }

        //executes if StartProgram is clicked
        private void StartProgram_Click(object sender, EventArgs e)
        {
            if (StartTime.Value < 0)
                return;

            ServerStartTime = (int)StartTime.Value;
            StartTime.Visible = false;
            StartProgram.Visible = false;
            InfoLabel.Visible = false;
            LoadingText.Visible = true;

            //creates thread that takes care of traffic network
            Thread thread = new Thread(new ThreadStart(UpdateNetwork));
            thread.Start();
            while (!thread.IsAlive) ;
        }
    }
}
