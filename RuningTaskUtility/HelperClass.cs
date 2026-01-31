using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuningTaskUtility
{
    public class HelperClass
    {
        public static void WriteLog(string message, string FileName)
        {
            string ErrorLogDir = ConfigurationManager.AppSettings["ErrorLogFile"].ToString();

            if (!Directory.Exists(ErrorLogDir))
                Directory.CreateDirectory(ErrorLogDir);
            using (StreamWriter sw = new StreamWriter(Path.Combine(ErrorLogDir, FileName + "ErrorLog" + DateTime.Now.ToString("dd-MMM-yy") + ".txt"), true))
            {
                sw.WriteLine(DateTime.Now.ToString("dd-MMM-yy HH:mm:ss") + "\t" + message);
            }
        }
    }
}
