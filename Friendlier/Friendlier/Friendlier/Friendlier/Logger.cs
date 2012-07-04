#region File Description
//-----------------------------------------------------------------------------
// Logger.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Diagnostics;
using System.Reflection;

namespace Xyglo
{
    /// <summary>
    /// Logging class for Xyglo applications - we can specify if we want to see a log timestamp
    /// and a stack trace for example.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Are we logging to a file?
        /// </summary>
        public static bool m_toFile = true;

        /// <summary>
        /// Are we logging to the console?
        /// </summary>
        public static bool m_toConsole = true;

        /// <summary>
        /// Are we logging time?
        /// </summary>
        public static bool m_showTime = true;

        /// <summary>
        /// Log file path
        /// </summary>
        public static string m_logFile = Project.getUserDataPath() + VersionInformation.getProductName() + VersionInformation.getProductVersion() + ".log";

        /// <summary>
        /// Log file handle
        /// </summary>
        public static System.IO.StreamWriter m_logFileStream = null;

        /// <summary>
        /// Static string for generating message
        /// </summary>
        public static string m_message = "";

        /// <summary>
        /// Sometimes we have performance issues with the time formatting so provide it as optional
        /// </summary>
        /// <param name="message"></param>
        /// <param name="showTime"></param>
        public static void logMsg(string message, bool showTime = false, bool showStack = false)
        {
            // Empty message
            //
            m_message = "";

            if (m_showTime)
            {
                //Console.Write(DateTimeNowCache.GetDateTime().ToString() + " - ");
                m_message += DateTime.Now.ToString() + " - ";
            }

            if (showStack)
            {
                StackTrace stackTrace = new StackTrace();
                m_message += stackTrace.GetFrame(0) + " - ";
            }

            m_message += message;

            if (m_toConsole)
            {
                Console.WriteLine(m_message);
            }

            if (m_toFile)
            {
                if (m_logFileStream == null)
                {
                    // Open log file in append mode
                    //
                    m_logFileStream = new System.IO.StreamWriter(m_logFile, true);
                }

                m_logFileStream.WriteLine(m_message);
                m_logFileStream.Flush();
            }

        }

#if NOT_USED
        private static void WhatsMyName()
        {
            StackFrame stackFrame = new StackFrame();
            MethodBase methodBase = stackFrame.GetMethod();
            Console.WriteLine(methodBase.Name); // Displays “WhatsmyName”
            WhoCalledMe();
        }

        // Function to display parent function
        private static void WhoCalledMe()
        {
            StackTrace stackTrace = new StackTrace();
            StackFrame stackFrame = stackTrace.GetFrame(1);
            MethodBase methodBase = stackFrame.GetMethod();
            // Displays “WhatsmyName”
            Console.WriteLine(" Parent Method Name {0} ", methodBase.Name);
        }
#endif
    }
}
