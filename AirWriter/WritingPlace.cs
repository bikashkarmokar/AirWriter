using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

//for drawing purpose
using System.Windows.Forms;
using System.Drawing;

namespace AirWriter
{
    public partial class WritingPlace : Panel
    {
        private Bitmap BufferImage = null;
        private bool MouseMoving = false;

        private Graphics g_Image = null;
        private Graphics g_Panel = null;

        private int p_Size = 10;//Default Point Size


        public WritingPlace()
        {
            InitializeComponent();
            InitializeWritingPlace();
        }

        private void InitializeWritingPlace()
        {
            //MessageBox.Show("AierWriter");
            CreateNewImage();
            base.MouseDown += new MouseEventHandler(WritingPlace_MouseDown);
            base.MouseUp += new MouseEventHandler(WritingPlace_MouseUp); 
            base.MouseMove += new MouseEventHandler(WritingPlace_MouseMove);
            base.Paint += new PaintEventHandler(WritingPlace_Paint);
            base.Resize += new EventHandler(WritingPlace_Resize);
            //base.BackColor = Color.White;
        }


        //this is for system theke picture browse kore nie asar jonno
        void WritingPlace_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(BufferImage, 0, 0);
            g.Dispose();
        }

        private void CreateNewImage()
        {
            BufferImage = new Bitmap(base.Width, base.Height);

            for (int i = 0; i < BufferImage.Height; i++)
                for (int j = 0; j < BufferImage.Width; j++)
                    //BufferImage.SetPixel(j, i, Color.Blue);
                    BufferImage.SetPixel(j, i, Color.White);
        }

        public void WritigPlaceClear()
        {
            CreateNewImage();
            base.Invalidate();//eita picture clear korar jonno dite hobe;
        }

        //void WritingPlace_Paint

        //resize na dile panel er size boro korleo panel ta byDefault je size 
        //pai setai thake ..so panel boro korleo size constant thake
        void WritingPlace_Resize(object sender, EventArgs e)
        {
            CreateNewImage();
        }
        public static int colorsl=0;
        void WritingPlace_MouseDown(object sender, MouseEventArgs e)
        {
            
            //MessageBox.Show("air");
            MouseMoving = true;
            if (colorsl == 1)
            {
                colorsl = 0;
            }
            else if (colorsl == 0)
            {
                colorsl = 1;
            }
        }


        void WritingPlace_MouseMove(object sender, MouseEventArgs e)
        {
            if (colorsl == 1)
            {
                if (MouseMoving)// &&
                //(e.X < (base.Width - p_Size * .85)) && (e.Y < (base.Height - p_Size * .85)) &&
                //(e.X > -p_Size * .15) && (e.Y > -p_Size * .15))//To Prevent Drawing Over The Panel 
                {
                    //Draw The Point to The Image
                    g_Image = Graphics.FromImage(BufferImage);
                    g_Image.FillEllipse(new SolidBrush(Color.Black), e.X, e.Y, p_Size, p_Size);

                    //Draw The Image to The Panel
                    g_Panel = base.CreateGraphics();
                    g_Panel.DrawImage(BufferImage, 0, 0);
                }
            }

            else if (colorsl == 0)
            {
                if (MouseMoving)// &&
                //(e.X < (base.Width - p_Size * .85)) && (e.Y < (base.Height - p_Size * .85)) &&
                //(e.X > -p_Size * .15) && (e.Y > -p_Size * .15))//To Prevent Drawing Over The Panel 
                {
                    //Draw The Point to The Image
                    g_Image = Graphics.FromImage(BufferImage);
                    g_Image.FillEllipse(new SolidBrush(Color.White), e.X, e.Y, p_Size, p_Size);

                    //Draw The Image to The Panel
                    g_Panel = base.CreateGraphics();
                    g_Panel.DrawImage(BufferImage, 0, 0);
                }
            }
        }

        void WritingPlace_MouseUp(object sender, MouseEventArgs e)
        {
            MouseMoving = false;
        }

        public Bitmap CharacterOnPanel
        {
            get
            {
                return BufferImage;
            }
            set
            {
                BufferImage = value;
                base.Invalidate();
            }
        }

        public int PointSize
        {
            get { return p_Size; }
            set { p_Size = value; }
        }

        //For Giving BuitIn name of panel
        string name = "Lekhoni";
        public string NameGiven
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        int color = 1;
        public int ColorSelector
        {
            get { return color; }
            set { color = value; }
        }               

    }
}
