using System;
using System.Diagnostics;
using System.Reflection;

namespace Xyglo
{
    /// <summary>
    /// Logging class for Xyglo applications
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Sometimes we have performance issues with the time formatting so provide it as optional
        /// </summary>
        /// <param name="message"></param>
        /// <param name="showTime"></param>
        public static void logMsg(string message, bool showTime = false, bool showStack = false)
        {
            //string prefix = "";
            if (showTime)
            {
                Console.Write(DateTimeNowCache.GetDateTime().ToString() + " - ");
            }

            if (showStack)
            {
                StackTrace stackTrace = new StackTrace();
                Console.Write(stackTrace.GetFrame(0) + " - ");
            }

            Console.Write(message + "\n");
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
