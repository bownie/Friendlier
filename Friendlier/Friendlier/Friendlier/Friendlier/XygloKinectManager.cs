using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace Xyglo
{
    public class XygloKinectEventArgs : EventArgs
    {
        public float m_distance;
    };

    public class XygloKinectManager
    {
        XygloKinectHelper m_kinectHelper;

        KinectSensor m_kinectSensor;

        private Skeleton[] m_skeletonData;

        public XygloKinectManager()
        {
            Logger.logMsg("XygloKinectManager::XygloKinectManager() - got " + KinectSensor.KinectSensors.Count + " kinect instances");
        }

        // A delegate type for hooking up change notifications.
        //
        public delegate void distanceMoved(object sender, XygloKinectEventArgs e);

        /// <summary>
        /// http://kinectxna.blogspot.com/2012/02/tutorial-2-moving-kinect-sensor.html
        /// </summary>
        public bool initialise()
        {
            // Return false if there are no kinects available
            //
            if (KinectSensor.KinectSensors.Count == 0)
            {
                return false;
            }

            // Generate the helper
            //
            if (m_kinectHelper == null)
            {
                m_kinectHelper = new XygloKinectHelper();
            }

            try
            {
                m_kinectSensor = KinectSensor.KinectSensors[0];
                m_kinectSensor.Start();

                m_kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                m_kinectSensor.DepthStream.Enable();

                m_kinectSensor.DepthFrameReady += this.kinectDepthFrameReady;
                m_kinectSensor.SkeletonFrameReady += this.kinectSkeletonsReady;
                m_kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters()
                {
                    Smoothing = 0.5f,
                    Correction = 0.5f,
                    Prediction = 0.5f,
                    JitterRadius = 0.05f,
                    MaxDeviationRadius = 0.04f
                });

            }
            catch (Exception e)
            {
                Logger.logMsg("XygloKinectManager::initialise() - couldn't start kinect " + e.Message);
            }
            //m_kinectHelper.InitializeKinectServices(m_kinectSensor);

            return true;
        }


        public void close()
        {
            if (m_kinectSensor != null)
            {
                try
                {
                    m_kinectSensor.Stop();
                }
                catch (Exception e)
                {
                    Logger.logMsg("XygloKinectManager::close() - can't stop kinect - " + e.Message);
                }
            }
        }

        /// <summary>
        /// The last position of the skeleton
        /// </summary>
        protected SkeletonPoint m_lastSkeletonPoint;

        /// <summary>
        /// Last tracked id
        /// </summary>
        protected int m_trackingId = -1;

        /// <summary>
        /// Have we acquired a user?
        /// </summary>
        /// <returns></returns>
        public bool gotUser()
        {
            return (m_trackingId != -1);
        }

        public SkeletonPoint getSkeleton()
        {
            return m_lastSkeletonPoint;
        }

        public string getSkeletonDetails()
        {
            string rS = "GOT SKELETON ID " + m_trackingId + " : X = " + m_lastSkeletonPoint.X + ", Y = " + m_lastSkeletonPoint.Y + ", Z = " + m_lastSkeletonPoint.Z;
            return rS;
        }


        short[] m_pixelData;

        DepthImageFormat m_lastImageFormat;

        /// <summary>
        /// How something is from the sensor
        /// </summary>
        protected float m_depthValue = 0;

        /// <summary>
        /// Return the depth value
        /// </summary>
        /// <returns></returns>
        public float getDepthValue()
        {
            return m_depthValue;
        }

        /// <summary>
        /// Check to see if we're stable against depth
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool depthIsStable(float value)
        {
            return (( m_depthValue * 0.95f ) < value && ( m_depthValue * 1.05f ) > value );
        }

        
        /// <summary>
        /// Handle the depth buffer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinectDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
//            Logger.logMsg("XygloKinectManager::kinectDepthFrameReady()");

            //SkeletonPoint sp = e.OpenDepthImageFrame().MapToSkeletonPoint(0, 0);
            //e.OpenDepthImageFrame().

            //Logger.logMsg("In front of me I got depth " + sp.Z);
            /*
            if ((m_pixelData == null) || (m_pixelData.Length != e.OpenDepthImageFrame().PixelDataLength))
            {
                m_pixelData = new short[e.OpenDepthImageFrame().PixelDataLength];
            }

            e.OpenDepthImageFrame().CopyPixelDataTo(m_pixelData);
            */

            using (DepthImageFrame imageFrame = e.OpenDepthImageFrame())
            {
                if (imageFrame != null)
                {
                    // We need to detect if the format has changed.
                    bool haveNewFormat = m_lastImageFormat != imageFrame.Format;

                    if (haveNewFormat)
                    {
                        m_pixelData = new short[imageFrame.PixelDataLength];
                        //this.depthFrame32 = new byte[imageFrame.Width * imageFrame.Height * Bgr32BytesPerPixel];
                    }

                    float width = (float)imageFrame.Width;
                    float height = (float)imageFrame.Height;

                    imageFrame.CopyPixelDataTo(m_pixelData);

                    int middlePixel = (int)((width + 0.5) * (height / 2.0f));

                    // Let's take a middle third sample
                    //
                    int endPosition = (int)((width * 2.0f * height / 3.0f) + (width * 2.0f / 3.0f));
                    int startPosition = (int)((width * height / 3.0f) + (width / 3.0f));
                    int curPos = startPosition;
                    int curHeight = (int)(height / 3.0f);
                    float counter = 0;
                    float sampleTotal = 0.0f;
                    while (curPos < endPosition)
                    {
                        curPos = (int)(width * curHeight + (width / 3.0f));
                        while ((curPos - (curHeight * width)) < (int)(width * 2.0f / 3.0f))
                        {
                            sampleTotal += m_pixelData[curPos];
                            counter++;
                            curPos++;
                        }
                        curHeight++;
                    }

                    float total = sampleTotal / counter;


                    //SkeletonPoint sp = imageFrame.MapToSkeletonPoint(0, 0);

                    //Logger.logMsg("Z IS = " + m_pixelData[middlePixel]);
                    //Logger.logMsg("AV IS = " + total);

                    m_depthValue = total;

                    /*
                    byte[] convertedDepthBits = this.ConvertDepthFrame(this.pixelData, ((KinectSensor)sender).DepthStream);

                    // A WriteableBitmap is a WPF construct that enables resetting the Bits of the image.
                    // This is more efficient than creating a new Bitmap every frame.
                    if (haveNewFormat)
                    {
                        this.outputBitmap = new WriteableBitmap(
                            imageFrame.Width,
                            imageFrame.Height,
                            96,  // DpiX
                            96,  // DpiY
                            PixelFormats.Bgr32,
                            null);

                        this.kinectDepthImage.Source = this.outputBitmap;
                    }

                    this.outputBitmap.WritePixels(
                        new Int32Rect(0, 0, imageFrame.Width, imageFrame.Height),
                        convertedDepthBits,
                        imageFrame.Width * Bgr32BytesPerPixel,
                        0);

                    this.lastImageFormat = imageFrame.Format;

                    UpdateFrameRate();
                     * */

                    m_lastImageFormat = imageFrame.Format;
                }
            }
        }

        private void kinectSkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    int skeletonSlot = 0;

                    if ((m_skeletonData == null) || (m_skeletonData.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        m_skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(m_skeletonData);


                    foreach (Skeleton skeleton in m_skeletonData)
                    {
                        if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                        {
                            //User player;
                            Logger.logMsg("XygloKinectManager::skeletonsReady - acquired user");

                            if (m_trackingId == -1)
                            {
                                m_lastSkeletonPoint = skeleton.Position;
                                m_trackingId = skeleton.TrackingId;
                                //skeleton.TrackingState
                            }
                            else
                            {
                                if (skeleton.TrackingId == m_trackingId)
                                {
                                    if (skeleton.Position != m_lastSkeletonPoint)
                                    {
                                        Logger.logMsg("SKELETON ID " + m_trackingId + " : X = " + skeleton.Position.X + ", Y = " + skeleton.Position.Y + ", Z = " + skeleton.Position.Z);
                                        m_lastSkeletonPoint = skeleton.Position;
                                    }
                                }
                            }

                            /*
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
                             */
                        }

                        skeletonSlot++;
                    }
                }
            }
        }
    }
}
