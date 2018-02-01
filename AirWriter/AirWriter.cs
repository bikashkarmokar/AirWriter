using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

//for spech;
using SpeechLib;
using System.Media;
//for Aforge.NET
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Video.DirectShow;
//for thread
using System.Threading;
//for neural lib
using AirWriter.Lib;
//for BitmapData
using System.Drawing.Imaging;

//for NameValueCollection
using System.Collections.Specialized;

//for path in xml
using System.Configuration;
using System.IO;




namespace AirWriter
{
    public partial class AirWriter : Form
    {
        int objectX;
        int objectY;
        int ax;
        int by;
        int axp;
        int ayp;

        //*************for camera input
        private FilterInfoCollection videodevices;      
        GrayscaleBT709 grayscaleFilter = new GrayscaleBT709();
        //for rectangle boxes in picture
        BlobCounter blobCounter = new BlobCounter();
        //*******************************************

        //Neural Network Object With Output Type String
        private NeuralNetwork<string> neuralNetwork = null;

        //Data Members Required For Neural Network

        private Dictionary<string, double[]> TrainingSet = null;
        private int av_ImageHeight = 0;
        private int av_ImageWidth = 0;
        private int NumOfPatterns = 0;

        //For Asynchronized Programming Instead of Handling Threads
        //**************************************************************
        private delegate bool TrainingCallBack();// TrainingCallBack 1 or 0 return karar katha

        private AsyncCallback asyCallBack = null;
        //Reference a method to be called when a corresponding asynchronous operation complets               
        private IAsyncResult res = null;//Represents the status of an asynchronous operation
        private ManualResetEvent ManualReset = null;
        //Notifies one or more waiting threads that an event has occurred. This class cannot be inherited


        private DateTime DTStart;       

        public AirWriter()
        {
            InitializeComponent();

            //for camera initia
            StartMonitoringCamera();
            //for neural network
            InitializeSettings();
            //ভুএ();              
            GenerateTrainingSet();
            CreateNeuralNetwork();          

            asyCallBack = new AsyncCallback(TraningCompleted);
            ManualReset = new ManualResetEvent(false);
            
            
        }
       
        private void ভুএ()
        {
            MessageBox.Show("Bikash");
        }

        private void CreateNeuralNetwork()
        {
            if (TrainingSet == null)
                throw new Exception("Unable to Create Neural Network As There is No Data to Train..");

            //****************************bks******************
            if (comboBoxLayers.SelectedIndex == 0)
            {

                neuralNetwork = new NeuralNetwork<string>
                    (new BP1Layer<string>(av_ImageHeight * av_ImageWidth, NumOfPatterns), TrainingSet);

            }
            //else if (comboBoxLayers.SelectedIndex == 1)
            //{
            //    int InputNum = Int16.Parse(textBoxInputUnit.Text);

            //    neuralNetwork = new NeuralNetwork<string>
            //        (new BP2Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, NumOfPatterns), TrainingSet);

            //}
            //else if (comboBoxLayers.SelectedIndex == 2)
            //{
            //    int InputNum = Int16.Parse(textBoxInputUnit.Text);
            //    int HiddenNum = Int16.Parse(textBoxHiddenUnit.Text);

            //    neuralNetwork = new NeuralNetwork<string>
            //        (new BP3Layer<string>(av_ImageHeight * av_ImageWidth, InputNum, HiddenNum, NumOfPatterns), TrainingSet);

            //}

            //tinta layer er jonno tin rokom coz go there note ache
            //**********************************************************************
           // MessageBox.Show("এই পর্যন্ত সব ঠিক আছে।");            
            neuralNetwork.IterationChanged += new NeuralNetwork<string>.IterationChangedCallBack(neuralNetwork_IterationChanged);

            neuralNetwork.MaximumError = Double.Parse(textBoxMaxError.Text); // settings page er maximum error textbox
        }


        void neuralNetwork_IterationChanged(object o, NeuralEventArgs args)
        {
            UpdateError(args.CurrentError);
            UpdateIteration(args.CurrentIteration);

            if (ManualReset.WaitOne(0, true))
                args.Stop = true;
        }   

        private void GenerateTrainingSet() // এইটা প্রত্যেক টা ইমেজ এর ডাটা training set এর মধ্যে রাখবে।
        {
            // textBoxState.AppendText("Generating Training Set..");

            string[] Patterns = Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");

            TrainingSet = new Dictionary<string, double[]>(Patterns.Length);
            foreach (string s in Patterns)
            {
                //private Dictionary<string, double[]> TrainingSet = null
                // eita upor theke copy kore nie aschi  ----bks
                //training set ekta array er moto jar indexing 0 theke suru hoi see (2008book-file dictionary bks)
                //এই খানে training set er moddhe দুই টাই থাকবে imagename,double result(4m imagerossin.tomatrix)

                Bitmap Temp = new Bitmap(s);
                TrainingSet.Add(Path.GetFileNameWithoutExtension(s), ImageProcessing.ToMatrix(Temp, av_ImageHeight, av_ImageWidth));
                //ImageProcessing class er Tomatrix function
                Temp.Dispose();
            }

            // textBoxState.AppendText("Done!\r\n");
           // MessageBox.Show("Generate traning set thik ache");
        }


        private void StartMonitoringCamera()
        {
            //**********************block for bringing camera dievice in combo box.......start
            try
            {
                videodevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videodevices.Count == 0)
                    throw new ApplicationException();

                foreach (FilterInfo device in videodevices)
                {
                    comboBox_camera.Items.Add(device.Name);
                }
                comboBox_camera.SelectedIndex = 0; //onno pc te jodi camera akta thake then 0 hobe
            }
            catch (ApplicationException)
            {
                //comboBox_camera.Items.Add("No Device Found");
                MessageBox.Show("No Device Is Found");
                videodevices = null;
            }
            //*********************for bringing camera dievice in combo box.......end


            //// pictureBox1 making white
            //Bitmap b = new Bitmap(320, 240);
            //// Rectangle a = (Rectangle)r;
            //Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
            //Graphics g2 = Graphics.FromImage(b);
            //pen1 = new Pen(Color.FromArgb(255, 0, 0), 3);
            //g2.Clear(Color.White);
            ////g2.DrawLine(pen1, b.Width / 2, 0, b.Width / 2, b.Width);
            ////g2.DrawLine(pen1, b.Width, b.Height / 2, 0, b.Height / 2);
            //pictureBox1.Image = (System.Drawing.Image)b;
        }
        private void InitializeSettings()
        {
            try
            {
            //thisworks for getting the current file path of pattern
            //for this have .IO and .configuration is needed and reference

            textBoxTrainingBrowse.Text = Path.GetFullPath(ConfigurationSettings.AppSettings["PatternsDirectory"]);
            //first index -1, 2nd +0/-0 (just 0 error dekhabe) , third +1,4th +2
            comboBoxLayers.SelectedIndex = (Int16.Parse(ConfigurationSettings.AppSettings["NumOfLayers"]) - 1);// ei khane 0 dea chilo tai object reference
                //er problem hochilo coz amar akahen just 1 ta layer enable kora ache.
            textBoxMaxError.Text = ConfigurationSettings.AppSettings["MaxError"];
            textBox_voiceDirectory.Text = Path.GetFullPath(ConfigurationSettings.AppSettings["VoiceDirectory"]);

            //***********অন্য ঊপায়******************
            //xml file থেকে ফল্ডার এর ফুল পথ নিয়ে আসার জন্য
            //forNameValueCollection needed using System.Collections.Specialized; and 
            //for ConfigurationManager needed add reference to .NET>system.configuration

            //NameValueCollection AppSettings = ConfigurationManager.AppSettings;
            //comboBoxLayers.SelectedIndex = (Int16.Parse(AppSettings["NumOfLayers"]) - 1);
            //textBoxTrainingBrowse.Text = Path.GetFullPath(AppSettings["PatternsDirectory"]);
            //textBoxMaxError.Text = AppSettings["MaxError"];
            //**********

            
                string[] Images = Directory.GetFiles(textBoxTrainingBrowse.Text, "*.bmp");
                NumOfPatterns = Images.Length;
                //patterns a je koi ta pic ta ache tar sonkha 
                //********************

                av_ImageHeight = 0;
                av_ImageWidth = 0;

                foreach (string s in Images)
                {
                    Bitmap Temp = new Bitmap(s);
                    av_ImageHeight += Temp.Height;
                    av_ImageWidth += Temp.Width;
                    Temp.Dispose();
                }
                av_ImageHeight /= NumOfPatterns;
                av_ImageWidth /= NumOfPatterns;

                //avarage image height ber kora hoise pattern folder er image er upor vitti kore
                //******************************************************************************

                int networkInput = av_ImageHeight * av_ImageWidth;

                //textBoxInputUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .33)).ToString();
                //textBoxHiddenUnit.Text = ((int)((double)(networkInput + NumOfPatterns) * .11)).ToString();
                textBoxOutputUnit.Text = NumOfPatterns.ToString();

                /// .33/.11 dara keno gun kora holo?
                //tab settings page er text box gulote valu insert kora holo
                //**********************************************


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Initializing Settings: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //************************************for smooth opening
            // Set Form's Transperancy 100 %
            this.Opacity = 0;            
            // Start the Timer To Animate Form
            timer2.Enabled = true;
            //***************************************************
            
            ////for camera starting
            //VideoCaptureDevice vs = new VideoCaptureDevice(videodevices[comboBox_camera.SelectedIndex].MonikerString);
            ////VideoCaptureDevice vs = new VideoCaptureDevice(videodivices[1].MonikerString);
            //vs.DesiredFrameSize = new Size(320, 240);
            //vs.DesiredFrameRate = 12;
            //videoSourcePlayer1.VideoSource = vs;
            ////videoSourcePlayer1.Start();
            ////videoSourcePlayer2.VideoSource = vs;
            ////videoSourcePlayer2.Start();
        }
        
        

        private void button6_Click(object sender, EventArgs e)
        {            
            NetworkControl form = new NetworkControl();                
            form.Show();                                      
        }

        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap image)
        {
            Bitmap objectsImage = null;
            Bitmap mImage = null;
            mImage = (Bitmap)image.Clone();


            EuclideanColorFiltering filter = new EuclideanColorFiltering();
            // set center colol and radius
            //filter.CenterColor = Color.FromArgb(0, 0, 0);
            //filter.CenterColor = Color.White;
            filter.CenterColor = Color.Red;
            filter.Radius = 120;
            // apply the filter
            objectsImage = image; // bitmap er akta object a; image ta asceh parametter theke
            //new_frame er image objectImage a prottek bar asche;

            filter.ApplyInPlace(objectsImage);
            //filter.ApplyInPlace(image);

            BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);

            //grayscaling
            UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));

            //unlock image
            objectsImage.UnlockBits(objectsData);

            //locate blobs 
            blobCounter.ProcessImage(grayImage);
            Rectangle[] rects = blobCounter.GetObjectRectangles();//rectangle rects er moddhe joto rectangle thakbe ta store kora thakbe
            //image = mImage;
            if (rects.Length > 0)
            {

                foreach (Rectangle objectRect in rects)
                {
                    // Rectangle objectRect = rects[0];

                    // draw rectangle around derected object
                    Graphics g = Graphics.FromImage(mImage);

                   // using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 5))
                    using (Pen pen = new Pen(Color.Red))
                    {
                        g.DrawRectangle(pen, objectRect);
                    }

                    g.Dispose();

                    //int objectX = objectRect.X + objectRect.Width / 2 - image.Width / 2;
                    //int objectY = image.Height / 2 - (objectRect.Y + objectRect.Height / 2);

                    
                    ParameterizedThreadStart thd = new ParameterizedThreadStart(writer);
                    Thread aa = new Thread(thd);
                    aa.Start(rects[0]);
                    Thread.Sleep(50);
                }
            }

            image = mImage;
        }        


        void writer(object r)
        {
            

            try
            {

                Bitmap b = new Bitmap(writingPlace3.CharacterOnPanel);
                Rectangle a = (Rectangle)r;
                //Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
                //Pen pen1 = new Pen(Color.Black);
                Graphics g2 = Graphics.FromImage(b);
               // pen1 = new Pen(Color.FromArgb(255, 0, 0), 3);
                // Brush b5 = null;
                SolidBrush sldbrs = new SolidBrush(Color.Black);
                //   g2.Clear(Color.Black);

                
                //**********************
                System.Drawing.Graphics graphicsObj;

                graphicsObj = this.CreateGraphics();

                Font f = new System.Drawing.Font("bksprjt", 40, FontStyle.Bold);

                //Brush myBrush = new SolidBrush(System.Drawing.Color.Red);

                //graphicsObj.DrawString("Hello C#", myFont, myBrush, 30, 30);

                //**********************
                //g2.DrawString("A", f, sldbrs, 137, 10);
                //g2.DrawString("I", f, sldbrs, 137, 40);
                //g2.DrawString("R", f, sldbrs, 137, 70);                
                //Font f = new Font(Font, FontStyle.Bold);
                
                //*********                              
                objectX = a.Location.X;
                objectY = a.Location.Y;// - a.Height;
                ax = objectX;
                by = objectY;

                if (objectX > 137)
                {
                    ax = objectX - 137;
                    objectX = 137 - ax;
                    //MessageBox.Show("আমি");
                }
                else if (objectX < 137)
                {
                    by = 137 - objectX;
                    objectX = 137 + by;
                }
                //*********

                //g2.DrawString(".", f, sldbrs, objectX, objectY);  
              
                

                g2.DrawString(".", f, sldbrs, objectX,objectY);

                //last x=274
                //last y=
                //MessageBox.Show(a.Location.ToString());
                //Console.Write(a.Location);
                //g2.FillEllipse(new SolidBrush(Color.Black), objectX, objectY, 10, 10);
                //g2.FillEllipse(new SolidBrush(Color.Black), e.X, e.Y, p_Size, p_Size);
                
                g2.Dispose();



                //pictureBox1.Image = (System.Drawing.Image)b;
                //writingPlace3.CharacterOnPanel = (System.Drawing.Bitmap)b;

               
                this.Invoke((MethodInvoker)delegate
                {
                    writingPlace3.CharacterOnPanel = (System.Drawing.Bitmap)b;                  
                    
                });
            }
            catch (Exception faa)
            {
                Thread.CurrentThread.Abort();
            }


            Thread.CurrentThread.Abort();

        }//void p ses

        private void AirWriter_FormClosed(object sender, FormClosedEventArgs e)
        {
            videoSourcePlayer1.Stop();
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            //// pictureBox1 making white
            ////*************************************
            //Bitmap b = new Bitmap(320, 240);
            //// Rectangle a = (Rectangle)r;
            //Pen pen1 = new Pen(Color.FromArgb(160, 255, 160), 3);
            //Graphics g2 = Graphics.FromImage(b);
            //pen1 = new Pen(Color.FromArgb(255, 0, 0), 3);
            //g2.Clear(Color.White);
            ////g2.DrawLine(pen1, b.Width / 2, 0, b.Width / 2, b.Width);
            ////g2.DrawLine(pen1, b.Width, b.Height / 2, 0, b.Height / 2);
            //pictureBox1.Image = (System.Drawing.Image)b;
            

            //panel clear korar jonno;
            //**************************
            writingPlace3.WritigPlaceClear();
                   
        }

        private void buttonTrain_Click(object sender, EventArgs e)
        {
            ManualReset.Reset();//bool EventWaitHandle.Reset()/>Sets the state of the event to nonsigned, causing threads to block.


            TrainingCallBack TR = new TrainingCallBack(neuralNetwork.Train);
            res = TR.BeginInvoke(asyCallBack, TR);

            DTStart = DateTime.Now;
            timer1.Start();
        }
        private void TraningCompleted(IAsyncResult result)
        {
            if (result.AsyncState is TrainingCallBack)
            {
                TrainingCallBack TR = (TrainingCallBack)result.AsyncState;
                timer1.Stop();
                MessageBox.Show("আমি প্যাটার্ন সম্পর্কে একটা ধারনা নিয়েছি। আপনি লিখা শুরু করতে পারেন।");
            }
            //button_recognize.Enabled = true; এখানে button enable বা disable kora jabe na coz eita thread theke call hoche
           
        }
       
        private void button_stop_Click(object sender, EventArgs e)
        {
            ManualReset.Set();
        }
        private void button_recognize_Click(object sender, EventArgs e)
        {
            string MatchedHigh = "?", MatchedLow = "?";
            double OutputValueHight = 0, OutputValueLow = 0;

            //Bitmap b = new Bitmap(pictureBox1.Image);

            double[] input = ImageProcessing.ToMatrix(writingPlace3.CharacterOnPanel, av_ImageHeight, av_ImageWidth);

            neuralNetwork.Recognize(input, ref MatchedHigh, ref OutputValueHight, ref MatchedLow, ref OutputValueLow);//***************
            
            //****for testing air
            //MessageBox.Show(MatchedHigh);

            string firstH = MatchedHigh.Substring(0,1);//ei khan theke first letter ta parse kore nichi
            string firstL = MatchedLow.Substring(0, 1);

            //MessageBox.Show(firstH);
            //MessageBox.Show(firstL);
            //MessageBox.Show(OutputValueLow.ToString());
            //ShowRecognitionResults(MatchedHigh, MatchedLow, OutputValueHight, OutputValueLow);
            ShowRecognitionResults(firstH, firstL, OutputValueHight, OutputValueLow);           
            

        }

        private void ShowRecognitionResults(string MatchedHigh, string MatchedLow, double OutputValueHight, double OutputValueLow)
        {
            labelMatchedHigh.Text = "High: " +  " ( " + ((int)100 * OutputValueHight).ToString("##") + "% )";//bangla letter er karone %value ta dekhache na
            labelMatchedLow.Text = "Low: " +  " ( " + ((int)100 * OutputValueLow).ToString("##") + "% )"; // koto percent milse seta show korbe

            //pictureBoxInput.Image = new Bitmap(drawingPanel1.ImageOnPanel, pictureBoxInput.Width, pictureBoxInput.Height); // jeta draw korbo oi ta show korbe

            if (MatchedHigh != "?")
                pictureBoxMatchedHigh.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\" + MatchedHigh + ".bmp"),
                    pictureBoxMatchedHigh.Width, pictureBoxMatchedHigh.Height);

            if (MatchedLow != "?")
                pictureBoxMatchedLow.Image = new Bitmap(new Bitmap(textBoxTrainingBrowse.Text + "\\" + MatchedLow + ".bmp"),
                    pictureBoxMatchedLow.Width, pictureBoxMatchedLow.Height);


            //writing in the text box
            //textBox_recognize.Text += MatchedHigh;
            textBox_recognize.Text = MatchedHigh;
            
            Application.DoEvents();// this for giving time to take voice to speak;
            //writeIntoTextBox();
            speakTheletter();
            
           

        }
        private void writeIntoTextBox()
        {
            //writing in the text box
            //textBox_recognize.Text += MatchedHigh;
            //textBox_recognize.Text = MatchedHigh;
        }
        
        private void speakTheletter()
        {
            //speak out what is written in the air
            //SpVoice voice = new SpVoice();
            //voice.Speak("You write" + textBox_recognize.Text + "in the air", SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
            //voice.Speak("Apni", SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
            
            //SoundPlayer sp = new SoundPlayer(@"D:\অ.wav");
            //SoundPlayer sp = new SoundPlayer(textBox_voiceDirectory.Text + "\\" + textBox_recognize.Text + ".wav");
            //sp.Play();

            if (textBox_recognize.Text != null)
            {
                SoundPlayer sp = new SoundPlayer(textBox_voiceDirectory.Text + "\\" + textBox_recognize.Text + ".wav");
                sp.Play();
            }

            //if (textBox_recognize.Text == "অ")
            //{
            //    SoundPlayer sp = new SoundPlayer(textBox_voiceDirectory.Text + "\\" + textBox_recognize.Text + ".wav");
            //    sp.Play();
            //}
            
            //voice.Speak("Likhechen", SpeechVoiceSpeakFlags.SVSFPurgeBeforeSpeak);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            textBox_RecentText.Text += textBox_recognize.Text;
        }

       
        #region Methods To Invoke UI Components If Required
        
        private delegate void UpdateUI(object o);        
        
        private void UpdateError(object o)
        {
            if (label_error.InvokeRequired)
            {
                label_error.Invoke(new UpdateUI(UpdateError), o);
            }
            else
            {
                label_error.Text = "Error: " + ((double)o).ToString(".###");
            }
        }
        private void UpdateIteration(object o)
        {
            if (label_iteration.InvokeRequired)
            {
                label_iteration.Invoke(new UpdateUI(UpdateIteration), o);
            }
            else
            {
                label_iteration.Text = "Iteration: " + ((int)o).ToString();
            }
        }       

        private void UpdateTimer(object o)
        {
            //if (labelTimer.InvokeRequired)
            if (label_timer.InvokeRequired)
            {
                label_timer.Invoke(new UpdateUI(UpdateTimer), o);
            }
            else
            {
                label_timer.Text = (string)o;
            }
        }

        #endregion
        
        #region RadioButton & CheckBox Event Handlers- Not Important
        private void comboBoxLayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxLayers.SelectedIndex == 0)
            {
               // textBoxInputUnit.Enabled = false;
               // textBoxHiddenUnit.Enabled = false;
            }           
        }
        #endregion
        
        private void timer1_Tick(object sender, EventArgs e)
        {         
           
                TimeSpan TSElapsed = DateTime.Now.Subtract(DTStart);
                UpdateTimer(TSElapsed.Hours.ToString("D2") + ":" +
                TSElapsed.Minutes.ToString("D2") + ":" +
                TSElapsed.Seconds.ToString("D2"));
            
        }

        private void button_restart_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            // Make Form Visible a Bit more on Every timer Tick
            this.Opacity += 0.07;
        }

        private void button_Browse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = "Bitmap Image(*.bmp)|*.bmp";
            fd.InitialDirectory = textBoxTrainingBrowse.Text;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                string filename = fd.FileName;
                if (Path.GetExtension(filename) == ".bmp")
                {
                    //pictureBox1.Image = new Bitmap(new Bitmap(filename), pictureBox1.Width, pictureBox1.Height);
                    writingPlace3.CharacterOnPanel = new Bitmap(new Bitmap(filename), writingPlace3.Width, writingPlace3.Height);
                    //drawingPanel1.ImageOnPanel = new Bitmap(new Bitmap(FileName), drawingPanel1.Width, drawingPanel1.Height);
                }
            }
            fd.Dispose();
        }

       
        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            
            //base.MouseUp +=new MouseEventHandler(AirWriter_MouseUp);
            //AirWriter_MouseDown();

        }
        
        

        private void button_Capture_Click(object sender, EventArgs e)
        {
            //for camera starting
            VideoCaptureDevice vs = new VideoCaptureDevice(videodevices[comboBox_camera.SelectedIndex].MonikerString);
            //VideoCaptureDevice vs = new VideoCaptureDevice(videodivices[1].MonikerString);
            vs.DesiredFrameSize = new Size(320, 240);
            vs.DesiredFrameRate = 12;
            videoSourcePlayer1.VideoSource = vs;
            videoSourcePlayer1.Start();
            //videoSourcePlayer2.VideoSource = vs;
            //videoSourcePlayer2.Start();
        }

        private void button_savechr_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.Filter = "Bitmap Image(*.bmp)|*.bmp";
            fd.InitialDirectory = textBoxTrainingBrowse.Text;

            Bitmap savechar = new Bitmap(writingPlace3.CharacterOnPanel, 30, 33);

            if (fd.ShowDialog() == DialogResult.OK)
            {
               
                savechar.Save(fd.FileName, ImageFormat.Bmp);

                MessageBox.Show("সংরক্ষণ করা হল");
            }
            fd.Dispose();
        }

        
        private void button_erase_Click(object sender, EventArgs e)
        {
            
        }
          
                            
    }
}
