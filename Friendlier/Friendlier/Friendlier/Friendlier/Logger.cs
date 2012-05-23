using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public static void logMsg(string message, bool showTime = false)
        {
            if (showTime)
            {
                //Console.WriteLine(String.Format("{0:s}", DateTime.Now) + " - " + message);
                Console.WriteLine(DateTime.Now.ToString() + " - " + message);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}
