using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace Xyglo
{
    /// <summary>
    /// Deal with registration and licencing for this piece of software
    /// </summary>
    public class Registration
    {
        /// <summary>
        /// Registry path
        /// </summary>
        static private string friendlierString = @"SOFTWARE\Xyglo\Friendlier\CurrentVersion";

        /// <summary>
        /// User email
        /// </summary>
        static private string m_userEmail = "";

        /// <summary>
        /// User organsiation
        /// </summary>
        static private string m_userOrg = "";

        /// <summary>
        /// Product name
        /// </summary>
        static private string m_productName = "";

        /// <summary>
        /// Version of this product
        /// </summary>
        static private string m_productVersion = "";

        /// <summary>
        /// A salt
        /// </summary>
        static private string m_salt = "InTHET0wnwh3r3IwasbornL#VED%AMAnwhoZieLEDDEZE3";

        /// <summary>
        /// Check the registry for an existing licence key and validate it.  If it's not
        /// found then generate a default one.
        /// </summary>
        static public bool checkRegistry()
        {
            //The Registry class provides us with the root keys
            //
            RegistryKey rkey = Registry.LocalMachine;

            // Licence key goes here
            //
            string licenceKey = "";

            //Now using GetValue(...) we read in various values 
            //from the opened key
            try
            {
                //Now let's open one of the sub keys
                //
                RegistryKey rkey1 = rkey.OpenSubKey(friendlierString);

                m_userEmail = rkey1.GetValue("User Email").ToString();
                m_userOrg = rkey1.GetValue("User Organisation").ToString();
                m_productName = rkey1.GetValue("Product Name").ToString();
                m_productVersion = rkey1.GetValue("Product Version").ToString();
                licenceKey = rkey1.GetValue("Licence Key").ToString();

                rkey1.Close();

                Logger.logMsg("Registration::checkRegistry() - user email : " + m_userEmail);
                Logger.logMsg("Registration::checkRegistry() - user org : " + m_userOrg);
                Logger.logMsg("Registration::checkRegistry() - product name : " + m_productName);
                Logger.logMsg("Registration::checkRegistry() - product version : " + m_productVersion);
                Logger.logMsg("Registration::checkRegistry() - reg key : " + licenceKey);
            }
            catch (Exception /* e */)
            {
                Logger.logMsg("Registration::checkRegistry() - couldn't get registry entries");
                generateDefaultRegistryEntries();
                return false;
            }

            // Now check the licence key value 
            //
            return (licenceKey == getLicenceKey());
        }

        /// <summary>
        /// Generate some default entries for this application
        /// </summary>
        static private void generateDefaultRegistryEntries()
        {
            Logger.logMsg("Registration::generateDefaultRegistryEntries() - generating defaults");

            RegistryKey rkey = Registry.LocalMachine;

            

            try
            {
                //RegistryKey writeKey = rkey.OpenSubKey(friendlierString, true);
                RegistryKey writeKey = rkey.CreateSubKey(friendlierString, RegistryKeyPermissionCheck.ReadWriteSubTree);

                writeKey.SetValue("User Email", "unregistered");
                writeKey.SetValue("User Organisation", "unregistered");
                writeKey.SetValue("Product Name", VersionInformation.getProductName());
                writeKey.SetValue("Product Version", VersionInformation.getProductVersion());
                writeKey.SetValue("Licence Key", "XXXX");
                writeKey.Close();
            }
            catch (Exception /* e */)
            {
                Logger.logMsg("Registration::generateDefaultRegistryEntries() - could not generate subkey");
            }
        }

        /// <summary>
        /// Generate a licence key from unique information
        /// </summary>
        static private string getLicenceKey(string org = "", string email = "", string product = "", string version = "")
        {
            // If not set then use the internal version
            //
            if (org == "")
            {
                org = m_userOrg;
            }

            if (email == "")
            {
                email = m_userEmail;
            }

            if (product == "")
            {
                product = m_productName;
            }

            if (version == "")
            {
                version = m_productVersion;
            }

            string majorVersion = version.Substring(0, m_productVersion.IndexOf('.'));

            Logger.logMsg("Registration::checkLicenceKey() - MAJOR VERSION = " + majorVersion);

            // Build the string to convert
            //
            string regCheck = org + email + product + majorVersion + m_salt;
            //Logger.logMsg("INPUT STRING = " + regCheck);

            // Create a byte stream
            //
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            Byte[] bytes = encoding.GetBytes(regCheck);

            // Convert
            //
            string md5String = md5Sum(bytes);

            Logger.logMsg("Registration::checkLicenceKey() - registry key is " + md5String);

            return md5String;
        }


        /// <summary>
        /// Return an MD5 hash of an input 
        /// </summary>
        /// <param name="FileOrText"></param>
        /// <returns></returns>
        static private string md5Sum(byte[] FileOrText, bool strip = true) //Output: String<-> Input: Byte[] //
        {
            string rS = BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(FileOrText)).ToLower();

            if (strip)
            {
                return rS.Replace("-", "");
            }
            else
            {
                return rS;
            }
        }

        /// <summary>
        /// A public interface.  Shh.
        /// </summary>
        /// <param name="org"></param>
        /// <param name="user"></param>
        /// <param name="product"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        static public string generate(string org, string user, string product, string version)
        {
            return getLicenceKey(org, user, product, version);
        }
    }
}
