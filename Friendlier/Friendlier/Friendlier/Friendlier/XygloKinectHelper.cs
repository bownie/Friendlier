using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
//using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Kinect;
//using ShapeGame.Speech;
//using ShapeGame.Utils;

namespace Xyglo
{


    //------------------------------------------------------------------------------
    // <copyright file="MainWindow.xaml.cs" company="Microsoft">
    //     Copyright (c) Microsoft Corporation.  All rights reserved.
    // </copyright>
    //------------------------------------------------------------------------------

    // This module contains code to do Kinect NUI initialization,
    // processing, displaying players on screen, and sending updated player
    // positions to the game portion for hit testing.

    

    public class XygloKinectHelper
    {
        private Skeleton[] skeletonData;

        
        private void SensorChooserKinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                this.UninitializeKinectServices((KinectSensor)e.OldValue);
            }

            // Only enable this checkbox if we have a sensor
            //enableAec.IsEnabled = e.NewValue != null;

            if (e.NewValue != null)
            {
                this.InitializeKinectServices((KinectSensor)e.NewValue);
            }
        }
        
        // Kinect enabled apps should customize which Kinect services it initializes here.
        public  KinectSensor InitializeKinectServices(KinectSensor sensor)
        {
            // Application should enable all streams first.
            //SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;



            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            
            sensor.SkeletonFrameReady += this.SkeletonsReady;
            sensor.SkeletonStream.Enable(new TransformSmoothParameters()
                                             {
                                                 Smoothing = 0.5f,
                                                 Correction = 0.5f,
                                                 Prediction = 0.5f,
                                                 JitterRadius = 0.05f,
                                                 MaxDeviationRadius = 0.04f
                                             });

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                //SensorChooser.AppConflictOccurred();
                return null;
            }
            /*
            // Start speech recognizer after KinectSensor.Start() is called
            // returns null if problem with speech prereqs or instantiation.
            this.mySpeechRecognizer = SpeechRecognizer.Create();
            this.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            this.mySpeechRecognizer.Start(sensor.AudioSource);
            enableAec.Visibility = Visibility.Visible;
            this.UpdateEchoCancellation(this.enableAec);
            */

            Logger.logMsg("XygloKinectHelper::InitializeKinectServices() - initialised");
            return sensor;
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.Stop();

            sensor.SkeletonFrameReady -= this.SkeletonsReady;

            /*
            if (this.mySpeechRecognizer != null)
            {
                this.mySpeechRecognizer.Stop();
                this.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
                this.mySpeechRecognizer.Dispose();
                this.mySpeechRecognizer = null;
            }

            enableAec.Visibility = Visibility.Collapsed;
             * */
        }

        //#endregion Kinect discovery + setup

        #region Kinect Skeleton processing
        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Logger.logMsg("XygloKinectHelper::skeletonsReady - processing a skeleton");

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);


                    foreach (Skeleton skeleton in this.skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            /*
                            Player player;
                            if (this.players.ContainsKey(skeletonSlot))
                            {
                                player = this.players[skeletonSlot];
                            }
                            else
                            {
                                player = new Player(skeletonSlot);
                                player.SetBounds(this.playerBounds);
                                this.players.Add(skeletonSlot, player);
                            }

                            player.LastUpdated = DateTime.Now;

                            // Update player's bone and joint positions
                            if (skeleton.Joints.Count > 0)
                            {
                                player.IsAlive = true;

                                // Head, hands, feet (hit testing happens in order here)
                                player.UpdateJointPosition(skeleton.Joints, JointType.Head);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandRight);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootRight);

                                // Hands and arms
                                player.UpdateBonePosition(skeleton.Joints, JointType.HandRight, JointType.WristRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristRight, JointType.ElbowRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowRight, JointType.ShoulderRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HandLeft, JointType.WristLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristLeft, JointType.ElbowLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowLeft, JointType.ShoulderLeft);

                                // Head and Shoulders
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.Head);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderLeft, JointType.ShoulderCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);

                                // Legs
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.HipCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                                // Spine
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.ShoulderCenter);
                             
                            }
                             * */
                        }

                        skeletonSlot++;
                    }
                }
            }
        }

        /*
        private void CheckPlayers()
        {
            foreach (var player in this.players)
            {
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    this.players.Remove(player.Value.GetId());
                    break;
                }
            }

            // Count alive players
            int alive = this.players.Count(player => player.Value.IsAlive);

            if (alive != this.playersAlive)
            {
                if (alive == 2)
                {
                    this.myFallingThings.SetGameMode(GameMode.TwoPlayer);
                }
                else if (alive == 1)
                {
                    this.myFallingThings.SetGameMode(GameMode.Solo);
                }
                else if (alive == 0)
                {
                    this.myFallingThings.SetGameMode(GameMode.Off);
                }

                if ((this.playersAlive == 0) && (this.mySpeechRecognizer != null))
                {
                    BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        this.screenRect,
                        true,
                        System.Windows.Media.Color.FromArgb(200, 255, 255, 255));
                }

                this.playersAlive = alive;
            }
        }

        private void PlayfieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdatePlayfieldSize();
        }

        private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            this.screenRect.X = 0;
            this.screenRect.Y = 0;
            this.screenRect.Width = this.playfield.ActualWidth;
            this.screenRect.Height = this.playfield.ActualHeight;

            BannerText.UpdateBounds(this.screenRect);

            this.playerBounds.X = 0;
            this.playerBounds.Width = this.playfield.ActualWidth;
            this.playerBounds.Y = this.playfield.ActualHeight * 0.2;
            this.playerBounds.Height = this.playfield.ActualHeight * 0.75;

            foreach (var player in this.players)
            {
                player.Value.SetBounds(this.playerBounds);
            }

            Rect fallingBounds = this.playerBounds;
            fallingBounds.Y = 0;
            fallingBounds.Height = playfield.ActualHeight;
            if (this.myFallingThings != null)
            {
                this.myFallingThings.SetBoundaries(fallingBounds);
            }
        }
         */
        #endregion Kinect Skeleton processing
        

        #region GameTimer/Thread
        private void GameThread()
        {
            /*
            this.runningGameThread = true;
            this.predNextFrame = DateTime.Now;
            this.actualFrameTime = 1000.0 / this.targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (this.runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (this.lastFrameDrawn == DateTime.MinValue)
                {
                    this.lastFrameDrawn = now;
                }

                double ms = now.Subtract(this.lastFrameDrawn).TotalMilliseconds;
                this.actualFrameTime = (this.actualFrameTime * 0.95) + (0.05 * ms);
                this.lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                this.frameCount++;
                if ((this.frameCount % 100 == 0) && (1000.0 / this.actualFrameTime < this.targetFramerate * 0.92))
                {
                    this.targetFramerate = Math.Max(MinFramerate, (this.targetFramerate + (1000.0 / this.actualFrameTime)) / 2);
                }

                if (now > this.predNextFrame)
                {
                    this.predNextFrame = now;
                }
                else
                {
                    double milliseconds = this.predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                this.predNextFrame += TimeSpan.FromMilliseconds(1000.0 / this.targetFramerate);

                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(this.HandleGameTimer), 0);
             
            }
             * */
        }

        private void HandleGameTimer(int param)
        {
            /*
            // Every so often, notify what our actual framerate is
            if ((this.frameCount % 100) == 0)
            {
                this.myFallingThings.SetFramerate(1000.0 / this.actualFrameTime);
            }

            // Advance animations, and do hit testing.
            for (int i = 0; i < NumIntraFrames; ++i)
            {
                foreach (var pair in this.players)
                {
                    HitType hit = this.myFallingThings.LookForHits(pair.Value.Segments, pair.Value.GetId());
                    if ((hit & HitType.Squeezed) != 0)
                    {
                        this.squeezeSound.Play();
                    }
                    else if ((hit & HitType.Popped) != 0)
                    {
                        this.popSound.Play();
                    }
                    else if ((hit & HitType.Hand) != 0)
                    {
                        this.hitSound.Play();
                    }
                }

                this.myFallingThings.AdvanceFrame();
            }

            // Draw new Wpf scene by adding all objects to canvas
            playfield.Children.Clear();
            this.myFallingThings.DrawFrame(this.playfield.Children);
            foreach (var player in this.players)
            {
                player.Value.Draw(playfield.Children);
            }

            BannerText.Draw(playfield.Children);
            FlyingText.Draw(playfield.Children);

            this.CheckPlayers();
             * */
        }
        #endregion GameTimer/Thread

        /*
        #region Kinect Speech processing
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), e.Matched);
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.Pause:
                    this.myFallingThings.SetDropRate(0);
                    this.myFallingThings.SetGravity(0);
                    break;
                case SpeechRecognizer.Verbs.Resume:
                    this.myFallingThings.SetDropRate(this.dropRate);
                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Reset:
                    this.dropRate = DefaultDropRate;
                    this.dropSize = DefaultDropSize;
                    this.dropGravity = DefaultDropGravity;
                    this.myFallingThings.SetPolies(PolyType.All);
                    this.myFallingThings.SetDropRate(this.dropRate);
                    this.myFallingThings.SetGravity(this.dropGravity);
                    this.myFallingThings.SetSize(this.dropSize);
                    this.myFallingThings.SetShapesColor(System.Windows.Media.Color.FromRgb(0, 0, 0), true);
                    this.myFallingThings.Reset();
                    break;
                case SpeechRecognizer.Verbs.DoShapes:
                    this.myFallingThings.SetPolies(e.Shape);
                    break;
                case SpeechRecognizer.Verbs.RandomColors:
                    this.myFallingThings.SetShapesColor(System.Windows.Media.Color.FromRgb(0, 0, 0), true);
                    break;
                case SpeechRecognizer.Verbs.Colorize:
                    this.myFallingThings.SetShapesColor(e.RgbColor, false);
                    break;
                case SpeechRecognizer.Verbs.ShapesAndColors:
                    this.myFallingThings.SetPolies(e.Shape);
                    this.myFallingThings.SetShapesColor(e.RgbColor, false);
                    break;
                case SpeechRecognizer.Verbs.More:
                    this.dropRate *= 1.5;
                    this.myFallingThings.SetDropRate(this.dropRate);
                    break;
                case SpeechRecognizer.Verbs.Fewer:
                    this.dropRate /= 1.5;
                    this.myFallingThings.SetDropRate(this.dropRate);
                    break;
                case SpeechRecognizer.Verbs.Bigger:
                    this.dropSize *= 1.5;
                    if (this.dropSize > MaxShapeSize)
                    {
                        this.dropSize = MaxShapeSize;
                    }

                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Biggest:
                    this.dropSize = MaxShapeSize;
                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smaller:
                    this.dropSize /= 1.5;
                    if (this.dropSize < MinShapeSize)
                    {
                        this.dropSize = MinShapeSize;
                    }

                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smallest:
                    this.dropSize = MinShapeSize;
                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Faster:
                    this.dropGravity *= 1.25;
                    if (this.dropGravity > 4.0)
                    {
                        this.dropGravity = 4.0;
                    }

                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Slower:
                    this.dropGravity /= 1.25;
                    if (this.dropGravity < 0.25)
                    {
                        this.dropGravity = 0.25;
                    }

                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
            }
        }

        //private void EnableAecChecked(object sender, RoutedEventArgs e)
        //{
            //CheckBox enableAecCheckBox = (CheckBox)sender;
            //this.UpdateEchoCancellation(enableAecCheckBox);
        //}

        //private void UpdateEchoCancellation(CheckBox aecCheckBox)
        //{
            //this.mySpeechRecognizer.EchoCancellationMode = aecCheckBox.IsChecked != null && aecCheckBox.IsChecked.Value
                //? EchoCancellationMode.CancellationAndSuppression
                //: EchoCancellationMode.None;
        //}

        #endregion Kinect Speech processing
         * */
    }
}





// ------------------------------------------------------------------------------ JUST FOR INFO ------




/*

//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

// This module contains code to do Kinect NUI initialization,
// processing, displaying players on screen, and sending updated player
// positions to the game portion for hit testing.

namespace ShapeGame
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Media;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Microsoft.Kinect;
    using ShapeGame.Speech;
    using ShapeGame.Utils;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        #region Private State
        private const int TimerResolution = 2;  // ms
        private const int NumIntraFrames = 3;
        private const int MaxShapes = 80;
        private const double MaxFramerate = 70;
        private const double MinFramerate = 15;
        private const double MinShapeSize = 12;
        private const double MaxShapeSize = 90;
        private const double DefaultDropRate = 2.5;
        private const double DefaultDropSize = 32.0;
        private const double DefaultDropGravity = 1.0;

        private readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        private readonly SoundPlayer popSound = new SoundPlayer();
        private readonly SoundPlayer hitSound = new SoundPlayer();
        private readonly SoundPlayer squeezeSound = new SoundPlayer();

        private double dropRate = DefaultDropRate;
        private double dropSize = DefaultDropSize;
        private double dropGravity = DefaultDropGravity;
        private DateTime lastFrameDrawn = DateTime.MinValue;
        private DateTime predNextFrame = DateTime.MinValue;
        private double actualFrameTime;

        private Skeleton[] skeletonData;

        // Player(s) placement in scene (z collapsed):
        private Rect playerBounds;
        private Rect screenRect;

        private double targetFramerate = MaxFramerate;
        private int frameCount;
        private bool runningGameThread;
        private FallingThings myFallingThings;
        private int playersAlive;

        private SpeechRecognizer mySpeechRecognizer;

        #endregion Private State

        #region ctor + Window Events
        public MainWindow()
        {
            InitializeComponent();
            this.RestoreWindowState();
        }

        // Since the timer resolution defaults to about 10ms precisely, we need to
        // increase the resolution to get framerates above between 50fps with any
        // consistency.
        [DllImport("Winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(uint period);

        private void RestoreWindowState()
        {
            // Restore window state to that last used
            Rect bounds = Properties.Settings.Default.PrevWinPosition;
            if (bounds.Right != bounds.Left)
            {
                this.Top = bounds.Top;
                this.Left = bounds.Left;
                this.Height = bounds.Height;
                this.Width = bounds.Width;
            }

            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void WindowLoaded(object sender, EventArgs e)
        {
            playfield.ClipToBounds = true;

            this.myFallingThings = new FallingThings(MaxShapes, this.targetFramerate, NumIntraFrames);

            this.UpdatePlayfieldSize();

            this.myFallingThings.SetGravity(this.dropGravity);
            this.myFallingThings.SetDropRate(this.dropRate);
            this.myFallingThings.SetSize(this.dropSize);
            this.myFallingThings.SetPolies(PolyType.All);
            this.myFallingThings.SetGameMode(GameMode.Off);

            SensorChooser.KinectSensorChanged += this.SensorChooserKinectSensorChanged;

            this.popSound.Stream = Properties.Resources.Pop_5;
            this.hitSound.Stream = Properties.Resources.Hit_2;
            this.squeezeSound.Stream = Properties.Resources.Squeeze;

            this.popSound.Play();

            TimeBeginPeriod(TimerResolution);
            var myGameThread = new Thread(this.GameThread);
            myGameThread.SetApartmentState(ApartmentState.STA);
            myGameThread.Start();

            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), "Shapes!");
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            this.runningGameThread = false;
            Properties.Settings.Default.PrevWinPosition = this.RestoreBounds;
            Properties.Settings.Default.WindowState = (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            SensorChooser.Kinect = null;
        }

        #endregion ctor + Window Events

        #region Kinect discovery + setup

        private void SensorChooserKinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                this.UninitializeKinectServices((KinectSensor)e.OldValue);
            }

            // Only enable this checkbox if we have a sensor
            enableAec.IsEnabled = e.NewValue != null;

            if (e.NewValue != null)
            {
                this.InitializeKinectServices((KinectSensor)e.NewValue);
            }
        }

        // Kinect enabled apps should customize which Kinect services it initializes here.
        private KinectSensor InitializeKinectServices(KinectSensor sensor)
        {
            // Application should enable all streams first.
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            sensor.SkeletonFrameReady += this.SkeletonsReady;
            sensor.SkeletonStream.Enable(new TransformSmoothParameters()
                                             {
                                                 Smoothing = 0.5f,
                                                 Correction = 0.5f,
                                                 Prediction = 0.5f,
                                                 JitterRadius = 0.05f,
                                                 MaxDeviationRadius = 0.04f
                                             });

            try
            {
                sensor.Start();
            }
            catch (IOException)
            {
                SensorChooser.AppConflictOccurred();
                return null;
            }

            // Start speech recognizer after KinectSensor.Start() is called
            // returns null if problem with speech prereqs or instantiation.
            this.mySpeechRecognizer = SpeechRecognizer.Create();
            this.mySpeechRecognizer.SaidSomething += this.RecognizerSaidSomething;
            this.mySpeechRecognizer.Start(sensor.AudioSource);
            enableAec.Visibility = Visibility.Visible;
            this.UpdateEchoCancellation(this.enableAec);

            return sensor;
        }

        // Kinect enabled apps should uninitialize all Kinect services that were initialized in InitializeKinectServices() here.
        private void UninitializeKinectServices(KinectSensor sensor)
        {
            sensor.Stop();

            sensor.SkeletonFrameReady -= this.SkeletonsReady;

            if (this.mySpeechRecognizer != null)
            {
                this.mySpeechRecognizer.Stop();
                this.mySpeechRecognizer.SaidSomething -= this.RecognizerSaidSomething;
                this.mySpeechRecognizer.Dispose();
                this.mySpeechRecognizer = null;
            }

            enableAec.Visibility = Visibility.Collapsed;
        }

        #endregion Kinect discovery + setup

        #region Kinect Skeleton processing
        private void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((this.skeletonData == null) || (this.skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);

                    foreach (Skeleton skeleton in this.skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            Player player;
                            if (this.players.ContainsKey(skeletonSlot))
                            {
                                player = this.players[skeletonSlot];
                            }
                            else
                            {
                                player = new Player(skeletonSlot);
                                player.SetBounds(this.playerBounds);
                                this.players.Add(skeletonSlot, player);
                            }

                            player.LastUpdated = DateTime.Now;

                            // Update player's bone and joint positions
                            if (skeleton.Joints.Count > 0)
                            {
                                player.IsAlive = true;

                                // Head, hands, feet (hit testing happens in order here)
                                player.UpdateJointPosition(skeleton.Joints, JointType.Head);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.HandRight);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootLeft);
                                player.UpdateJointPosition(skeleton.Joints, JointType.FootRight);

                                // Hands and arms
                                player.UpdateBonePosition(skeleton.Joints, JointType.HandRight, JointType.WristRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristRight, JointType.ElbowRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowRight, JointType.ShoulderRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HandLeft, JointType.WristLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.WristLeft, JointType.ElbowLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ElbowLeft, JointType.ShoulderLeft);

                                // Head and Shoulders
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.Head);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderLeft, JointType.ShoulderCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.ShoulderCenter, JointType.ShoulderRight);

                                // Legs
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.KneeLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeLeft, JointType.AnkleLeft);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleLeft, JointType.FootLeft);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipRight, JointType.KneeRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.KneeRight, JointType.AnkleRight);
                                player.UpdateBonePosition(skeleton.Joints, JointType.AnkleRight, JointType.FootRight);

                                player.UpdateBonePosition(skeleton.Joints, JointType.HipLeft, JointType.HipCenter);
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.HipRight);

                                // Spine
                                player.UpdateBonePosition(skeleton.Joints, JointType.HipCenter, JointType.ShoulderCenter);
                            }
                        }

                        skeletonSlot++;
                    }
                }
            }
        }

        private void CheckPlayers()
        {
            foreach (var player in this.players)
            {
                if (!player.Value.IsAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    this.players.Remove(player.Value.GetId());
                    break;
                }
            }

            // Count alive players
            int alive = this.players.Count(player => player.Value.IsAlive);

            if (alive != this.playersAlive)
            {
                if (alive == 2)
                {
                    this.myFallingThings.SetGameMode(GameMode.TwoPlayer);
                }
                else if (alive == 1)
                {
                    this.myFallingThings.SetGameMode(GameMode.Solo);
                }
                else if (alive == 0)
                {
                    this.myFallingThings.SetGameMode(GameMode.Off);
                }

                if ((this.playersAlive == 0) && (this.mySpeechRecognizer != null))
                {
                    BannerText.NewBanner(
                        Properties.Resources.Vocabulary,
                        this.screenRect,
                        true,
                        System.Windows.Media.Color.FromArgb(200, 255, 255, 255));
                }

                this.playersAlive = alive;
            }
        }

        private void PlayfieldSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdatePlayfieldSize();
        }

        private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            this.screenRect.X = 0;
            this.screenRect.Y = 0;
            this.screenRect.Width = this.playfield.ActualWidth;
            this.screenRect.Height = this.playfield.ActualHeight;

            BannerText.UpdateBounds(this.screenRect);

            this.playerBounds.X = 0;
            this.playerBounds.Width = this.playfield.ActualWidth;
            this.playerBounds.Y = this.playfield.ActualHeight * 0.2;
            this.playerBounds.Height = this.playfield.ActualHeight * 0.75;

            foreach (var player in this.players)
            {
                player.Value.SetBounds(this.playerBounds);
            }

            Rect fallingBounds = this.playerBounds;
            fallingBounds.Y = 0;
            fallingBounds.Height = playfield.ActualHeight;
            if (this.myFallingThings != null)
            {
                this.myFallingThings.SetBoundaries(fallingBounds);
            }
        }
        #endregion Kinect Skeleton processing

        #region GameTimer/Thread
        private void GameThread()
        {
            this.runningGameThread = true;
            this.predNextFrame = DateTime.Now;
            this.actualFrameTime = 1000.0 / this.targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (this.runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (this.lastFrameDrawn == DateTime.MinValue)
                {
                    this.lastFrameDrawn = now;
                }

                double ms = now.Subtract(this.lastFrameDrawn).TotalMilliseconds;
                this.actualFrameTime = (this.actualFrameTime * 0.95) + (0.05 * ms);
                this.lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                this.frameCount++;
                if ((this.frameCount % 100 == 0) && (1000.0 / this.actualFrameTime < this.targetFramerate * 0.92))
                {
                    this.targetFramerate = Math.Max(MinFramerate, (this.targetFramerate + (1000.0 / this.actualFrameTime)) / 2);
                }

                if (now > this.predNextFrame)
                {
                    this.predNextFrame = now;
                }
                else
                {
                    double milliseconds = this.predNextFrame.Subtract(now).TotalMilliseconds;
                    if (milliseconds >= TimerResolution)
                    {
                        Thread.Sleep((int)(milliseconds + 0.5));
                    }
                }

                this.predNextFrame += TimeSpan.FromMilliseconds(1000.0 / this.targetFramerate);

                this.Dispatcher.Invoke(DispatcherPriority.Send, new Action<int>(this.HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {
            // Every so often, notify what our actual framerate is
            if ((this.frameCount % 100) == 0)
            {
                this.myFallingThings.SetFramerate(1000.0 / this.actualFrameTime);
            }

            // Advance animations, and do hit testing.
            for (int i = 0; i < NumIntraFrames; ++i)
            {
                foreach (var pair in this.players)
                {
                    HitType hit = this.myFallingThings.LookForHits(pair.Value.Segments, pair.Value.GetId());
                    if ((hit & HitType.Squeezed) != 0)
                    {
                        this.squeezeSound.Play();
                    }
                    else if ((hit & HitType.Popped) != 0)
                    {
                        this.popSound.Play();
                    }
                    else if ((hit & HitType.Hand) != 0)
                    {
                        this.hitSound.Play();
                    }
                }

                this.myFallingThings.AdvanceFrame();
            }

            // Draw new Wpf scene by adding all objects to canvas
            playfield.Children.Clear();
            this.myFallingThings.DrawFrame(this.playfield.Children);
            foreach (var player in this.players)
            {
                player.Value.Draw(playfield.Children);
            }

            BannerText.Draw(playfield.Children);
            FlyingText.Draw(playfield.Children);

            this.CheckPlayers();
        }
        #endregion GameTimer/Thread

        #region Kinect Speech processing
        private void RecognizerSaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            FlyingText.NewFlyingText(this.screenRect.Width / 30, new Point(this.screenRect.Width / 2, this.screenRect.Height / 2), e.Matched);
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.Pause:
                    this.myFallingThings.SetDropRate(0);
                    this.myFallingThings.SetGravity(0);
                    break;
                case SpeechRecognizer.Verbs.Resume:
                    this.myFallingThings.SetDropRate(this.dropRate);
                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Reset:
                    this.dropRate = DefaultDropRate;
                    this.dropSize = DefaultDropSize;
                    this.dropGravity = DefaultDropGravity;
                    this.myFallingThings.SetPolies(PolyType.All);
                    this.myFallingThings.SetDropRate(this.dropRate);
                    this.myFallingThings.SetGravity(this.dropGravity);
                    this.myFallingThings.SetSize(this.dropSize);
                    this.myFallingThings.SetShapesColor(System.Windows.Media.Color.FromRgb(0, 0, 0), true);
                    this.myFallingThings.Reset();
                    break;
                case SpeechRecognizer.Verbs.DoShapes:
                    this.myFallingThings.SetPolies(e.Shape);
                    break;
                case SpeechRecognizer.Verbs.RandomColors:
                    this.myFallingThings.SetShapesColor(System.Windows.Media.Color.FromRgb(0, 0, 0), true);
                    break;
                case SpeechRecognizer.Verbs.Colorize:
                    this.myFallingThings.SetShapesColor(e.RgbColor, false);
                    break;
                case SpeechRecognizer.Verbs.ShapesAndColors:
                    this.myFallingThings.SetPolies(e.Shape);
                    this.myFallingThings.SetShapesColor(e.RgbColor, false);
                    break;
                case SpeechRecognizer.Verbs.More:
                    this.dropRate *= 1.5;
                    this.myFallingThings.SetDropRate(this.dropRate);
                    break;
                case SpeechRecognizer.Verbs.Fewer:
                    this.dropRate /= 1.5;
                    this.myFallingThings.SetDropRate(this.dropRate);
                    break;
                case SpeechRecognizer.Verbs.Bigger:
                    this.dropSize *= 1.5;
                    if (this.dropSize > MaxShapeSize)
                    {
                        this.dropSize = MaxShapeSize;
                    }

                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Biggest:
                    this.dropSize = MaxShapeSize;
                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smaller:
                    this.dropSize /= 1.5;
                    if (this.dropSize < MinShapeSize)
                    {
                        this.dropSize = MinShapeSize;
                    }

                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smallest:
                    this.dropSize = MinShapeSize;
                    this.myFallingThings.SetSize(this.dropSize);
                    break;
                case SpeechRecognizer.Verbs.Faster:
                    this.dropGravity *= 1.25;
                    if (this.dropGravity > 4.0)
                    {
                        this.dropGravity = 4.0;
                    }

                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Slower:
                    this.dropGravity /= 1.25;
                    if (this.dropGravity < 0.25)
                    {
                        this.dropGravity = 0.25;
                    }

                    this.myFallingThings.SetGravity(this.dropGravity);
                    break;
            }
        }

        private void EnableAecChecked(object sender, RoutedEventArgs e)
        {
            CheckBox enableAecCheckBox = (CheckBox)sender;
            this.UpdateEchoCancellation(enableAecCheckBox);
        }

        private void UpdateEchoCancellation(CheckBox aecCheckBox)
        {
            this.mySpeechRecognizer.EchoCancellationMode = aecCheckBox.IsChecked != null && aecCheckBox.IsChecked.Value
                ? EchoCancellationMode.CancellationAndSuppression
                : EchoCancellationMode.None;
        }

        #endregion Kinect Speech processing
    }
}
*/