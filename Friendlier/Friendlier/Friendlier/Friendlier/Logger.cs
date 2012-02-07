using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public static class Logger
    {
        public static void logMsg(string message)
        {
            Console.WriteLine(/* String.Format("{0:s}", DateTime.Now) + " - " + */ message);
        }
    }
}
