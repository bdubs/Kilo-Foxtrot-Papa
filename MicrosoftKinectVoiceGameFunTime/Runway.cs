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

namespace MicrosoftKinectVoiceGameFunTime
{
    public class Runway
    {
        public double x;
        public double y;
        public double width;
        public double height;
        public bool occupied;
        public int ID;
        public System.Windows.Shapes.Ellipse redSign;
        public int mTimer;

        public Runway(double mx, double my, double mwidth, double mheight, int id, System.Windows.Shapes.Ellipse ellipse)
        {
            mTimer = 0;
            ID = id;
            x = mx;
            y = my;
            width = mwidth;
            height = mheight;
            occupied = false;
            redSign = ellipse;

        }

        public void checkTime()
        {
            if (mTimer > 0)
            {
                mTimer--;
            }
            else
            {
                redSign.Visibility = Visibility.Hidden;
                occupied = false;
            }
        }
    }
}
