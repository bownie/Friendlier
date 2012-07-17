using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Xyglo
{
    /// <summary>
    /// Public client details 
    /// </summary>
    public struct ClientRegistrationDetails
    {
        public string appName;
        public string appVersion;
        public string fromDate;
        public string toDate;
    }

    /// <summary>
    /// Decrypt a client string in the registry
    /// </summary>
    public class ClientDecrypt
    {
        public static ClientRegistrationDetails clientRegistration = new ClientRegistrationDetails();

        public static ClientRegistrationDetails desDecryptString(string Message, string Passphrase)
        {
            byte[] Results;
            System.Text.UTF8Encoding UTF8 = new System.Text.UTF8Encoding();

            // Step 1. We hash the passphrase using MD5
            // We use the MD5 hash generator as the result is a 128 bit byte array
            // which is a valid length for the TripleDES encoder we use below

            using (var HashProvider = MD5CryptoServiceProvider.Create())
            {
                byte[] TDESKey = HashProvider.ComputeHash(UTF8.GetBytes(Passphrase));

                // Step 2. Create a new TripleDESCryptoServiceProvider object
                using (var TDESAlgorithm = TripleDESCryptoServiceProvider.Create())
                {

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
                    string result = UTF8.GetString(Results);

                    clientRegistration.appName = result.Split('|')[0];
                    clientRegistration.appVersion = result.Split('|')[1];
                    clientRegistration.fromDate = result.Split('|')[2];
                    clientRegistration.toDate = result.Split('|')[3];
                }
            }
            return clientRegistration;
        }

        
    }
}
