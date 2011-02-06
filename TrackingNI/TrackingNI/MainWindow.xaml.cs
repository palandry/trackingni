using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using xn;
using xnv;
using System.ComponentModel;

namespace TrackingNI
{
    public partial class MainWindow : Window
    {
        private readonly string CONFIG_FILE = @"User.xml";
        private readonly int DPI_X = 96;
        private readonly int DPI_Y = 96;

        private Console console;

        private Context context;
        private ImageGenerator imageGenerator;
        private DepthGenerator depthGenerator;
        private UserGenerator userGenerator;

        private WriteableBitmap imageBitmap;
        private WriteableBitmap depthBitmap;
        private ImageMetaData imageData;
        private DepthMetaData depthData;

        private PoseDetectionCapability poseDetectionCapability;
        private SkeletonCapability skeletonCapability;

        private SkeletonDraw skeletonDraw;

        private int[] Histogram { get; set; }

        private Thread reader;
        private BackgroundWorker worker;
        private bool stop;

        public ImageSource RawImageSource
        {
            get
            {
                if (imageBitmap != null)
                {
                    imageBitmap.Lock();
                    imageBitmap.WritePixels(new Int32Rect(0, 0, imageData.XRes, imageData.YRes), imageData.ImageMapPtr, (int)imageData.DataSize, imageBitmap.BackBufferStride);
                    imageBitmap.Unlock();
                }

                return imageBitmap;
            }
        }

        public ImageSource DepthImageSource
        {
            get
            {
                if (depthBitmap != null)
                {
                    UpdateHistogram(depthData);

                    depthBitmap.Lock();

                    unsafe
                    {
                        ushort* pDepth = (ushort*)depthGenerator.GetDepthMapPtr().ToPointer();
                        for (int y = 0; y < depthData.YRes; ++y)
                        {
                            byte* pDest = (byte*)depthBitmap.BackBuffer.ToPointer() + y * depthBitmap.BackBufferStride;
                            for (int x = 0; x < depthData.XRes; ++x, ++pDepth, pDest += 3)
                            {
                                byte pixel = (byte)Histogram[*pDepth];
                                pDest[0] = pixel;
                                pDest[1] = pixel;
                                pDest[2] = pixel;
                            }
                        }
                    }

                    depthBitmap.AddDirtyRect(new Int32Rect(0, 0, depthData.XRes, depthData.YRes));
                    depthBitmap.Unlock();
                }

                skeletonDraw.DrawStickFigure(ref depthBitmap, depthGenerator, depthData, userGenerator);

                return depthBitmap;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            console = new Console();
            console.Show();
            console.Top = 0;
            console.Left = 0;

            Console.Write("TrackingNI by Richard Pianka");

            context = new Context(CONFIG_FILE);
            imageGenerator = new ImageGenerator(context);
            depthGenerator = new DepthGenerator(context);
            userGenerator = new UserGenerator(context);

            poseDetectionCapability = userGenerator.GetPoseDetectionCap();
            skeletonCapability = userGenerator.GetSkeletonCap();

            MapOutputMode mapMode = depthGenerator.GetMapOutputMode();

            int width = (int)mapMode.nXRes;
            int height = (int)mapMode.nYRes;

            imageBitmap = new WriteableBitmap(width, height, DPI_X, DPI_Y, PixelFormats.Rgb24, null);
            depthBitmap = new WriteableBitmap(width, height, DPI_X, DPI_Y, PixelFormats.Rgb24, null);
            imageData = new ImageMetaData();
            depthData = new DepthMetaData();

            skeletonDraw = new SkeletonDraw();

            Histogram = new int[depthGenerator.GetDeviceMaxDepth()];

            reader = new Thread(new ThreadStart(Reader));
            reader.IsBackground = true;
            worker = new BackgroundWorker();
            stop = false;

            CompositionTarget.Rendering += new EventHandler(Worker);
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            userGenerator.NewUser += new xn.UserGenerator.NewUserHandler(NewUser);
            userGenerator.LostUser += new xn.UserGenerator.LostUserHandler(LostUser);

            userGenerator.GetSkeletonCap().CalibrationStart += new SkeletonCapability.CalibrationStartHandler(CalibrationStart);
            userGenerator.GetSkeletonCap().CalibrationEnd += new SkeletonCapability.CalibrationEndHandler(CalibrationEnd);
            userGenerator.GetSkeletonCap().SetSkeletonProfile(SkeletonProfile.All);
            userGenerator.GetPoseDetectionCap().PoseDetected += new PoseDetectionCapability.PoseDetectedHandler(PoseDetected);
            userGenerator.GetPoseDetectionCap().PoseEnded += new PoseDetectionCapability.PoseEndedHandler(PoseEnded);

            reader.Start();
            worker.DoWork += new DoWorkEventHandler(WorkerTick);
        }

        private void NewUser(ProductionNode node, uint id)
        {
            userGenerator.GetPoseDetectionCap().StartPoseDetection(userGenerator.GetSkeletonCap().GetCalibrationPose(), id);
            Console.Write(id + " Found new user");
        }

        private void LostUser(ProductionNode node, uint id)
        {
            Console.Write(id + " Lost user");
        }

        private void CalibrationStart(ProductionNode node, uint id)
        {
            Console.Write(id + " Calibration start");
        }

        private void CalibrationEnd(ProductionNode node, uint id, bool success)
        {
            Console.Write(id + " Calibration ended " + (success ? "successfully" : "unsuccessfully"));
            if (success)
            {
                userGenerator.GetSkeletonCap().StartTracking(id);
                //this.joints.Add(id, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
            }
            else
            {
                userGenerator.GetPoseDetectionCap().StartPoseDetection(userGenerator.GetSkeletonCap().GetCalibrationPose(), id);
            }
        }

        private void PoseDetected(ProductionNode node, string pose, uint id)
        {
            Console.Write(id + " Detected pose " + pose);
            userGenerator.GetPoseDetectionCap().StopPoseDetection(id);
            userGenerator.GetSkeletonCap().RequestCalibration(id, false);
        }

        private void PoseEnded(ProductionNode node, string pose, uint id)
        {
            Console.Write(id + " Lost Pose " + pose);
        }

        private void Reader()
        {
            //context.StartGeneratingAll();

            while (!stop)
            {
                try
                {
                    unsafe
                    {
                        context.WaitAndUpdateAll();
                    }
                    imageGenerator.GetMetaData(imageData);
                    depthGenerator.GetMetaData(depthData);
                }
                catch (Exception)
                {

                }
            }
        }

        private void WorkerTick(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                imgRaw.Source = RawImageSource;
                imgDepth.Source = DepthImageSource;
            });
        }

        private void Worker(object sender, EventArgs e)
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stop = true;
        }

        public unsafe void UpdateHistogram(DepthMetaData depthMD)
        {
            // Reset.
            for (int i = 0; i < Histogram.Length; ++i)
                Histogram[i] = 0;

            ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

            int points = 0;
            for (int y = 0; y < depthMD.YRes; ++y)
            {
                for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
                {
                    ushort depthVal = *pDepth;
                    if (depthVal != 0)
                    {
                        Histogram[depthVal]++;
                        points++;
                    }
                }
            }

            for (int i = 1; i < Histogram.Length; i++)
            {
                Histogram[i] += Histogram[i - 1];
            }

            if (points > 0)
            {
                for (int i = 1; i < Histogram.Length; i++)
                {
                    Histogram[i] = (int)(256 * (1.0f - (Histogram[i] / (float)points)));
                }
            }
        }
    }
}
