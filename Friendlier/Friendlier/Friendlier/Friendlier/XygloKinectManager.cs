using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace Xyglo
{
    public class XygloKinectManager
    {
        XygloKinectHelper m_kinectHelper;

        KinectSensor m_kinectSensor;

        public XygloKinectManager()
        {
            Logger.logMsg("XygloKinectManager::XygloKinectManager() - got " + KinectSensor.KinectSensors.Count + " kinect instances");
        }


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
    }
}
