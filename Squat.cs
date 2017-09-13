using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Shapes;
using Microsoft.Kinect;


namespace Kinometry
{
    class Squat : Exercise
    {
        public static CameraSpacePoint base_spine;
        public static Joint SpineBase;
 
        public override int GetInputsCount() { return 1; }    // number of features
        public override int[] GetNeuronsCount() { return new int[] { 5, 1 }; }   //rest of the layers
        public override string GetExerciseName() { return "Squat"; }

        int GetPostureAngle(double[] input)
        {
            double[] vector1 = { input[27] - input[18], input[28] - input[19], input[29] - input[20] };
            double[] vector2 = { 0, input[28] - input[19], 0 };
            double angle = Math.Acos((Math.Pow(input[28] - input[19], 2)) / (Math.Sqrt(Math.Pow(input[27] - input[18], 2) + Math.Pow(input[28] - input[19], 2) + Math.Pow(input[29] - input[20], 2)) * (input[28] - input[19])));
            int in_degrees = (int)(angle * 180 / Math.PI);
            return in_degrees;
        }

        int GetFemurTibiaAngle(double[] input)
        {
            Vector V1 = new Vector(input[17], input[16]);
            Vector V2 = new Vector(input[5],input[4]);
            Vector V3 = new Vector(input[11],input[10]);
            //knee ankle vector
            Vector V4 = Vector.Subtract(V3, V2);
            //knee hip vector
            Vector V5 = Vector.Subtract(V1, V2);
            return (int)Vector.AngleBetween(V4,V5);
        }

        private void AddFeature(double[] input, int index, CameraSpacePoint pos)
        {
            input[index] = pos.X;
            input[index + 1] = pos.Y;
            input[index + 2] = pos.Z < 0 ? 0 : pos.Z;
            if (index == 18)
            {
                base_spine = pos;
            }
        }


        public override List<int> Test(Body body)
        {
            List<int> squat_angles = new List<int>();
            SpineBase = body.Joints[JointType.SpineBase];
            double[] input = new double[31];
            foreach (JointType type in body.Joints.Keys)
            {
                int vectorPos = -1;
                // Draw all the body joints
                switch (type)
                {
                    case JointType.KneeLeft:
                        vectorPos = 0;
                        break;
                    case JointType.KneeRight:
                        vectorPos = 3;
                        break;
                    case JointType.AnkleLeft:
                        vectorPos = 6;
                        break;
                    case JointType.AnkleRight:
                        vectorPos = 9;
                        break;
                    case JointType.HipLeft:
                        vectorPos = 12;
                        break;
                    case JointType.HipRight:
                        vectorPos = 15;
                        break;
                    case JointType.SpineBase:
                        vectorPos = 18;
                        break;
                    case JointType.SpineMid:
                        vectorPos = 21;
                        break;
                    case JointType.SpineShoulder:
                        vectorPos = 27;
                        break;
                }
                if (vectorPos >= 0)
                {
                    AddFeature(input, vectorPos, body.Joints[type].Position);
                }
            }

            int angle = GetPostureAngle(input);
            int angle1 = GetFemurTibiaAngle(input);
            squat_angles.Add(angle);
            squat_angles.Add(angle1);
            return squat_angles;
        }
    }
}
