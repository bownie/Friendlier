using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography; 
using System.Windows.Forms;
using System.Net;

namespace Xyglo 
{
    public enum EncryptionMethod
    {
        RijndaelManaged,
        AES,
        DES,
        Blowfish
    }

    /// <summary>
    /// Struct holding licence details
    /// </summary>
    public struct LicenceDetails
    {
        public string appName;
        public string appVersion;
        public string validFromDate;
        public string validToDate;
        public int highValue;
    }


    public class LicenceManager
    {
        //protected string m_generateFilePath = @"C:\temp\licences.txt";

        //protected string m_loadFilePath = @"C:\temp\licences.txt";

        public string m_appName { get; set; }

        public string m_appVersion { get; set; }

        public int m_sequenceNumber { get; set; }

        public string m_password { get; set; }

        public bool m_isValidFromDate = false;

        public DateTime m_validFromDate { get; set; }

        public bool m_isValidToDate = false;

        public DateTime m_validToDate { get; set; }

        public EncryptionMethod m_encryptionMethod { get; set; }

        /// <summary>
        /// Return struct for possible licence details
        /// </summary>
        public LicenceDetails m_possibleLicenceDetails = new LicenceDetails();

        /// <summary>
        /// We cheat and get a handle on this from the main window to avoid sending things around
        /// </summary>
        public ToolStripProgressBar m_progressBar { get; set; }

        public LicenceManager()
        {
            testLoad();
        }

        protected void testLoad()
        {
            m_appName = "Friendlier";
            m_appVersion = "1.0";
            m_sequenceNumber = 1;
            m_password = "MYPASS";
        }

        /// <summary>
        /// Copy the possible values to the current ones
        /// </summary>
        public void setPossibleValues()
        {
            m_appName = m_possibleLicenceDetails.appName;
            m_appVersion = m_possibleLicenceDetails.appVersion;

            try
            {
                if (m_possibleLicenceDetails.validFromDate != "")
                {
                    m_validFromDate = Convert.ToDateTime(m_possibleLicenceDetails.validFromDate);
                    m_isValidFromDate = true;
                }
                else
                {
                    m_isValidFromDate = false;
                }

                if (m_possibleLicenceDetails.validToDate != "")
                {
                    m_validToDate = Convert.ToDateTime(m_possibleLicenceDetails.validToDate);
                    m_isValidToDate = true;
                }
                else
                {
                    m_isValidToDate = false;
                }

                m_sequenceNumber = Convert.ToInt32(m_possibleLicenceDetails.highValue) + 1;
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn't convert whilst copying possible values");
            }
        }



        /// <summary>
        /// Generate a number of licences
        /// </summary>
        /// <param name="number"></param>
        /// <param name="outputFilePath"></param>
        public void generateLicences(int number, string outputFilePath)
        {
            // Set progress bar to zero
            //
            m_progressBar.Value = 0;

            if (m_appName == "" || m_appVersion == "" || m_password == "")
            {
                throw new Exception("Please supply app name, app version and password");
            }

            // Compile app details
            //
            string plainPrefix  = m_appName + "|" + m_appVersion + "|";

            // Add dates as necessary
            //
            if (m_isValidFromDate)
            {
                plainPrefix += m_validFromDate;
            }

            plainPrefix += "|";

            if (m_isValidToDate)
            {
                plainPrefix += m_validToDate;
            }


            string uniqueNowID = "";
            string plainText = "";
            string encryptText = "";

            string host = Dns.GetHostName();
            IPHostEntry ip = Dns.GetHostEntry(host);
           
            // Write file out with append
            //
            using (StreamWriter outfile = new StreamWriter(outputFilePath, true))
            {
                uniqueNowID = System.Windows.Forms.SystemInformation.UserName + "|" + ip.AddressList[0].ToString() + "|" + DateTime.Now.ToShortDateString() + "|" + DateTime.Now.ToLongTimeString();

                // Now generate the final 
                for (int i = m_sequenceNumber; i < m_sequenceNumber + number; i++)
                {
                    plainText = plainPrefix + "|" + i.ToString().PadLeft(9, '0') + "|" + uniqueNowID;

             //       Console.WriteLine("PLAIN TEXT = " + plainText);

                    switch (m_encryptionMethod)
                    {
                        case Xyglo.EncryptionMethod.RijndaelManaged:
                            encryptText = encryptRijndaelManaged(plainText);
                            break;

                        case Xyglo.EncryptionMethod.DES:
                            encryptText = desEncryptString(plainText, m_password);
                            break;

                        default:
                            break;
                    }

                    // Set the progress bar
                    //
                    m_progressBar.Value = (int)(((double)i - m_sequenceNumber)/((double)(number)) * 100.0f);

                    //Console.WriteLine("ENCRYPTED = " + encryptText);
                    outfile.WriteLine(encryptText);
                }
            }

            // Increment the sequence number so we start next time with a new one
            //
            m_sequenceNumber += number;

            // Complete the progress bar in case it's not happened yet
            //
            m_progressBar.Value = 100;
        }

        /// <summary>
        /// Parse a keystring
        /// </summary>
        /// <param name="keyString"></param>
        protected void parseKeyString(string keyString)
        {
            string[] split = keyString.Split('|');

            m_possibleLicenceDetails.appName = split[0];
            m_possibleLicenceDetails.appVersion = split[1];
            m_possibleLicenceDetails.validFromDate = split[2];
            m_possibleLicenceDetails.validToDate = split[3];
            m_possibleLicenceDetails.highValue = Convert.ToInt32(split[4]);
        }


        /// <summary>
        /// Load keys from a licence file and work out what the highest sequence is
        /// </summary>
        /// <param name="keyFile"></param>
        /// <returns></returns>
        public int fetchHighestSequence(string keyFile)
        {
            // Set progress bar to zero
            //
            m_progressBar.Value = 0;

            string plainText = "";
            string encryptText = "";
            int lineCount = File.ReadAllLines(keyFile).Length;
            int i = 0;
            int highValue = -1;
            int sequenceId = -1;

            using (StreamReader sr = new StreamReader(keyFile))
            {
                while((encryptText = sr.ReadLine()) != null)
                {
                    //Console.WriteLine("ENCRYPTED = " + encryptText);

                    switch (m_encryptionMethod)
                    {
                        case Xyglo.EncryptionMethod.RijndaelManaged:
                            plainText = decryptRijndaelManaged(encryptText);
                            break;

                        case Xyglo.EncryptionMethod.DES:
                            plainText = desDecryptString(encryptText, m_password);
                            break;

                        default:
                            break;
                    }

                    try
                    {
                        sequenceId = Convert.ToInt32(plainText.Split('|')[4]);

                        if (sequenceId > highValue)
                        {
                            highValue = sequenceId;
                            parseKeyString(plainText);
                        }
                    }
                    catch (Exception /* e */)
                    {
                        throw new Exception("Got bad decrypt on line " + i);
                    }

                    //Console.WriteLine("PLAIN = " + plainText);

                    m_progressBar.Value = (int)(((double)i) / ((double)(lineCount)) * 100.0f);
                }
            }

            // Full progress
            //
            m_progressBar.Value = 100;

            return lineCount;
        }


        /// <summary>
        /// Encrypt rijndael managed
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        protected string encryptRijndaelManaged(string original)
        {
            string rS = "";

            try
            {
                // Create a new instance of the RijndaelManaged
                // class.  This generates a new key and initialization 
                // vector (IV).
                using (RijndaelManaged myRijndael = new RijndaelManaged())
                {
                    // Encrypt the string to an array of bytes.
                    string encrypted = EncryptStringToBytes(original, myRijndael.Key, myRijndael.IV);

                    // Decrypt the bytes to a string.
                    string roundtrip = DecryptStringFromBytes(encrypted, myRijndael.Key, myRijndael.IV);

                    //Display the original data and the decrypted data.
                    Console.WriteLine("Original:   {0}", original);
                    Console.WriteLine("Round Trip: {0}", roundtrip);

                    rS = encrypted;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }

            return rS;
        }

        protected string decryptRijndaelManaged(string cryptString)
        {
            string rS = "";

            try
            {
                // Create a new instance of the RijndaelManaged
                // class.  This generates a new key and initialization 
                // vector (IV).
                using (RijndaelManaged myRijndael = new RijndaelManaged())
                {
                    // Decrypt the bytes to a string.
                    string decrypted = DecryptStringFromBytes(cryptString, myRijndael.Key, myRijndael.IV);

                    //Display the original data and the decrypted data.
                    Console.WriteLine("Original:   {0}", cryptString);
                    Console.WriteLine("Decrypted:  {0}", decrypted);

                    rS = decrypted;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }

            return rS;
        }

        static string EncryptStringToBytes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return Convert.ToBase64String(encrypted);
        }

        static string DecryptStringFromBytes(string cryptMessage, byte[] Key, byte[] IV)
        {
            byte[] cipherText = Convert.FromBase64String(cryptMessage);

            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an RijndaelManaged object
            // with the specified key and IV.
            using (RijndaelManaged rijAlg = new RijndaelManaged())
            {
                rijAlg.Key = Key;
                rijAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;

        }

        //------------------------------------------------------------------------------------
        // Courtesy of:
        //
        // http://www.dijksterhuis.org/encrypting-decrypting-string/
        //
        //

        public static string desEncryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the encoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToEncrypt = UTF8.GetBytes(Message);

            // Step 5. Attempt to encrypt the string
            try
            {
                ICryptoTransform Encryptor = TDESAlgorithm.CreateEncryptor();
                Results = Encryptor.TransformFinalBlock(DataToEncrypt, 0, DataToEncrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the encrypted string as a base64 encoded string
            return Convert.ToBase64String(Results);
        }

        public static string desDecryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            MD5CryptoServiceProvider HashProvider = new MD5CryptoServiceProvider();
            byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

            // Step 2. Create a new TripleDESCryptoServiceProvider object
            TripleDESCryptoServiceProvider TDESAlgorithm = new TripleDESCryptoServiceProvider();

            // Step 3. Setup the decoder
            TDESAlgorithm.Key = TDESKey;
            TDESAlgorithm.Mode = CipherMode.ECB;
            TDESAlgorithm.Padding = PaddingMode.PKCS7;

            // Step 4. Convert the input string to a byte[]
            byte[] DataToDecrypt = Convert.FromBase64String(Message);

            // Step 5. Attempt to decrypt the string
            try
            {
                ICryptoTransform Decryptor = TDESAlgorithm.CreateDecryptor();
                Results = Decryptor.TransformFinalBlock(DataToDecrypt, 0, DataToDecrypt.Length);
            }
            finally
            {
                // Clear the TripleDes and Hashprovider services of any sensitive information
                TDESAlgorithm.Clear();
                HashProvider.Clear();
            }

            // Step 6. Return the decrypted string in UTF8 format
            return UTF8.GetString(Results);
        }
    }
}
