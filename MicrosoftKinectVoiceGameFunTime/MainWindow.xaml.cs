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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
        private double angle;
        private bool running = true;
        private DispatcherTimer readyTimer;
        private EnergyCalculatingPassThroughStream stream;
        private SpeechRecognitionEngine speechRecognizer;

        //my happy things
        private string transmission;
        private bool transmissionDone = false;
        private Random rdm = new Random();
        private Stack<airplane> hangar = new Stack<airplane>();
        private airplane tempAirplane;

        public MainWindow()
        {
            InitializeComponent();

            var colorList = new List<Color> { Colors.Black, Colors.Green };
            this.bitmapWave = new WriteableBitmap(WaveImageWidth, WaveImageHeight, 96, 96, PixelFormats.Indexed1, new BitmapPalette(colorList));

            this.pixels = new byte[WaveImageWidth];
            for (int i = 0; i < this.pixels.Length; i++)
            {
                this.pixels[i] = 0xff;
            }

            imgWav.Source = this.bitmapWave;

            SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;
        }

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

        private void SensorChooserKinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = e.OldValue as KinectSensor;
            if (oldSensor != null)
            {
                this.UninitializeKinect();
            }

            KinectSensor newSensor = e.NewValue as KinectSensor;
            this.kinect = newSensor;

            // Only enable this checkbox if we have a sensor
            enableAec.IsEnabled = this.kinect != null;

            if (newSensor != null)
            {
                this.InitializeKinect();
            }
        }

        private void InitializeKinect()
        {
            var sensor = this.kinect;
            this.speechRecognizer = this.CreateSpeechRecognizer();
            try
            {
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                sensor.Start();
            }
            catch (Exception)
            {
                SensorChooser.AppConflictOccurred();
                return;
            }
            if (this.speechRecognizer != null && sensor != null)
            {
                // NOTE: Need to wait 4 seconds for device to be ready to stream audio right after initialization
                this.readyTimer = new DispatcherTimer();
                this.readyTimer.Tick += this.ReadyTimerTick;
                this.readyTimer.Interval = new TimeSpan(0, 0, 4);
                this.readyTimer.Start();

                this.ReportSpeechStatus("Initializing audio stream...");
                this.UpdateInstructionsText(string.Empty);

                this.Closing += this.MainWindowClosing;
            }
            initializeGame();
            this.running = true;
        }

        private void ReadyTimerTick(object sender, EventArgs e)
        {
            this.Start();
            this.ReportSpeechStatus("Ready to recognize speech!");
            this.UpdateInstructionsText("Say: 'red', 'green' or 'blue'");
            this.readyTimer.Stop();
            this.readyTimer = null;
        }

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
        }

        private void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UninitializeKinect();
        }

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
           // grammar.Add("elevation");
            grammar.Add("Camera on");
            grammar.Add("Camera off");

            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Append(grammar);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);

            sre.LoadGrammar(g);
            sre.SpeechRecognized += this.SreSpeechRecognized;
            sre.SpeechHypothesized += this.SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += this.SreSpeechRecognitionRejected;

            return sre;
        }

        private void RejectSpeech(RecognitionResult result)
        {
            string status = "Rejected: " + (result == null ? string.Empty : result.Text + " " + result.Confidence);
            this.ReportSpeechStatus(status);

            Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = blackBrush; }), DispatcherPriority.Normal);
        }

        private void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            this.RejectSpeech(e.Result);
        }

        private void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            this.ReportSpeechStatus("Hypothesized: " + e.Result.Text + " " + e.Result.Confidence);
        }

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
                    label1.Content = "A";
                    break;
                case "BRAVO":
                    brush = this.greenBrush;
                    label1.Content = "B";
                    break;
                case "CHARLIE":
                    brush = this.blueBrush;
                    label1.Content = "C";
                    break;
                case "DELTA":
                    brush = this.blueBrush;
                    label1.Content = "D";
                    break;
                case "ECHO":
                    brush = this.blueBrush;
                    label1.Content = "E";
                    break;
                case "FOXTROT":
                    brush = this.blueBrush;
                    label1.Content = "F";
                    break;
                case "GOLF":
                    brush = this.blueBrush;
                    label1.Content = "G";
                    break;
                case "HOTEL":
                    brush = this.blueBrush;
                    label1.Content = "H";
                    break;
                case "INDIA":
                    brush = this.blueBrush;
                    label1.Content = "I";
                    break;
                case "JULIET":
                    brush = this.blueBrush;
                    label1.Content = "J";
                    break;
                case "KILO":
                    brush = this.blueBrush;
                    label1.Content = "K";
                    break;
                case "LIMA":
                    brush = this.blueBrush;
                    label1.Content = "L";
                    break;
                case "MIKE":
                    brush = this.blueBrush;
                    label1.Content = "M";
                    break;
                case "NOVEMBER":
                    brush = this.blueBrush;
                    label1.Content = "N";
                    break;
                case "OSCAR":
                    brush = this.blueBrush;
                    label1.Content = "O";
                    break;
                case "PAPA":
                    brush = this.blueBrush;
                    label1.Content = "P";
                    break;
                case "QUEBEC":
                    brush = this.blueBrush;
                    label1.Content = "Q";
                    break;
                case "ROMEO":
                    brush = this.blueBrush;
                    label1.Content = "R";
                    break;
                case "SIERRA":
                    brush = this.blueBrush;
                    label1.Content = "S";
                    break;
                case "TANGO":
                    brush = this.blueBrush;
                    label1.Content = "T";
                    break;
                case "UNIFORM":
                    brush = this.blueBrush;
                    label1.Content = "U";
                    break;
                case "VICTOR":
                    brush = this.blueBrush;
                    label1.Content = "V";
                    break;
                case "WHISKEY":
                    brush = this.blueBrush;
                    label1.Content = "W";
                    break;
                case "X RAY":
                    brush = this.blueBrush;
                    label1.Content = "X";
                    break;
                case "YANKEE":
                    brush = this.blueBrush;
                    label1.Content = "Y";
                    break;
                case "ZULU":
                    brush = this.blueBrush;
                    label1.Content = "Z";
                    break;
                case "ZERO":
                    brush = this.blueBrush;
                    label1.Content = "0";
                    break;
                case "ONE":
                    brush = this.blueBrush;
                    label1.Content = "1";
                    break;
                case "TWO":
                    brush = this.blueBrush;
                    label1.Content = "2";
                    break;
                case "THREE":
                    brush = this.blueBrush;
                    label1.Content = "3";
                    break;
                case "FOUR":
                    brush = this.blueBrush;
                    label1.Content = "4";
                    break;
                case "FIVE":
                    brush = this.blueBrush;
                    label1.Content = "5";
                    break;
                case "SIX":
                    brush = this.blueBrush;
                    label1.Content = "6";
                    break;
                case "SEVEN":
                    brush = this.blueBrush;
                    label1.Content = "7";
                    break;
                case "EIGHT":
                    brush = this.blueBrush;
                    label1.Content = "8";
                    break;
                case "NINER":
                    brush = this.blueBrush;
                    label1.Content = "9";
                    break;
                case "OVER":
                    brush = this.blueBrush;
                    transmissionDone = true;
                    break;
                case "CAMERA ON":
                    this.kinectColorViewer1.Visibility = System.Windows.Visibility.Visible;
                    brush = this.blackBrush;
                    break;
                case "CAMERA OFF":
                    this.kinectColorViewer1.Visibility = System.Windows.Visibility.Hidden;
                    brush = this.blackBrush;
                    break;
                default:
                    brush = this.blackBrush;
                    break;
            }

            string status = "Recognized: " + e.Result.Text + " " + e.Result.Confidence;
            this.ReportSpeechStatus(status);
            hangarFull();
            transmissionEnd();
            
           // Dispatcher.BeginInvoke(new Action(() => { tbColor.Background = brush; }), DispatcherPriority.Normal);
        }
        private void transmissionEnd()
        {
            if (transmissionDone == false)
            {
                tailNum.Content = tempAirplane.getTailNum();
                transmission += label1.Content.ToString();
                label1.Content = transmission;
            }
            else
            {
                checkPlayerAccuracy(transmission); // check to see if you were right
                lastUsed.Content = transmission; //set the current string you were building to the 'last used' label
                transmission = ""; // null out the string you were building
                label1.Content = ""; // null out the label of the string you were building
                //randomTailnum(); //generate a new random tailnumber
                transmissionDone = false;
                //hangarFull();
                
            }
        }

        private void randomTailnum()
        {
            string tailnum = "N";
            int remianingChars = rdm.Next(5);
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
                tailNum.Content = tailnum;
            }
        }

        private void hangarFull()
        {
            if (hangar.Count == 0)//come back and fix to ensure that it's truly empty
            {
                //let's add some airplanes!
                while(hangar.Count < 5){
                    //airplane plane = new airplane();
                    hangar.Push(new airplane());
                }
            }
            else{
                return;
                }
        }
        private void checkPlayerAccuracy(string testTailNumber){
            if (testTailNumber == null)
            {
                return;
            }
            else if(tailNum.Content.Equals(testTailNumber)){
                lastUsed.Background = Brushes.Green;
                tempAirplane = hangar.Pop();
                testlabel.Content = hangar.Count;
                tailNum.Content = tempAirplane.getTailNum();
                
                
            }
            else{
                label3.Content = testTailNumber;
                label4.Content = tailNum.Content;
                lastUsed.Background = Brushes.Red;
            }
        }


        private void ReportSpeechStatus(string status)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbSpeechStatus.Text = status; }), DispatcherPriority.Normal);
        }

        private void UpdateInstructionsText(string instructions)
        {
            Dispatcher.BeginInvoke(new Action(() => { tbColor.Text = instructions; }), DispatcherPriority.Normal);
        }

        private void Start()
        {
            var audioSource = this.kinect.AudioSource;
            audioSource.BeamAngleMode = BeamAngleMode.Adaptive;
            var kinectStream = audioSource.Start();
            this.stream = new EnergyCalculatingPassThroughStream(kinectStream);
            this.speechRecognizer.SetInputToAudioStream(
                this.stream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            this.speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
            
        }

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            CheckBox enableAecCheckBox = (CheckBox)sender;
            if (enableAecCheckBox.IsChecked != null)
            {
                this.kinect.AudioSource.EchoCancellationMode = enableAecCheckBox.IsChecked.Value
                                                             ? EchoCancellationMode.CancellationAndSuppression
                                                             : EchoCancellationMode.None;
            }
        }
        public class airplane{
            private string tailNumber;
            private float timer;
            private int size;
            private Random rdm = new Random();

            public airplane(){
                tailNumber = randomTailnum();
                timer = 25.00f;
                size = 1;
            }
            public airplane(string tailNumberGiven)
            {
                tailNumber = tailNumberGiven;
                timer = 25.00f;
                size = 1;
            }
            public string getTailNum()
            {
                return tailNumber;
            }

            private string randomTailnum()
            {
                string tailnum = "N";
                int remianingChars = rdm.Next(1);//FIX WHEN DONE
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
                    //tailNum.Content = tailnum;
                }
                return tailnum;
            }
        }

        public void initializeGame(){
            hangarFull();
            tempAirplane = hangar.Pop();
            tailNum.Content = tempAirplane.getTailNum();
        }
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
        }
    }
}
