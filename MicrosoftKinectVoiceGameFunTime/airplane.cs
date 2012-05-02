using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicrosoftKinectVoiceGameFunTime
{
    public class airplane
    {
        private string tailNumber;
        private float timer;
        private int size;

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
    }
}
