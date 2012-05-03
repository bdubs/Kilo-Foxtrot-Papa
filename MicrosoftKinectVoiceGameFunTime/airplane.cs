using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace MicrosoftKinectVoiceGameFunTime
{
    public class airplane
    {
        private string tailNumber;
        public int timer;
        private int size;
        public ProgressBar fuel;
        private int baseFuelTime = 1800;
        
        public airplane(string tailNumberGiven)
        {
            tailNumber = tailNumberGiven;
            timer = baseFuelTime;
            size = 1;
        }
        public string getTailNum()
        {
            return tailNumber;
        }
        public void updateFuel(){
           // fuel.Value = (timer / baseFuelTime) * 100;
            fuel.Value = timer;
        }

        public void consumeFuel()
        {
            timer--;
            updateFuel();
        }

    }//end class airplane
}//end namespace
