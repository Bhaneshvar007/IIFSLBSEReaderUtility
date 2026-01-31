using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;

namespace IIFSLBSEReaderUtility.Helper
{
    public class BSE_Indexread
    {
      
            static string downloadPath = ConfigurationManager.AppSettings["SDL_DestinationDirectory"].ToString();
            static string tempFilePath = ConfigurationManager.AppSettings["BackupDirectory"].ToString();
        // static string chromeDriverPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
            static string DriveFiles = ConfigurationManager.AppSettings["DriveFile"].ToString();

        //static string chromeDriverPath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
        static string MicroDriverPath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

        public static void DownloadIndexFileFromURL()
        {
            TimeSpan runTime = new TimeSpan(10, 30, 0);
            if (DateTime.Now.TimeOfDay >= runTime)
            {
                // Set up Chrome options to handle file downloads
                //ChromeOptions options = new ChromeOptions();
                //options.BinaryLocation = chromeDriverPath;
                //options.AddUserProfilePreference("download.default_directory", downloadPath);

                //options.AddUserProfilePreference("download.prompt_for_download", false);
                //options.AddUserProfilePreference("disable-popup-blocking", "true");
                
                var options = new EdgeOptions();
                options.BinaryLocation = MicroDriverPath;

                options.PageLoadStrategy = PageLoadStrategy.Normal;




                options.AddUserProfilePreference("download.default_directory", downloadPath);
                options.AddUserProfilePreference("download.prompt_for_download", false);
                options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
                options.AddUserProfilePreference("safebrowsing.enabled", true);
                options.AddUserProfilePreference("download.directory_upgrade", true);
                options.AddUserProfilePreference("profile.default_content_setting_values.automatic_downloads", 1);
                options.AddArgument("--start-maximized");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--safebrowsing-disable-download-protection");

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
                Console.WriteLine("Anshu");
                try
                {

                    //using (IWebDriver driver = new ChromeDriver(options))
                    // Abhay 13-11-2025
                    

                    using (IWebDriver driver = new EdgeDriver(DriveFiles, options))
                    {
                        
                        try
                        {
                            File.Delete(bkppath);
                            Console.WriteLine("After try");
                            driver.Navigate().GoToUrl("https://www.bseindia.com/sensex/IndexHighlight.html");
                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                            ;
                            wait.Until(d => d.FindElements(By.XPath("//table[@id='tblinsidertrd']")).Count >= 2);


                            //IWebElement table = driver.FindElement(By.XPath("//table[@id='tblinsidertrd']"));
                            IList<IWebElement> tables =
                                 driver.FindElements(By.XPath("//table[@id='tblinsidertrd']"));

                            IWebElement table = tables[1];

                            DataTable dataTable = new DataTable();
                            /* dataTable.Columns.Add("Index");
                             dataTable.Columns.Add(" Open");
                             dataTable.Columns.Add(" High");
                             dataTable.Columns.Add(" Low");
                             dataTable.Columns.Add(" Current Value");
                             dataTable.Columns.Add(" Prev. Close");
                             dataTable.Columns.Add(" Ch (pts)");
                             dataTable.Columns.Add(" Ch (%)");
                             dataTable.Columns.Add(" 52 Wk High");
                             dataTable.Columns.Add(" 52 WK Low");
                             dataTable.Columns.Add(" Turnover (Rs. Cr)");
                             dataTable.Columns.Add(" % in Total Turnover");

                            */

                            dataTable.Columns.Add("Index");
                            dataTable.Columns.Add(" Current Value");
                            dataTable.Columns.Add(" prev. close");
                            dataTable.Columns.Add(" Ch (pts)");
                            dataTable.Columns.Add(" Ch (%)");
                            dataTable.Columns.Add(" Date");



                            // ===============================
                            // STEP 1: READ ONLY "BSE 500" FROM REAL TIME TABLE (table[0])
                            // ===============================
                            IWebElement rtTable = tables[0];

                            IList<IWebElement> bse500Rows = rtTable.FindElements(
                                By.XPath(".//tbody/tr[td[1][contains(normalize-space(),'BSE 500')]]")
                            );

                            if (bse500Rows.Count > 0)
                            {
                                IList<IWebElement> rtCells = bse500Rows[0].FindElements(By.TagName("td"));

                                if (rtCells.Count >= 8)
                                {
                                    DataRow dr = dataTable.NewRow();
                                    dr["Index"] = rtCells[0].Text;
                                    dr[" Current Value"] = rtCells[4].Text;
                                    dr[" prev. close"] = rtCells[5].Text;
                                    dr[" Ch (pts)"] = rtCells[6].Text;
                                    dr[" Ch (%)"] = rtCells[7].Text;
                                    dr[" Date"] = DateTime.Now.AddDays(-1).ToString("dd-MMM-yyyy");


                                    dataTable.Rows.Add(dr);
                                }
                            }





                            // ===============================
                            // STEP 2: READ All Index FROM END Of Day (table[1])
                            // ===============================

                            //IList<IWebElement> rows = table.FindElements(By.XPath(".//tbody/tr"));

                            IList<IWebElement> rows = table.FindElements(
                                                By.XPath(".//tbody/tr[td and normalize-space()]")
                                            );

                            foreach (IWebElement row in rows)
                            {
                                try
                                {
                                    IList<IWebElement> cells = row.FindElements(By.XPath(".//td"));

                                    if (cells.Count == 6)
                                    {
                                        DataRow dataRow = dataTable.NewRow();
                                        /*  dataRow["Index"] = cells[0].Text;
                                          dataRow[" Open"] = cells[1].Text;
                                          dataRow[" High"] = cells[2].Text;
                                          dataRow[" Low"] = cells[3].Text;
                                          dataRow[" Current Value"] = cells[4].Text;
                                          dataRow[" Prev. Close"] = cells[5].Text;
                                          dataRow[" Ch (pts)"] = cells[6].Text;
                                          dataRow[" Ch (%)"] = cells[7].Text;
                                          dataRow[" 52 Wk High"] = cells[8].Text;
                                          dataRow[" 52 WK Low"] = cells[9].Text;
                                          dataRow[" Turnover (Rs. Cr)"] = cells[10].Text;
                                          dataRow[" % in Total Turnover"] = cells[11].Text;*/


                                        dataRow["Index"] = cells[0].Text;
                                        dataRow[" Current Value"] = cells[1].Text;
                                        dataRow[" Prev. Close"] = cells[2].Text;
                                        dataRow[" Ch (pts)"] = cells[3].Text;
                                        dataRow[" Ch (%)"] = cells[4].Text;
                                        dataRow[" Date"] = cells[5].Text;

                                        dataTable.Rows.Add(dataRow);
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Skipped a row with {cells.Count} cells.");
                                    }
                                }
                                catch (StaleElementReferenceException)
                                {

                                    Console.WriteLine("StaleElementReferenceException caught. Retrying...");

                                }
                            }


                            


                            WriteDataTableToCsv(dataTable, Path.Combine(downloadPath, "index.csv"));
                            bool isFileDownloaded = WaitForFile(filePath, TimeSpan.FromSeconds(30));

                            if (isFileDownloaded)
                            {
                                File.Copy(filePath, tempFilePath + Path.GetFileName(filePath), true);
                                Console.WriteLine("File downloaded successfully.");
                            }
                        }

                        catch (Exception ex)
                        {
                            Console.WriteLine("An error occurred: " + ex.Message);
                            CommonHelper.LogError("An error occurred: " + ex.Message);
                        }
                        finally
                        {
                            driver.Quit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    CommonHelper.LogError(ex.Message);
                }
            }
        }


        static void WriteDataTableToCsv(DataTable dataTable, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
               
                foreach (DataColumn column in dataTable.Columns)
                {
                    writer.Write(column.ColumnName + ",");
                }
                writer.WriteLine();

                foreach (DataRow row in dataTable.Rows)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        writer.Write(row[i].ToString().Replace(",", "") + ",");
                    }
                    writer.WriteLine();
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

    }
    }

