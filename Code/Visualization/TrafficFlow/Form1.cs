﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrafficFlow
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void picture_MouseMove(object sender, MouseEventArgs e)
        {
            CursorPos.Text = Cursor.Position.X + " " + Cursor.Position.Y;
        }

        private void loadBMP_Click(object sender, EventArgs e)
        {
            Bitmap bmp = new Bitmap(@".\Warsaw.bmp");
            picture.Image = bmp;
        }

        private void picture_Click(object sender, EventArgs e)
        {
            Program.testDraw();
            textBox1.Text += Cursor.Position.X + ", " + Cursor.Position.Y + " - 22\n";
        }
    }
}
