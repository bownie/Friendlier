using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Microsoft.VisualBasic.ApplicationServices;



namespace Xyglo
{
    /// <summary>
    /// Convenience class for storing some global information
    /// </summary>
    public class VersionInformation // : AssemblyInfo
    {

        /// <summary>
        /// Default constructor
        /// </summary>
        /*
        public VersionInformation(AssemblyInfo ass)
        {
        }*/

        /// <summary>
        /// Version of this software
        /// </summary>
        static private string m_version = "1.0.0 Beta";

        /// <summary>
        /// Email to report bugs at
        /// </summary>
        static private string m_bugEmail = "info@xyglo.com";

        /// <summary>
        /// Product name
        /// </summary>
        static private string m_productName = "Friendlier";

        /// <summary>
        /// Define a drop dead date for this piece of software if unlicenced
        /// </summary>
        static private DateTime m_dropDead = new DateTime(2012, 7, 15);

        /// <summary>
        /// Current version
        /// </summary>
        /// <returns></returns>
        static public string getProductVersion()
        {
            return m_version;
        }

        /// <summary>
        /// Product name
        /// </summary>
        /// <returns></returns>
        static public string getProductName()
        {
            return m_productName;
        }

        /// <summary>
        /// Email address for bug reports
        /// </summary>
        /// <returns></returns>
        static public string getBugReportEmail()
        {
            return m_bugEmail;
        }

        /// <summary>
        /// Drop dead time for unlicenced software
        /// </summary>
        /// <returns></returns>
        static public DateTime getDropDead()
        {
            return m_dropDead;
        }
    }
}
