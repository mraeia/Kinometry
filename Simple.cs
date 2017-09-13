using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Kinometry
{
    class Simple : Exercise
    {
        public override int GetInputsCount() { return 2; }
        public override int[] GetNeuronsCount() { return new int[] {2,1}; }
        public override string GetExerciseName() { return "Simple"; }

        private void AddFeature(double[] input, int index, CameraSpacePoint pos)
        {
            input[index] = pos.X;
            input[index + 1] = pos.Y;
            //input[index + 2] = pos.Z < 0 ? 0.1 : pos.Z;
            Console.Write("X" + input[index] + " Y" + input[index + 1]+"\n");
        }
        public override List<int> Test(Body body)
        {
            List<int> hello = new List<int>();
            double[] input = new double[2];
            foreach (JointType type in body.Joints.Keys)
            {
                if (type.Equals(JointType.HandRight))// (vectorPos >= 0)
                {
                    AddFeature(input, 0, body.Joints[type].Position);
                    break;
                }
            }
            return hello;
        }
    }
}
