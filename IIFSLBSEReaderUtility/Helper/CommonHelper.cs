using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIFSLBSEReaderUtility.Helper
{
    internal class CommonHelper
    {
        public static void LogError(string message)
        {
            string FileName = "BSE_INDEX_WTS_Logs";
            string ErrorLogFile = ConfigurationManager.AppSettings["ErrorLogFile"];
            if (!Directory.Exists(ErrorLogFile))
                Directory.CreateDirectory(ErrorLogFile);

            ErrorLogFile += "\\" + FileName + "ErrorLog" + DateTime.Now.ToString("dd-MMM-yy") + ".txt";
            using (StreamWriter sw = new StreamWriter(ErrorLogFile, true))
            {
                sw.WriteLine(DateTime.Now.ToString("dd-MMM-yy HH:mm:ss") + "\t" + message);
            }
        }
    }
}
