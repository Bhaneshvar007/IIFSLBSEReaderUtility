using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace midcap_IIFsl_utility
{
    internal class Program
    {
        static void Main(string[] args)
        {

            /* CommonHelper.LogError("Utility Started");
             BSE_Indexread.DownloadIndexFileFromURL();
             CommonHelper.LogError("Utility Completed");*/
            using (IWebDriver driver = new ChromeDriver())
            {
                // Navigate to the URL
                driver.Navigate().GoToUrl("https://www.bseindia.com/sensex/IndexHighlight.html");

                // Perform any additional actions (if needed)

                // Close the browser
               // driver.Quit();
            }
        }
    }
}
