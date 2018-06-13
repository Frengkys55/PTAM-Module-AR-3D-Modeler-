using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IPC;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Drawing;

namespace PTAM_Module__AR_3D_Modeler_
{
    class Program
    {
        #region Application informations
        static Configuration savedInformations = new Configuration();

        #region Application start-up mode
        static bool isStartHidden = false;
        #endregion Application start-up mode

        #region Inter-process Communication objects

        #region Named pipe objects
        static string HUBNotifierChannel        = string.Empty;
        static string modelerNotifierChannel    = string.Empty;

        static NamedPipesServer HUBNotifier;
        static NamedPipeClient modelerNotifier;
        #endregion Named pipe objects

        #region Memory mapped file objects
        static string mmfFileName = string.Empty;

        static MMF imageFile;
        #endregion Memory mapped file objects

        #endregion Inter-process Communication objects

        #region Threading objects

        static Thread HUBNotificationReceiverThread;

        #endregion Threading objects

        #region Window mode
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        #endregion Window mode

        #region Application performance info

        static float overallPerformance             = 0;
        static float HUBReceiverPerformance         = 0;
        static float PTAMPerformance                = 0;

        #endregion Application performance info

        #region PTAM result info

        static int xPosition = 0;
        static int yPosition = 0;
        static int zPosition = 0;

        static float xOrientation = 0;
        static float yOrientation = 0;
        static float zOrientation = 0;

        #endregion PTAM result info

        #region Other settings
        static Image<Bgra, byte> receivedImage;
        static bool isExitRequested     = false;
        static bool isStartFirstTime    = true;

        static int totalFrameCount           = 0;

        static string PTAMContentInfo   = string.Empty;

        #endregion Other settings

        #endregion Application informations

        static void ApplicationDataLoader()
        {
            HUBNotifierChannel = savedInformations.HUBNotifierChannel;
            modelerNotifierChannel = savedInformations.ModelerNotifierChannel;

            mmfFileName = savedInformations.MMFFileName;

            isStartHidden = savedInformations.StartHidden;
        }

        static void WindowMode()
        {
            // Hide/Show console based on configuration
            var handle = GetConsoleWindow();
            if (isStartHidden)
                ShowWindow(handle, SW_HIDE);
            else
                ShowWindow(handle, SW_SHOW);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Loading configurations...");
            ApplicationDataLoader();
            WindowMode();
            Console.WriteLine("Now start processing");
            MainProcess();
        }

        static void MainProcess()
        {
            while (true)
            {
                Stopwatch overallPerformanceWatcher = new Stopwatch();
                overallPerformanceWatcher.Start();
                
                try
                {
                    if (!isExitRequested)
                    {
                        HUBNotifierReceiver();

                        PTAMProcess();
                        DummyInfo();

                        ContentBuilder();
                        ModelerNotificationSender(modelerNotifierChannel, PTAMContentInfo);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
                overallPerformanceWatcher.Stop();
                Program.overallPerformance = overallPerformanceWatcher.ElapsedMilliseconds;
                totalFrameCount++;
            }
            
        }

        static void PTAMProcess()
        {
            Stopwatch PTAMProcessWatcher = new Stopwatch();
            PTAMProcessWatcher.Start();

            PTAMProcessWatcher.Stop();
            PTAMPerformance = PTAMProcessWatcher.ElapsedMilliseconds;
        }

        static void ImageLoader()
        {
            imageFile = new MMF();
            imageFile.OpenExisting(mmfFileName);
            using (var ms = new MemoryStream(Convert.FromBase64String(imageFile.ReadContent(MMF.DataType.DataString))))
            {
                receivedImage = new Image<Bgra, byte>(new Bitmap(ms));
            }
            
        }

        static void ContentBuilder()
        {
            /* Content layout
             * ---[ Main content
             * 1. Camera Position
             * 2. Camera Orientation
             * 3. Total created points
             * ---[ Performance content
             * 3. Overall performance
             * 4. HUB receiver performance
             * 5. Scene construction performance
             * 6. Scene conversion performance
             */
        }
        static void HUBNotifierReceiver()
        {
            Stopwatch HUBNotifierWatcher = new Stopwatch();
            HUBNotifierWatcher.Start();

            HUBNotifier = new NamedPipesServer();
            HUBNotifier.CreateNewServerPipe(HUBNotifierChannel, NamedPipesServer.PipeDirection.DirectionInOut, NamedPipesServer.SendMode.ByteMode);
            HUBNotifier.WaitForConnection();
            if ((char)HUBNotifier.ReadByte() == 'y')
            {
                ImageLoader();
            }
            HUBNotifier.WaitForPipeDrain();
            HUBNotifier.Disconnect();
            HUBNotifier.ClosePipe();

            HUBNotifierWatcher.Stop();
            HUBReceiverPerformance = HUBNotifierWatcher.ElapsedMilliseconds;
        }

        static void ModelerNotificationSender(string PipeName, string Content)
        {
            NamedPipeClient client = new NamedPipeClient(PipeName);
            if (!client.CheckConnection())
            {
                client.ConnectToServer();
            }
            byte[] tempLocation = new byte[Content.Length];
            int i = 0;
            foreach (char character in Content)
            {
                tempLocation[i] = (byte)character;
                i++;
            }
            client.WriteToServer(tempLocation, 0, tempLocation.Length);
            client.DisconnectToServer();
        }

        #region Sample functions

        // For setting dummy info
        static void DummyInfo()
        {
            xPosition = totalFrameCount;
            yPosition = totalFrameCount + 2;
            zPosition = totalFrameCount - 5;

            xOrientation = totalFrameCount;
            yOrientation = totalFrameCount + 2;
            zOrientation = totalFrameCount - 5;
        }

#endregion Sample functions
    }
}
