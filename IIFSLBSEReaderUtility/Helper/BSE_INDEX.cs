using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IIFSLBSEReaderUtility.Helper
{
    public class BSE_INDEX
    {
      static  string downloadPath = ConfigurationManager.AppSettings["SDL_DestinationDirectory"].ToString();
       static string tempFilePath = ConfigurationManager.AppSettings["BackupDirectory"].ToString();
        public static void DownloadIndexFileFromURL()
        {
            // Set up Chrome options to handle file downloads
            ChromeOptions options = new ChromeOptions();
            
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("disable-popup-blocking", "true");
            string filePath = Path.Combine(downloadPath, "Index.csv");
            string bkppath = Path.Combine(tempFilePath, "Index.csv");
            if (File.Exists(bkppath))
            {
                DateTime lastWriteTime = File.GetLastWriteTime(bkppath);
                if (lastWriteTime.Date == DateTime.Today)
                {
                    Console.WriteLine("Today's file already exists .");
                    return;
                }
            }
            using (IWebDriver driver = new ChromeDriver(options))
            {
                try
                {
                    File.Delete(bkppath);
                    driver.Navigate().GoToUrl("https://www.bseindia.com/sensex/IndexHighlight.html");

                    // Wait for the icon to be present
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    Thread.Sleep(10000);
                    // Find the icon and click it
                    IWebElement downloadIcon = driver.FindElement(By.CssSelector("i.fa.fa-download.iconfont"));
                    downloadIcon.Click();
                   
                    bool isFileDownloaded = WaitForFile(filePath, TimeSpan.FromSeconds(30));
                    
                    if (isFileDownloaded)
                    {
                        File.Copy(filePath, tempFilePath +Path.GetFileName(filePath), true);
                        Console.WriteLine("File downloaded successfully.");
                    }
                    // Wait for the download to complete (you may need to adjust this)
                    //System.Threading.Thread.Sleep(5000);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    CommonHelper.LogError($"DownloadIndexFileFromURL() :An error occurred: {e.Message}");
                }
                finally
                {
                    driver.Quit();
                }
            }
        }

        private static bool WaitForFile(string filePath, TimeSpan timeout)
        {
            DateTime endTime = DateTime.Now.Add(timeout);
            while (DateTime.Now < endTime)
            {
                if (File.Exists(filePath))
                {
                    return true;
                }
                Thread.Sleep(500); 
            }
            return false;
        }
        public static void DeleteFileIfIndexCsvExists()
        {
            // Get the download directory path from configuration
            string downloadPath = ConfigurationManager.AppSettings["SDL_DestinationDirectory"].ToString();

            try
            {
                // Check if the directory exists
                if (Directory.Exists(downloadPath))
                {
                    // Get all files in the directory
                    string[] files = Directory.GetFiles(downloadPath);

                    // Check if any file with the name "Index.csv" exists
                    if (Array.Exists(files, file => Path.GetFileName(file).Equals("Index.csv", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Delete the target file if it exists
                        string targetFile = Path.Combine(downloadPath, "Index.csv"); // Replace "your_file_name.ext" with the target file name
                        if (File.Exists(targetFile))
                        {
                            File.Delete(targetFile);
                            Console.WriteLine("File deleted successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Target file does not exist.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No file with the name 'Index.csv' found in the directory.");
                    }
                }
                else
                {
                    Console.WriteLine("Directory does not exist.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }

        }
    }
}