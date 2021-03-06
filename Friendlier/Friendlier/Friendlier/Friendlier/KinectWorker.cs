﻿#region File Description
//-----------------------------------------------------------------------------
// KinectWorker.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Management;

namespace Xyglo
{
    /// <summary>
    /// We have a KinecteWorker thread for initialising the (slow to start) Kinect interface.
    /// </summary>
    public class KinectWorker
    {
        XygloKinectManager m_kinectManager = null;

        /// <summary>
        /// Initialise out Kinect
        /// </summary>
        public void initialise()
        {
#if GOT_KINECT
            // Generate a kinect manager
            //
            m_kinectManager = new XygloKinectManager();

            if (!m_kinectManager.initialise())
            {
                Logger.logMsg("Friendlier::initialiseProject() - no kinect device found");
            }
#endif
        }

        /// <summary>
        /// This method is called when the thread is started
        /// </summary>
        public void startWorking()
        {
            if (m_kinectManager == null)
            {
                Logger.logMsg("KinectWorker::startWorking() - initialising the worker thread for kinect");
                initialise();
            }

            while (!m_shouldStop)
            {
                processDepthInformation();
                Thread.Sleep(50); // sleep for 10ms
            }

#if GOT_KINECT
            m_kinectManager.close();
#endif
            Console.WriteLine("KinectWorker::startWorking() - terminating gracefully");
        }

        
        /// <summary>
        /// Depth position memory
        /// </summary>
        public float m_lastDepthPosition = 0;

        /// <summary>
        /// Initial depth position
        /// </summary>
        public float m_initialDepthPosition = 0;

        /// <summary>
        /// Process depth information from the Kinect
        /// </summary>
        protected void processDepthInformation()
        {
#if GOT_KINECT
            if (m_kinectManager.getDepthValue() != 0)
            {
                if (m_initialDepthPosition == 0)
                {
                    m_initialDepthPosition = m_kinectManager.getDepthValue();
                }

                /*
                if (gameTime.TotalGameTime.Milliseconds % 250 == 0)
                {
                    if (m_kinectManager.depthIsStable(m_lastDepthPosition))
                    {
                        //Logger.logMsg("DEPTH STABLE @ " + m_kinectManager.getDepthValue());
                        ;
                    }
                    else
                    {
                        //m_lastDepthPosition = m_kinectManager.getDepthValue();
                        //Logger.logMsg("LAST = " + m_lastDepthPosition);
                        //Logger.logMsg("NEW  = " + m_kinectManager.getDepthValue());


                        // Move to new position
                        //Vector3 newPosition = m_eye;
                        //newPosition.Z += (m_kinectManager.getDepthValue() - m_initialDepthPosition) / 100.0f;
                        //flyToPosition(newPosition);
                    }

                    m_lastDepthPosition = m_kinectManager.getDepthValue();

                }
                 * */
            }
#endif // GOT_KINECT
        }

        /// <summary>
        /// Stop this thread
        /// </summary>
        public void requestStop()
        {
            m_shouldStop = true;
        }

        /// <summary>
        /// Volatile is used as hint to the compiler that this data member will be accessed by multiple threads.
        /// </summary>
        private volatile bool m_shouldStop;
    }
}
