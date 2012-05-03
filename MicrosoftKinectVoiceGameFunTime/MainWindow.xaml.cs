//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace KinectAudioDemo
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using System.Collections;
    using Microsoft.Kinect;
    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using MicrosoftKinectVoiceGameFunTime;
    using Microsoft.Xna.Framework;
    using Coding4Fun.Kinect.Wpf;

    public partial class MainWindow : Window
    {

        private const double AngleChangeSmoothingFactor = 0.35;
        private const string AcceptedSpeechPrefix = "Accepted_";
        private const string RejectedSpeechPrefix = "Rejected_";

        private const int WaveImageWidth = 500;
        private const int WaveImageHeight = 100;

        private readonly SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
        private readonly SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        private readonly SolidColorBrush blueBrush = new SolidColorBrush(Colors.Blue);
        private readonly SolidColorBrush blackBrush = new SolidColorBrush(Colors.Black);

        private readonly WriteableBitmap bitmapWave;
        private readonly byte[] pixels;
        private readonly double[] energyBuffer = new double[WaveImageWidth];
        private readonly byte[] blackPixels = new byte[WaveImageWidth * WaveImageHeight];
        private readonly Int32Rect fullImageRect = new Int32Rect(0, 0, WaveImageWidth, WaveImageHeight);

        private KinectSensor kinect;
        private bool running = true;
        private DispatcherTimer readyTimer;
        private EnergyCalculatingPassThroughStream stream;
        private SpeechRecognitionEngine speechRecognizer;

        //my happy things
        private string transmission = "";
        private bool transmissionDone = false;
        private Random rdm = new Random();
        private Stack<airplane> hangar = new Stack<airplane>();
        //private airplane tempAirplane;
        private bool mistake = false;
        //int activeAirplanePosition = 0;
        //Skeleton Stuff
        //bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        //Set Airspace, Hangar, and Difficulty
        int difficulty = 0;
        int totalPlanesInAir = 3;
        Label[] planeLabels;
        System.Windows.Shapes.Ellipse[] ellipseAry;
        airplane [] airspace;
        int airspaceIndex = 0;
        
        //Runway objects
        Runway runway1;
        Runway runway2;
        Runway runway3;

        //Array of fuel tanks to update
        ProgressBar[] fuelTankArray;



        /**********************************************************************************/
        /****************** ALLFRAMESREADY, INITIALIZATION & MAIN WINDOW ******************/
        /**********************************************************************************/

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            runway1.checkTime();
            runway2.checkTime();
            runway3.checkTime();
            for (int i = 0; i < airspace.Length; i++)
            {
                airspace[i].consumeFuel();
            }

            //throw new NotImplementedException();
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame == null || colorFrame == null)
                    {
                        return;
                    }

                    byte[] pixels = new byte[colorFrame.PixelDataLength];//original thingy

                    //***copy data out into our byte array***
                    colorFrame.CopyPixelDataTo(pixels);


                    byte[] depthPixels = enableLanding(depthFrame, pixels);//after messing with depthdata
                    byte[] pixels2 = new byte[pixels.Length];//pixels after modifying the color image



                    int stride = colorFrame.Width * 4; //because RGB + alpha

                    BackdropImage.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96,
                    PixelFormats.Bgr32, null, depthPixels, stride);
                    ColorCameraImage.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96,
                    PixelFormats.Bgr32, null, pixels, stride);

                    // BitmapSource.Create();
                }//end depth frame
            }//end color frame

            Skeleton first = GetFirstSkeleton(e);
            if (first == null)
            {
                return;
            }

            ScalePosition(rightLander, first.Joints[JointType.HandRight]);
            //GetCameraPoint(first, e);
            



        }//end allFramesReady

        private void InitializeKinect()
        {
            var sensor = this.kinect;
            this.speechRecognizer = this.CreateSpeechRecognizer();

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };
            try
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.DepthStream.Enable();
                sensor.SkeletonStream.Enable(parameters);
                sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
                sensor.Start();
            }
            catch (Exception)
            {
                SensorChooser.AppConflictOccurred();
                return;
            }
            if (this.speechRecognizer != null && sensor != null)
            {
                //This example of the readyTimer may help us with landing planes
                // NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);
                this.readyTimer.Start();
                this.ReportSpeechStatus("Initializing audio stream...");
                //this.UpdateInstructionsText(string.Empty);
                this.Closing += this.MainWindowClosing;
            }
            initializeGame();
            this.running = true;
        }//end InitializeKinect

        public void initializeGame()
        {
            
            //This is where we populate the array planeLabels
            Label[] mplaneLabels = { planeLabel1, planeLabel2, planeLabel3 };
            planeLabels = mplaneLabels;
            //This is where we populate the array of FuelTanks
            ProgressBar[] mfuelTankArray = {fuelTank1, fuelTank2, fuelTank3};
            fuelTankArray = mfuelTankArray;
            //We fill the hangar with the initial amount of planes.  This is the total number of planes in the game
            hangar = fillHangar(totalPlanesInAir * 3, difficulty);

            //Now we fill airspace, it is the array of airplanes currently in the air
            airspace = new airplane[totalPlanesInAir];
            for (int i = 0; i < totalPlanesInAir; i++)
            {
                airspace[i] = null;
            }
            fillAirspace();

            runway1 = new Runway(Canvas.GetLeft(runwayRect1), Canvas.GetTop(runwayRect1), runwayRect1.Width, runwayRect1.Height, 1, doNotEnter1);
            runway2 = new Runway(Canvas.GetLeft(runwayRect2), Canvas.GetTop(runwayRect2), runwayRect2.Width, runwayRect2.Height, 2, doNotEnter2);
            runway3 = new Runway(Canvas.GetLeft(runwayRect3), Canvas.GetTop(runwayRect3), runwayRect3.Width, runwayRect3.Height, 3, doNotEnter3);

        }//end initalizeGame

        public MainWindow()
        {
            InitializeComponent();
            var colorList = new List<System.Windows.Media.Color> { Colors.Black, Colors.Green };
            this.bitmapWave = new WriteableBitmap(WaveImageWidth, WaveImageHeight, 96, 96, PixelFormats.Indexed1, new BitmapPalette(colorList));
            this.pixels = new byte[WaveImageWidth];
            for (int i = 0; i < this.pixels.Length; i++)
            {
                this.pixels[i] = 0xff;
            }
            SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;
        }//end MainWindow

        private void SensorChooserKinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = e.OldValue as KinectSensor;
            if (oldSensor != null)
            {
                this.UninitializeKinect();
            }
            KinectSensor newSensor = e.NewValue as KinectSensor;
            this.kinect = newSensor;
            if (newSensor != null)
            {
                this.InitializeKinect();
            }
        }//end SensorChooserKinectSensorChanged

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            this.Start();
            this.ReportSpeechStatus("Ready to recognize speech!");
          //  this.UpdateInstructionsText("Say: 'red', 'green' or 'blue'");
            this.readyTimer.Stop();
            this.readyTimer = null;
        }//end ReadyTimerTick

        private void UninitializeKinect()
        {
            var sensor = this.kinect;
            this.running = false;
            if (this.speechRecognizer != null && sensor != null)
            {
                sensor.AudioSource.Stop();
                sensor.Stop();
                this.speechRecognizer.RecognizeAsyncCancel();
                this.speechRecognizer.RecognizeAsyncStop();
            }

            if (this.readyTimer != null)
            {
                this.readyTimer.Stop();
                this.readyTimer = null;
            }
        }//end UninitializeKinect

        private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UninitializeKinect();
        }//end MainWindowClosing

        private void Start()
        {
            var audioSource = this.kinect.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = audioSource.Start();
            this.stream = new EnergyCalculatingPassThroughStream(kinectStream);
            this.speechRecognizer.SetInputToAudioStream(
                this.stream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);

        }//end Start



        /**********************************************************************************/
        /******************************** AIRPLANE RELATED ********************************/
        /**********************************************************************************/

        private Stack<airplane> fillHangar(int total, int difficulty)
        {
            //fillHangar is called at the beginning of a round to create airplanes for the user to interact with
            Stack<airplane> hangar = new Stack<airplane>();
            for (int i = 0; i < total; i++)
            {
                hangar.Push(new airplane(randomTailnum(4)));
            }
            return hangar;
        }//end fillHangar

        private void checkAllPlanes()
        {
            /*checkAllPlanes determines if a transmission matches a tailnumber 
            of an airplane in the global variable airspace*/
            for (int i = 0; i < airspace.Length; i++)
            {
                if (transmission.Equals(airspace[i].getTailNum()))
                {
                    capturedPlaneNumber.Content = airspace[i].getTailNum();
                    lastUsed.Background = Brushes.Green;
                    //airspace[i] = null;
                    fillAirspace();
                    if (airspaceIndex != -1)
                    {
                        highlightPlaneTag(false);
                    }
                    airspaceIndex = i;
                    highlightPlaneTag(true);
                    return;
                    //TODO: add score
                }
            }
            lastUsed.Background = Brushes.Red;
            airspaceIndex = -1;
        }//end checkAllPlanes

        public void fillAirspace()
        {
        /*fillAirspace populates each element of airspace if it is null
        airspace is global, and therefore does not need to be passed*/
            if (hangar.Count != 0)
            {
                for (int i = 0; i < airspace.Length; i++)
                {
                    if (airspace[i] == null)
                    {
                        //TODO: add try/catch for pop statement (if cannot pop, do something)
                        airspace[i] = hangar.Pop();
                        airspace[i].fuel = fuelTankArray[i];
                        planeLabels[i].Content = airspace[i].getTailNum();
                    }
                }
            }
        }//end fillAirspace

        public void planesToLabel(Label[] l, airplane[] a)
        {
        //When called, this method updates the array of labels with the tailnumbers of an array of planes
            for (int i = 0; i < a.Length; i++)
            {
                l[i].Content = a[i].getTailNum();
            }
        }//end planesToLabel

        private string randomTailnum(int length)
        {
            //This method returns a randomized string to be used as a unique identifier for an airplane when it is constructed
            string tailnum = "N";
            int remianingChars = rdm.Next(length-1);
            int temp;
            for (int i = 0; i <= remianingChars; i++)
            {
                temp = rdm.Next(35);
                if (temp > 25)
                {
                    temp -= 25;
                    tailnum += Convert.ToString(temp);
                }
                else
                {
                    tailnum += Convert.ToString((Char)(temp + 65));
                }
                tailnum = tailnum.ToUpper();
            }
            return tailnum;
        }//end randomTailnum

        public byte[] enableLanding(DepthImageFrame depthFrame, byte[] colorImage)
        {
            //enableLanding is a method that determines when a user is in "landing mode" based off their depth

            short[] rawDepthData = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthData);

            //Height * Width * 4 pixels
            //(Blue, Red, Green, empty pixels per unit of height
            Byte[] pixelies = new Byte[depthFrame.Height * depthFrame.Width * 4];

            //ColorImagePoint[] colorPoint = new ColorImagePoint[depthFrame.PixelDataLength];
            //this.kinect.MapDepthFrameToColorFrame(DepthImageFormat.Resolution640x480Fps30,
            //            rawDepthData,
            //            ColorImageFormat.RgbResolution640x480Fps30,
            //            colorPoint);



            //hardcoded locations to the various pixels
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;
            //const int Opacity = 3;

            for (int depthIndex = 0, colorIndex = 0;
                depthIndex < rawDepthData.Length && colorIndex < pixelies.Length;
                depthIndex++, colorIndex += 4)
            {
                //get the player
                int player = rawDepthData[depthIndex] & DepthImageFrame.PlayerIndexBitmask;

                //get the depth value
                int depth = rawDepthData[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (player != 0 && depth <= 3000)
                {
                    //Begin drawing landers on screen
                    //If "land" command is spoken, AND a lander is matched to a runway, land the lander's plane on runway
                    pixelies[colorIndex + GreenIndex] = 200;
                    pixelies[colorIndex + BlueIndex] = 200;
                    pixelies[colorIndex + RedIndex] = 200;
                }

                else if (player != 0 && depth > 3000)
                {
                    pixelies[colorIndex + GreenIndex] = 0;
                    pixelies[colorIndex + BlueIndex] = 0;
                    pixelies[colorIndex + RedIndex] = 0;
                }

                else
                {

                }

            }//end loopy
            return pixelies;
        }//end enableLanding

        public void checkForLanding(Runway mrunway)
        {
            double landerx = Canvas.GetLeft(rightLander) + (rightLander.Width/2);
            double landery = Canvas.GetTop(rightLander) + (rightLander.Height/2);
            if (!mrunway.occupied && airspaceIndex != -1)
            {
                if (landerx > mrunway.x
                    && landerx < (mrunway.x + mrunway.width)
                    && landery > mrunway.y
                    && landery < (mrunway.y + mrunway.height))
                {
                    Land(mrunway);
                    //rightLander.Fill = Brushes.AntiqueWhite;
                    fillAirspace();
                }
            }

        }

        public void Land(Runway landRunway) 
        {
            landRunway.occupied = true;
            landRunway.mTimer = airspace[airspaceIndex].timer;
            landRunway.redSign.Visibility = Visibility.Visible;
            highlightPlaneTag(false);
            airspace[airspaceIndex] = null;
            airspaceIndex = -1;
            capturedPlaneNumber.Content = "";
            if (checkForGameOver())
            {
                //our happy victory dance
                theBigSecret.Visibility = Visibility.Visible;
                //show game over screen
                blackBackgroundRectangle.Visibility = Visibility.Visible;
            }
        }

        public bool checkForGameOver()
        {
            for(int i=0;i<airspace.Length; i++){
                if(airspace[i] != null)
                return false;
            }
            return true;
        }

        public void highlightPlaneTag(bool trueFalse)
        {
            if (trueFalse)
            {
                planeLabels[airspaceIndex].Background = Brushes.LimeGreen;
            }
            else
            {
                planeLabels[airspaceIndex].Background = Brushes.Transparent;
            }
        }



        /**********************************************************************************/
        /******************************* SKELETAL TRACKING ********************************/
        /**********************************************************************************/

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();
                return first;

            }
        }//end GetFirstSkeleton

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);//center image on the joint
            Canvas.SetTop(element, point.Y - element.Height / 2);//center image on the joint

        }//end CameraPosition

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo((int)canvas1.Width, (int)canvas1.Height); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo((int)canvas1.Width,(int) canvas1.Height, .2f, .2f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);

            Canvas.SetLeft(capturedPlaneNumber, (Canvas.GetLeft(rightLander) + (rightLander.Width / 2)) - (capturedPlaneNumber.Width / 2));
            Canvas.SetTop(capturedPlaneNumber, (Canvas.GetTop(rightLander) + (rightLander.Height / 2)) - (capturedPlaneNumber.Height / 2));

        }//end ScalePosition

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            /*if (first == null)
            {
                return;
            }*/

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    SensorChooser.Kinect == null)
                {
                    return;
                }


                //Map a joint location to a point on the depth map

                //left hand
                /*DepthImagePoint leftDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);*/
                //right hand
                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);


                //Map a depth point to a point on the color image

                //left hand
                /*ColorImagePoint leftColorPoint =
                    depth.MapToColorImagePoint(leftDepthPoint.X, leftDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);*/
                //right hand
                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);


                CameraPosition(rightLander, rightColorPoint);
               //******* ScalePosition(rightLander, first.Joints[JointType.HandRight]);

            }

        }//end GetCameraPoint



        /**********************************************************************************/
        /**************************** SPEECH RECOGNITION & SRE ****************************/
        /**********************************************************************************/

        private SpeechRecognitionEngine CreateSpeechRecognizer()
        {
            RecognizerInfo ri = GetKinectRecognizer();
            if (ri == null)
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
                    Ensure you have the Microsoft Speech SDK installed.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Close();
                return null;
            }

            SpeechRecognitionEngine sre;
            try
            {
                sre = new SpeechRecognitionEngine(ri.Id);
            }
            catch
            {
                MessageBox.Show(
                    @"There was a problem initializing Speech Recognition.
                    Ensure you have the Microsoft Speech SDK installed and configured.",
                    "Failed to load Speech SDK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                this.Close();
                return null;
            }

            var grammar = new Choices();
            grammar.Add("alpha");
            grammar.Add("bravo");
            grammar.Add("charlie");
            grammar.Add("delta");
            grammar.Add("foxtrot");
            grammar.Add("echo");
            grammar.Add("golf");
            grammar.Add("hotel");
            grammar.Add("india");
            grammar.Add("juliet");
            grammar.Add("kilo");
            grammar.Add("lima");
            grammar.Add("mike");
            grammar.Add("november");
            grammar.Add("oscar");
            grammar.Add("papa");
            grammar.Add("quebec");
            grammar.Add("romeo");
            grammar.Add("sierra");
            grammar.Add("tango");
            grammar.Add("uniform");
            grammar.Add("victor");
            grammar.Add("whiskey");
            grammar.Add("x ray");
            grammar.Add("yankee");
            grammar.Add("zulu");
            grammar.Add("zero");
            grammar.Add("one");
            grammar.Add("two");
            grammar.Add("three");
            grammar.Add("four");
            grammar.Add("five");
            grammar.Add("six");
            grammar.Add("seven");
            grammar.Add("eight");
            grammar.Add("niner");
            grammar.Add("over");
            grammar.Add("delete");
            // grammar.Add("elevation");
            grammar.Add("Camera on");
            grammar.Add("Camera off");
            grammar.Add("land");

            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(grammar);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);

            sre.LoadGrammar(g);
            sre.SpeechRecognized += this.SreSpeechRecognized;
            sre.SpeechHypothesized += this.SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;

            return sre;
        }//end CreateSpeechRecognizer

        private void RejectSpeech(RecognitionResult result)
        {
            string status = "Rejected: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            this.ReportSpeechStatus(status);

            //   Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = blackBrush; }), DispatcherPriority.Normal);
        }//end RejectSpeech

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.RejectSpeech(e.Result);
        }//end SreSpeechRecognitionRejected

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            this.ReportSpeechStatus("Hypothesized: " + e.Result.Text + " " + e.Result.Confidence);
        }//end SreSpeechHypothesized

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SolidColorBrush brush;

            if (e.Result.Confidence < 0.5)
            {
                this.RejectSpeech(e.Result);
                return;
            }

            switch (e.Result.Text.ToUpperInvariant())
            {
                case "ALPHA":
                    //brush = this.redBrush;
                    currentCallSign.Content = "A";
                    break;
                case "BRAVO":
                    brush = this.greenBrush;
                    currentCallSign.Content = "B";
                    break;
                case "CHARLIE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "C";
                    break;
                case "DELTA":
                    brush = this.blueBrush;
                    currentCallSign.Content = "D";
                    break;
                case "ECHO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "E";
                    break;
                case "FOXTROT":
                    brush = this.blueBrush;
                    currentCallSign.Content = "F";
                    break;
                case "GOLF":
                    brush = this.blueBrush;
                    currentCallSign.Content = "G";
                    break;
                case "HOTEL":
                    brush = this.blueBrush;
                    currentCallSign.Content = "H";
                    break;
                case "INDIA":
                    brush = this.blueBrush;
                    currentCallSign.Content = "I";
                    break;
                case "JULIET":
                    brush = this.blueBrush;
                    currentCallSign.Content = "J";
                    break;
                case "KILO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "K";
                    break;
                case "LIMA":
                    brush = this.blueBrush;
                    currentCallSign.Content = "L";
                    break;
                case "MIKE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "M";
                    break;
                case "NOVEMBER":
                    brush = this.blueBrush;
                    currentCallSign.Content = "N";
                    break;
                case "OSCAR":
                    brush = this.blueBrush;
                    currentCallSign.Content = "O";
                    break;
                case "PAPA":
                    brush = this.blueBrush;
                    currentCallSign.Content = "P";
                    break;
                case "QUEBEC":
                    brush = this.blueBrush;
                    currentCallSign.Content = "Q";
                    break;
                case "ROMEO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "R";
                    break;
                case "SIERRA":
                    brush = this.blueBrush;
                    currentCallSign.Content = "S";
                    break;
                case "TANGO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "T";
                    break;
                case "UNIFORM":
                    brush = this.blueBrush;
                    currentCallSign.Content = "U";
                    break;
                case "VICTOR":
                    brush = this.blueBrush;
                    currentCallSign.Content = "V";
                    break;
                case "WHISKEY":
                    brush = this.blueBrush;
                    currentCallSign.Content = "W";
                    break;
                case "X RAY":
                    brush = this.blueBrush;
                    currentCallSign.Content = "X";
                    break;
                case "YANKEE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "Y";
                    break;
                case "ZULU":
                    brush = this.blueBrush;
                    currentCallSign.Content = "Z";
                    break;
                case "ZERO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "0";
                    break;
                case "ONE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "1";
                    break;
                case "TWO":
                    brush = this.blueBrush;
                    currentCallSign.Content = "2";
                    break;
                case "THREE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "3";
                    break;
                case "FOUR":
                    brush = this.blueBrush;
                    currentCallSign.Content = "4";
                    break;
                case "FIVE":
                    brush = this.blueBrush;
                    currentCallSign.Content = "5";
                    break;
                case "SIX":
                    brush = this.blueBrush;
                    currentCallSign.Content = "6";
                    break;
                case "SEVEN":
                    brush = this.blueBrush;
                    currentCallSign.Content = "7";
                    break;
                case "EIGHT":
                    brush = this.blueBrush;
                    currentCallSign.Content = "8";
                    break;
                case "NINER":
                    brush = this.blueBrush;
                    currentCallSign.Content = "9";
                    break;
                case "OVER":
                    brush = this.blueBrush;
                    transmissionDone = true;
                    break;
                case "DELETE":
                    brush = this.blueBrush;
                    //transmissionDone = false;
                    mistake = true;
                    //transmission.Remove(transmission.Length - 1);
                    //currentCallSign.Content = transmission;
                    break;
                case "CAMERA ON":
                    //this.kinectColorViewer1.Visibility = System.Windows.Visibility.Visible;
                    brush = this.blackBrush;
                    break;
                case "CAMERA OFF":
                    //this.kinectColorViewer1.Visibility = System.Windows.Visibility.Hidden;
                    brush = this.blackBrush;
                    break;
                case "LAND":
                         transmissionDone = true;
                        checkForLanding(runway1);
                        checkForLanding(runway2);
                        checkForLanding(runway3);
                    break;
                default:
                    brush = this.blackBrush;
                    break;
            }

            string status = "Recognized: " + e.Result.Text + " " + e.Result.Confidence;
            this.ReportSpeechStatus(status);
            //hangarFull(2);
            transmissionEnd();

            // Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = brush; }), DispatcherPriority.Normal);
        }//end SreSpeechRecognized

        private void transmissionEnd()
        {
            //we haven't said 'OVER' yet, and we haven't done a mistake, so we just append the letter to our string
            if (!transmissionDone && !mistake)
            {
                //tailNum.Content = tempAirplane.getTailNum();
                transmission += currentCallSign.Content.ToString();
                currentCallSign.Content = transmission;
            }
            else if (!transmissionDone && mistake && transmission.Length > 0)
            {
                transmission = mistakeCorrection((string)currentCallSign.Content);
                currentCallSign.Content = transmission;
                mistake = false;
            }
            else
            {
                mistake = false;
                //checkPlayerAccuracy(transmission); // check to see if you were right
                checkAllPlanes();
                lastUsed.Content = transmission; //set the current string you were building to the 'last used' label
                transmission = ""; // null out the string you were building
                currentCallSign.Content = ""; // null out the label of the string you were building
                //randomTailnum(); //generate a new random tailnumber
                transmissionDone = false;
                //hangarFull();

            }
        }//end transmissionEnd

        private string mistakeCorrection(string errorString)
        {
            string correctedstring = errorString.Remove(errorString.Length - 1);
            return correctedstring;
        }//end mistakeCorrection

        private class EnergyCalculatingPassThroughStream : Stream
        {
            private const int SamplesPerPixel = 10;

            private readonly double[] energy = new double[WaveImageWidth];
            private readonly object syncRoot = new object();
            private readonly Stream baseStream;

            private int index;
            private int sampleCount;
            private double avgSample;

            public EnergyCalculatingPassThroughStream(Stream stream)
            {
                this.baseStream = stream;
            }

            public override long Length
            {
                get { return this.baseStream.Length; }
            }

            public override long Position
            {
                get { return this.baseStream.Position; }
                set { this.baseStream.Position = value; }
            }

            public override bool CanRead
            {
                get { return this.baseStream.CanRead; }
            }

            public override bool CanSeek
            {
                get { return this.baseStream.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return this.baseStream.CanWrite; }
            }

            public override void Flush()
            {
                this.baseStream.Flush();
            }



            public void GetEnergy(double[] energyBuffer)
            {
                lock (this.syncRoot)
                {
                    int energyIndex = this.index;
                    for (int i = 0; i < this.energy.Length; i++)
                    {
                        energyBuffer[i] = this.energy[energyIndex];
                        energyIndex++;
                        if (energyIndex >= this.energy.Length)
                        {
                            energyIndex = 0;
                        }
                    }
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int retVal = this.baseStream.Read(buffer, offset, count);
                const double A = 0.3;
                lock (this.syncRoot)
                {
                    for (int i = 0; i < retVal; i += 2)
                    {
                        short sample = BitConverter.ToInt16(buffer, i + offset);
                        this.avgSample += sample * sample;
                        this.sampleCount++;

                        if (this.sampleCount == SamplesPerPixel)
                        {
                            this.avgSample /= SamplesPerPixel;

                            this.energy[this.index] = .2 + ((this.avgSample * 11) / (int.MaxValue / 2));
                            this.energy[this.index] = this.energy[this.index] > 10 ? 10 : this.energy[this.index];

                            if (this.index > 0)
                            {
                                this.energy[this.index] = (this.energy[this.index] * A) + ((1 - A) * this.energy[this.index - 1]);
                            }

                            this.index++;
                            if (this.index >= this.energy.Length)
                            {
                                this.index = 0;
                            }

                            this.avgSample = 0;
                            this.sampleCount = 0;
                        }
                    }
                }

                return retVal;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return this.baseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                this.baseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.baseStream.Write(buffer, offset, count);
            }




        }//end EnergyCalculatingPassThroughStream

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            CheckBox enableAecCheckBox = (CheckBox)sender;
            if (enableAecCheckBox.IsChecked != null)
            {
                this.kinect.AudioSource.EchoCancellationMode = enableAecCheckBox.IsChecked.Value
                                                             ? EchoCancellationMode.CancellationAndSuppression
                                                             : EchoCancellationMode.None;
            }
        }//end EnableAecChecked

        private void ReportSpeechStatus(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbSpeechStatus.Text = status; }), DispatcherPriority.Normal);
        }//end ReportSpeechStatus

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }



    }//end MainWindow:Window
}//end KinectAudioDemo