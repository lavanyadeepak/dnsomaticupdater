using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LavanyaDeepak.Networking.Utilities
{
    public class Program
    {
        string strUsername = "deepak.vasudevan@outlook.com";
        string strPassword = "UaV!ybg2";
        string strURL = "https://updates.dnsomatic.com/nic/update";
        string strLastError = string.Empty;
        string strAppFolder = string.Empty;
        string strDataFile = string.Empty;
        UserPassword uP = new UserPassword();

        static void Main(string[] args)
        {
            Program dnsUpdater = new Program();
            dnsUpdater.InitializeEnvironment();

            if (args.Length > 0 && args[0].Contains("/setup"))
            {
                dnsUpdater.CreateDataFile();
            }
            else
            {
                if (!File.Exists(dnsUpdater.strDataFile))
                {
                    Console.WriteLine("Program Setup Incomplete");
                }
                else
                {
                    dnsUpdater.ReadDataFile();
                    dnsUpdater.AuthenticateDns();
                }
            }
        }

        public void CreateDataFile()
        {
            Console.Write("Enter Opendns Username: ");
            uP.UserName =  Console.ReadLine();

            Console.Write("Enter Opendns Password: ");
            uP.Password = SecuredPasswordReader.ReadPassword('*');

            uP.OperatingSystemUsername = WindowsPrincipal.Current.Identity.Name;
            uP.Hostname = GetMACAddress() ;

            using (StreamWriter swDataFile = new StreamWriter(strDataFile))
            {
                swDataFile.Write(Convert.ToBase64String(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(uP, Formatting.Indented))));
                swDataFile.Close();
            }
        }

        public bool ReadDataFile()
        {
            using (StreamReader srDataFile = new StreamReader(strDataFile))
            {
                string strContents = srDataFile.ReadToEnd();
                strContents = Encoding.Unicode.GetString(Convert.FromBase64String(strContents));

                uP = JsonConvert.DeserializeObject<UserPassword>(strContents);

                if (uP.Hostname != GetMACAddress())
                {
                    strLastError = "Data File does not belong to this computer.";
                }    

                if (uP.OperatingSystemUsername != WindowsPrincipal.Current.Identity.Name)
                {
                    strLastError = "Data Files does not belong to the logged on user.";
                }

                return (true);
            }
        }

        public void InitializeEnvironment()
        {
            strAppFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LavanyaDeepak", "DNSOMaticUpdater");
            Directory.CreateDirectory(strAppFolder);

            strDataFile = Path.Combine(strAppFolder, "data.json");
        }

        public bool AuthenticateDns()
        {
            try
            {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(uP.UserName + ":" + uP.Password));

                WebClient wc = new WebClient();
                wc.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                string strMessage = wc.DownloadString(strURL);
                return (strMessage.Contains("good"));
            }
            catch (Exception WebClientException)
            {
                strLastError = WebClientException.Message;
                return (false);
            }
        }

        public string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }
            }
            return sMacAddress;
        }
    }

    public class UserPassword
    {
        public string Hostname { get; set; }
        public string OperatingSystemUsername { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}