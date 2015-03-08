using System;
using System.Collections;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace cointoss.Controllers
{
    public class CryptoHelper
    {
        static byte[] plain;
        static byte[] cipher;

        static string strplain;
        static string strcipher;

        static void Test()
        {        
                string dataString = "Data to Sign";             
                string signedData;
                
                // Hash and sign the data.
                signedData = HashAndSignBytes(dataString);

                // Verify the data and display the result to the 
                // console.
                if (VerifySignedHash(dataString, signedData))
                {
                    Console.WriteLine("The data was verified.");
                }
                else
                {
                    Console.WriteLine("The data does not match the signature.");
                }

        }

        static byte[] GetBytes(string str)
        {
            return Convert.FromBase64String(str);
        }

        static string GetString(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
            
        }
        static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            IStructuralEquatable eqa1 = a1;
            return eqa1.Equals(a2, StructuralComparisons.StructuralEqualityComparer);
        }

        static public string EncodeTo64(string toEncode)
        {
            string urlDecodedVal = HttpUtility.UrlDecode(toEncode);
            byte[] toEncodeAsBytes
                  = System.Text.ASCIIEncoding.ASCII.GetBytes(urlDecodedVal);
            string returnValue
                  = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData)
        {
            byte[] encodedDataAsBytes
                = System.Convert.FromBase64String(encodedData);
            string returnValue =
               System.Text.ASCIIEncoding.ASCII.GetString(encodedDataAsBytes);
            string urlEncodedVal = HttpUtility.UrlEncode(returnValue);
            return urlEncodedVal;
        }

        public static string HashAndSignBytes(string dataString)
        {
            dataString = EncodeTo64(dataString);
            strplain = dataString;

            byte[] DataToSign = GetBytes(dataString);
            CspParameters cspParams = null;
            RSACryptoServiceProvider rsaProvider = null;
            StreamReader publicKeyFile = null;
            string publicKeyText = "";
            string key_root = "C:\\CCP";
                
            cspParams = new CspParameters();
            cspParams.ProviderType = 1;// PROV_RSA_FULL;
            rsaProvider = new RSACryptoServiceProvider(384, cspParams);

            // Read public key from file
            publicKeyFile = File.OpenText(key_root + "\\RSAKeys\\prikey_CaaS.xml");
            publicKeyText = publicKeyFile.ReadToEnd();

            // Import public key
            rsaProvider.FromXmlString(publicKeyText);


            // Hash and sign the data. Pass a new instance of SHA1CryptoServiceProvider
            // to specify the use of SHA1 for hashing.
            byte[] result = rsaProvider.SignData(DataToSign, new SHA1CryptoServiceProvider());

            cipher = result;
            plain = DataToSign;

            strcipher = GetString(result);
            return strcipher;
            
        }

        public static bool VerifySignedHash(string dataString, string SignedData)
        {
            try
            {
                dataString = EncodeTo64(dataString);
                // Create a new instance of RSACryptoServiceProvider using the 
                // key from RSAParameters.
                byte[] DataToVerify = GetBytes(dataString);
                CspParameters cspParams = null;
                RSACryptoServiceProvider rsaProvider = null;
                StreamReader publicKeyFile = null;
                string publicKeyText = "";
                string key_root = "C:\\CCP";

                cspParams = new CspParameters();
                cspParams.ProviderType = 1;// PROV_RSA_FULL;
                rsaProvider = new RSACryptoServiceProvider(384, cspParams);

                // Read public key from file
                publicKeyFile = File.OpenText(key_root + "\\RSAKeys\\pubkey_CaaS.xml");
                publicKeyText = publicKeyFile.ReadToEnd();

                // Import public key
                rsaProvider.FromXmlString(publicKeyText);
               

                // Verify the data using the signature.  Pass a new instance of SHA1CryptoServiceProvider
                // to specify the use of SHA1 for hashing.
                bool result = rsaProvider.VerifyData(DataToVerify, new SHA1CryptoServiceProvider(), GetBytes(SignedData));


                return result;

            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return false;
            }
        }
    }
}