using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Media;
using Microsoft.Kinect;

using AForge;
using AForge.Neuro;
using AForge.Neuro.Learning;

namespace Kinometry
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Exercise _exerciseType = null;
        string _exerciseString = "";

        KinectSensor _kinect = null;
 
        /// <summary>
        /// All tracked bodies
        /// </summary>
        private Body[] _bodies = null;

        /// <summary>
        /// FrameReader for the bodies
        /// </summary>
        private BodyFrameReader _bodyReader = null;

        /// <summary>
        /// Size fo the RGB pixel in bitmap
        /// </summary>
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// FrameReader for our coloroutput
        /// </summary>
        private ColorFrameReader _colorReader = null;

        /// <summary>
        /// Array of color pixels
        /// </summary>
        private byte[] _colorPixels = null;

        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _colorBitmap = null;

        /// <summary>
        /// Description of the data contained in the body index frame
        /// </summary>
        private FrameDescription frameDesc = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private uint[] _bodyIndexPixels = null;

        private int _mainBodyIndex = -1;

        private string _statusText = "";

        private bool _isTesting = false;
        private bool _isSelected = false;

        private int reps_counter = 0;
        private int reps_binary = 0;
        private bool first_frame = false;


        public MainWindow()
        {
            InitializeComponent();
            InitializeKinect();
            Closing += OnClosing;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kinect != null) _kinect.Close();

            if (_bodyReader != null)
            {
                // BodyFrameReader is IDisposable
                _bodyReader.Dispose();
                _bodyReader = null;
            }

            if (_kinect != null)
            {
                _kinect.Close();
                _kinect = null;
            }
            
        }

        private void InitializeKinect()
        {
            _kinect = KinectSensor.GetDefault();
            if (_kinect == null) return;
            
            // set IsAvailableChanged event notifier
            _kinect.IsAvailableChanged += this.OnSensorAvailableChanged;

            _kinect.Open();
            
            // set the status text
            _statusText = _kinect.IsAvailable ? Properties.Resources.RunningStatusText
                                               : Properties.Resources.NoSensorStatusText;

            // Initialize Body
            InitializeBody();
            //Initialize Camera
            InitializeCamera();
        }

        private void InitializeBody()
        {
            if (_kinect == null) return;

            // Allocate Bodies array
            _bodies = new Body[_kinect.BodyFrameSource.BodyCount];

            // Open reader
            _bodyReader = _kinect.BodyFrameSource.OpenReader();

            // Hook up event
            _bodyReader.FrameArrived += OnBodyFrameArrived;
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void OnSensorAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            _statusText = _kinect.IsAvailable ? Properties.Resources.RunningStatusText
                                                : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // Get frame reference
            BodyFrameReference refer = e.FrameReference;

            if (refer == null) return;

            
            // Get body frame
            using (BodyFrame frame = refer.AcquireFrame())
            {
                if (frame == null) return;

                Body body=null;
                
                if (first_frame == false || _mainBodyIndex == -1)
                    body = FindClosestBody(frame);
                else
                {
                    body = TrackedBody(frame);
                }
                if (body == null)
                {
                    return;
                }
                drawing_canvas.Children.Clear();
                // Loop all bodies
                if (body.IsTracked && _isTesting && _isSelected)
                {
                    List<int> angles =_exerciseType.Test(body);
                    if (angles[0] > 40)
                    {
                        System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"C:\Users\Raeia\Desktop\Hatchery\IncorrectPosture.wav");
                        player.Play();
                        //h3llo
                        drawellipse(Squat.SpineBase);
                        _result.Content = "Incorrect Posture!";
                    }
                    if (Math.Abs(angles[1]) < 100 && reps_binary == 0)
                    {
                        reps_counter++;
                        reps_binary = 1;
                    }
                    else if (Math.Abs(angles[1]) >100 && reps_binary == 1)
                    {
                        reps_binary = 0;
                    }
                    _number_of_reps.Content = reps_counter.ToString();
                }
            }
        }


        private void OnExerciseSelected(object sender, SelectionChangedEventArgs e)
        {
            _exerciseString = (string)(((ComboBoxItem)_exerciseCombobox.SelectedItem).Content);
            switch (_exerciseString)
            {
                case ("Simple"):
                    _exerciseType = new Simple();
                    _isSelected = true;
                    break;
                case ("Squat"):
                    _exerciseType = new Squat();
                    _isSelected = true;
                    break;
                default:
                    throw new Exception("Invalid exercise");
            }
        }

        private void OnTestClick(object sender, RoutedEventArgs e)
        {
            if (_isTesting == false)
            {
                _isTesting = true;
                _test.Content="Testing!";
            }
            else {
                _isTesting = false;
                _test.Content = "Test!";
            }
        }

        private Body FindClosestBody(BodyFrame bodyFrame)
        {
            Body result = null;
            double closestBodyDistance = double.MaxValue;
            int i = 0;

            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    double distance_x = Math.Abs(body.Joints[JointType.SpineBase].Position.X);

                    if (result == null || distance_x < closestBodyDistance)
                    {
                        result = body;
                        closestBodyDistance = distance_x;
                        _mainBodyIndex = i;
                    }
                }
                first_frame = true;
                i++;
            }

            return result;
        }

        private Body TrackedBody(BodyFrame bodyFrame)
        {
            Body body = null;
            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);
            body = bodies[_mainBodyIndex];
            return body;
        }

        private void InitializeCamera()
        {
            if (_kinect == null) return;

            if (_kinect == null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            // Get the framereader for Color
            _colorReader = _kinect.ColorFrameSource.OpenReader();

            // Allocate pixel array
            _colorPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _colorBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
            CameraImage.Source = _colorBitmap;

            // Hook-up event
            _colorReader.FrameArrived += OnColorFrameArrived;
        }


        private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            // Acquire frame for specific reference
            ColorFrame frame = colorRef.AcquireFrame();

            // It's possible that we skipped a frame or it is already gone
            if (frame == null) return;

            using (frame)
            {
                // Get frame description
                frameDesc = frame.FrameDescription;

                // Check if width/height matches
                if (frameDesc.Width == _colorBitmap.PixelWidth && frameDesc.Height == _colorBitmap.PixelHeight)
                {
                    // Copy data to array based on image format
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(_colorPixels);
                    }
                    else frame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);

                    // Copy output to bitmap
                    _colorBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _colorPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }
        private void drawellipse(Joint joint)
        {
            if (joint.TrackingState == TrackingState.NotTracked)
                return;
            double widthRatio = CameraImage.ActualWidth / frameDesc.Width;
            double heightRatio = CameraImage.ActualHeight / frameDesc.Height;
            double ratio = Math.Max(widthRatio, heightRatio);
            //joint.Position.X = _kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position).X;
            //joint.Position.Y = _kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position).Y;
            //joint = ScaleTo(joint,drawing_canvas.ActualWidth,drawing_canvas.ActualHeight);
            ColorSpacePoint pt = _kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = Brushes.Red
            };
            // Position the ellipse according to the point's coordinates.
            
            //Console.WriteLine(point.X + " " + point.Y);
            //Console.WriteLine(joint.Position.X + " " + joint.Position.Y);
            Canvas.SetLeft(ellipse, pt.X * ratio - ellipse.Width / 2);
            Canvas.SetTop(ellipse, pt.Y * ratio - ellipse.Width / 2);

            // Add the ellipse to the canvas.
            drawing_canvas.Children.Add(ellipse);
        }

        private Joint ScaleTo(Joint joint, double width,double height)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, 1.0f, joint.Position.X),
                Y = Scale(height, 1.0f, -joint.Position.Y),
                Z = joint.Position.Z
            };
            return joint;
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }
    }
}
