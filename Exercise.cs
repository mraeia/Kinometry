using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace Kinometry
{
    abstract class Exercise
    {
        public abstract List<int> Test(Body body);
        public abstract int GetInputsCount();
        public abstract int[] GetNeuronsCount();
        public abstract string GetExerciseName();
    }
}
