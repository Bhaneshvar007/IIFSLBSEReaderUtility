using IIFSLBSEReaderUtility.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIFSLBSEReaderUtility
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CommonHelper.LogError("Utility Started");
          
            BSE_Indexread.DownloadIndexFileFromURL();
            CommonHelper.LogError("Utility Completed");

           // Environment.Exit(1);

           
           
        }
    }
}
