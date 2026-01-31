using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Converters;
using WinSCP;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace IIFSLBSEReaderUtility.Helper
{
    internal class SFTPDownload
    {

        private static string Host = ConfigurationManager.AppSettings["Host"].ToString();
        private static int Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"].ToString());
        private static string UserName = ConfigurationManager.AppSettings["UserName"].ToString();
        private static string Password = ConfigurationManager.AppSettings["Password"].ToString();
        private static string SFTP_Source_Path = ConfigurationManager.AppSettings["SFTP_Source_Path"].ToString();
        private static string DestinationDirectory = ConfigurationManager.AppSettings["DestinationDirectory"].ToString();
        private static string DownloadedExcelFilePath = ConfigurationManager.AppSettings["DownloadedExcelFilePath"].ToString();
        private static string MpowerPath = ConfigurationManager.AppSettings["MpowerPath"].ToString();
        private static string BackupDirectory = ConfigurationManager.AppSettings["BackupDirectory"].ToString();
        static string _folderNameSuffix = ConfigurationManager.AppSettings["FileSuffixes"].ToString();
        private static string SDL_DestinationDirectory = ConfigurationManager.AppSettings["SDL_DestinationDirectory"].ToString();
        static string[] folderNameSuffix = _folderNameSuffix.Split(',');
        static string fileDate = DateTime.Now.ToString("yyyyMMdd");

        public static void DownloadFilesFromSFTP()
        {
            string remoteDirectory = "/utiamc/" + DateTime.Now.ToString("ddMMyyyy");

            string fileSuffixes = ConfigurationManager.AppSettings["FileSuffixes"];
            if (string.IsNullOrEmpty(fileSuffixes))
            {
                Console.WriteLine("No file suffixes found in app.config");
                return;
            }

            var suffixes = fileSuffixes.Split(',').Select(s => s.Trim()).ToList();

            // Setup session options
            SessionOptions sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = Host,
                UserName = UserName,
                Password = Password,
                PortNumber = Port,
                //GiveUpSecurityAndAcceptAnySshHostKey = true // This should be changed in a production environment
            };

            using (Session session = new Session())
            {
                try
                {
                    // Connect
                    session.Open(sessionOptions);
                    Console.WriteLine("Connected to SFTP server");

                    // Get list of files in the directory
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(remoteDirectory);
                    Console.WriteLine("directoryInfo :" + directoryInfo);
                    if (directoryInfo == null)
                    {
                        Console.WriteLine("No Directory available on SFTP");
                        return;
                    }
                    Console.WriteLine("SFTP server File count: " + directoryInfo.Files.Count());
                    foreach (RemoteFileInfo file in directoryInfo.Files)
                    {
                        if (!file.IsDirectory)
                        {
                            string remoteFileName = file.Name;

                            string backupFilePath = Path.Combine(BackupDirectory, remoteFileName);
                            if (File.Exists(backupFilePath))
                            {
                                Console.WriteLine("File is already download and available in backup folder :" + remoteFileName);
                                return;
                            }

                            string remoteFilePath = remoteDirectory + "/" + remoteFileName; // Add '/' separator if necessary
                            //Console.WriteLine("SFTP server File Name : " + remoteFileName);

                            // Check if the file name ends with any of the specified suffixes
                            if (suffixes.Any(suffix => remoteFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                            {
                                string localFilePath = Path.Combine(DestinationDirectory, remoteFileName);
                                session.GetFiles(remoteFilePath, localFilePath).Check();
                                Console.WriteLine($"Downloaded File >>>>>>> : {remoteFileName}");

                                CommonHelper.LogError($"SDC Downloaded File >>>>>>> : {remoteFileName}");

                            }
                            if (remoteFileName.EndsWith(".SDL", StringComparison.OrdinalIgnoreCase))
                            {
                                string localFilePath = Path.Combine(SDL_DestinationDirectory, remoteFileName);

                                session.GetFiles(remoteFilePath, localFilePath).Check();
                                Console.WriteLine($"SDL Downloaded File >>>>>>> : {remoteFileName}");

                                CommonHelper.LogError($"SDL Downloaded File >>>>>>> : {remoteFileName}");
                            }

                        }
                    }

                    Console.WriteLine("Disconnected from SFTP server");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An error occurred: {e.Message}");
                    CommonHelper.LogError($"DownloadFilesFromSFTP() :An error occurred: {e.Message}");
                }
            }
        }

        public static bool ConvertDatatableToExcel(DataTable dt, string fileName)
        {
            try
            {

                string filePath = Path.Combine(DownloadedExcelFilePath, fileName);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (ExcelPackage pck = new ExcelPackage())
                {
                    ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Sheet1");

                    ws.Cells["A1"].LoadFromDataTable(dt, true);

                    FileInfo fi = new FileInfo(filePath);
                    pck.SaveAs(fi);
                    return true;
                }
            }
            catch (Exception ex)
            {
                CommonHelper.LogError($"Error in ConvertDatatableToExcel(): {ex.Message}");
                return false;
            }
        }

        public static DataTable ReadSDC(string filePath, string delimiter, int startIndex)
        {
            Console.WriteLine("Reading the file");
            DataTable dt = new DataTable();
            try
            {
                string[] columns = null;

                var lines = File.ReadAllLines(filePath);
                lines = lines.Take(lines.Count() - 1).ToArray();

                if (lines.Count() > 0)
                {
                    columns = lines[startIndex].Split(new string[] { delimiter }, StringSplitOptions.None);

                    foreach (var column in columns)
                        if (column.Length > 1)
                            dt.Columns.Add(column.Replace("\"", ""));
                }

                for (int i = startIndex + 1; i < lines.Count(); i++)
                {
                    DataRow dr = dt.NewRow();
                    string[] values = lines[i].Split(new string[] { delimiter }, StringSplitOptions.None);

                    for (int j = 0; j < values.Count() && j < columns.Count(); j++)
                    {
                        dr[j] = values[j].Replace("\"", "");
                    }
                    dt.Rows.Add(dr);
                }
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    CommonHelper.LogError("error while deleting file in ReadSDC() :" + ex.Message);
                }
            }
            catch (Exception ex)
            {
                CommonHelper.LogError("ReadSDC: Unable to read SDC file :" + Path.GetFileName(filePath) + ":  " + ex.ToString());
            }
            return dt;
        }
        public static void ReadSDCFile()
        {

            try
            {
                Console.WriteLine("Converting SDC To Excel Start.");
                Console.WriteLine("Moved SDC File path :" + DestinationDirectory);
                if (Directory.Exists(DestinationDirectory))
                {
                    FileInfo[] DownloadedFiles = new DirectoryInfo(DestinationDirectory).GetFiles();

                    CommonHelper.LogError($"Total file count on Destination Directory :{DownloadedFiles.Length}");


                    Console.WriteLine("Total file count :" + DownloadedFiles.Length);
                    CommonHelper.LogError("File Count: " + DownloadedFiles.Length);

                    for (int i = 0; i < DownloadedFiles.Length; i++)
                    {
                        DataTable dtReadSDC = ReadSDC(DownloadedFiles[i].FullName, "\t", 0);
                        if (dtReadSDC.Rows.Count > 0)
                        {
                            var FileExtension = Path.GetExtension(DownloadedFiles[i].FullName);

                            var FileName = Path.GetFileName(DownloadedFiles[i].FullName);

                            var newFileName = FileName.Replace(".SDC", ".xlsx");


                            if (ConvertDatatableToExcel(dtReadSDC, newFileName))
                            {
                                CopyExcelFileToMpower(newFileName);
                            }

                            CommonHelper.LogError("SDC File Converted to Excel End: " + DownloadedFiles[i].Name);
                        }
                        else
                        {
                            CommonHelper.LogError("Records not found this file: " + DownloadedFiles[i].Name);
                        }
                    }
                }
                else
                {
                    CommonHelper.LogError("Directory not found: ");
                }
            }
            catch (Exception ex)
            {
                CommonHelper.LogError("Failed a File Reading: " + ex.ToString());
            }
        }

        public static void CopyExcelFileToMpower(string newFileName)
        {
            try
            {
                string sourceFilePath = Path.Combine(DownloadedExcelFilePath, newFileName);
                string backupFilePath = Path.Combine(BackupDirectory, newFileName);
                string mpowerFilePath = Path.Combine(MpowerPath, newFileName);

               // CommonHelper.LogError($"Copying excel file from {sourceFilePath} to {mpowerFilePath} and {BackupDirectory} start");

                if (!File.Exists(backupFilePath) && Path.GetExtension(backupFilePath) == ".xlsx")
                {
                    File.Copy(sourceFilePath, mpowerFilePath, true);
                    CommonHelper.LogError($"Copied excel file to Mpower directory: {newFileName}");
                }
                if (!File.Exists(backupFilePath))
                {
                    File.Copy(sourceFilePath, backupFilePath, true);
                    CommonHelper.LogError($"Copied excel file to backup directory: {newFileName}");
                }
                File.Delete(sourceFilePath);
            }
            catch (Exception ex)
            {
                CommonHelper.LogError("An error occurred in CopyExcelFileToMpower(): " + ex.Message);
            }

        }
    }
}