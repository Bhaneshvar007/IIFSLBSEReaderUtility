using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32.TaskScheduler;
using Npgsql;
using RuningTaskUtility;

public static class TaskReader
{
    static string connectionString = ConfigurationManager.ConnectionStrings["dbCon"].ToString();

    public static void ExecuteUtility()
    {
        int startHour = int.Parse(ConfigurationManager.AppSettings["StartHour"]);
        int endHour = int.Parse(ConfigurationManager.AppSettings["EndHour"]);

        var currentTime = DateTime.Now;

        if (currentTime.Hour >= startHour && currentTime.Hour < endHour)
        {
            try
            {
                Console.WriteLine($"Executing utility at {currentTime}");
                HelperClass.WriteLog($"Executing utility at {currentTime}", "taskerrorlog");

                // Get the scheduled tasks
                var scheduledTasks = GetAllScheduledTasks();

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                   SaveAllTasks(connection, scheduledTasks);
                    SaveScheduledTasks(connection, scheduledTasks);
                }

                Console.WriteLine("All scheduled tasks saved to the database successfully.");
                HelperClass.WriteLog("All scheduled tasks saved to the database successfully.", "taskerrorlog");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during utility execution: {ex.Message}");
                HelperClass.WriteLog($"Error during utility execution: {ex.Message}", "taskerrorlog");
            }
            finally
            {
                Console.WriteLine("Utility Completed");
                HelperClass.WriteLog("Utility Completed", "taskerrorlog");
            }
        }
        else
        {
            Console.WriteLine($"Skipping execution at {currentTime}, outside of operational hours.");
            HelperClass.WriteLog($"Skipping execution at {currentTime}, outside of operational hours.", "taskerrorlog");
        }
    }

    public static List<ScheduledTaskModel> GetAllScheduledTasks()
    {
        var taskList = new List<ScheduledTaskModel>();
        var excludedFolders = new string[] { "Microsoft", "GoogleSystem", "OfficeSoftwareProtectionPlatform", "Mozilla" };

        using (TaskService ts = new TaskService())
        {
            foreach (Microsoft.Win32.TaskScheduler.Task task in ts.AllTasks)
            {
                string folderName = task.Path;
                string taskid = task.Definition.RegistrationInfo.Source;
             
                    var taskInfo = new ScheduledTaskModel
                    {
                        Name = task.Name,
                        Status = task.State.ToString(),
                        NextRunTime = task.NextRunTime,
                        LastRunTime = task.LastRunTime,
                        taskid= taskid,
                        LastTaskResult = task.LastTaskResult.ToString(),
                        Author = task.Definition.RegistrationInfo.Author
                    };
                    taskList.Add(taskInfo);
                }
                    }
       
        return taskList;
    }

    public static void SaveAllTasks(NpgsqlConnection connection, List<ScheduledTaskModel> taskList)
    {
       
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                foreach (var task in taskList)
                {
                    DataTable dt = new DataTable();
                    using (NpgsqlCommand cmd = new NpgsqlCommand("select * from  tbl_main_alltask where taskname='" + task.Name + "'", connection))
                    {
                        using (NpgsqlDataAdapter adp = new NpgsqlDataAdapter(cmd))
                        {
                            adp.Fill(dt);

                        }
                    }
                    if (dt.Rows.Count < 1)
                    {
                        string insertCommandText = "insert into tbl_main_alltask(taskid,taskname) values('" + task.taskid + "','" + task.Name + "')";
                        using (NpgsqlCommand command = new NpgsqlCommand(insertCommandText, connection, transaction))
                          
                        {
                            try
                            {
                                if (connection.State != ConnectionState.Open)
                                {
                                    connection.Open();
                                    Console.WriteLine("Reconnected to Database server.");
                                }
                                command.ExecuteNonQuery();
                                HelperClass.WriteLog(insertCommandText, "taskerrorlog");
                                
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(":Failed, Error inserting records in  :" + ex.Message);
                                HelperClass.WriteLog(": Failed, Error inserting records in  :" + ex.Message, "taskerrorlog");
                              
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(":Records already present. No new Inserts  :" +task.Name);
                       }
                }
              
                transaction.Commit();
              /*  HelperClass.WriteLog("total records insert" + dt.Rows.Count, "taskerrorlog");*/
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving scheduled tasks: {ex.Message}");
                HelperClass.WriteLog($"Error saving scheduled tasks: {ex.Message}", "taskerrorlog");

                transaction.Rollback();
            }
        }

    }



    public static void SaveScheduledTasks(NpgsqlConnection connection, List<ScheduledTaskModel> taskList)
    {
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                var updateCommandText = @"
            UPDATE tbl_scheduledtasks 
            SET is_active = false 
            WHERE is_active = true;";

                var updateCommand = new NpgsqlCommand(updateCommandText, connection, transaction);
                updateCommand.ExecuteNonQuery();

                var insertCommandText = @"
            INSERT INTO tbl_scheduledtasks 
                (taskname, status, nextruntime, lastruntime, lasttaskresult, author, is_active) 
            VALUES 
                (@taskname, @status, @nextruntime, @lastruntime, @lasttaskresult, @author, true);";

                foreach (var task in taskList)
                {
                    DataTable dt = new DataTable();
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM tbl_main_alltask WHERE taskname = @taskname AND is_run IS NULL", connection))
                    {
                        
                        cmd.Parameters.AddWithValue("@taskname", task.Name);
                        using (NpgsqlDataAdapter adp = new NpgsqlDataAdapter(cmd))
                        {
                            adp.Fill(dt);
                        }
                    }

                    if (dt.Rows.Count > 0)
                    {
                       
                        using (NpgsqlCommand command = new NpgsqlCommand(insertCommandText, connection, transaction))

                        {
                            try
                            {
                                if (connection.State != ConnectionState.Open)
                                {
                                    connection.Open();
                                    Console.WriteLine("Reconnected to Database server.");
                                }
                                command.Parameters.AddWithValue("taskname", task.Name != null ? task.Name : (object)DBNull.Value);
                                command.Parameters.AddWithValue("status", task.Status);
                                command.Parameters.AddWithValue("nextruntime", task.NextRunTime.HasValue ? (object)task.NextRunTime.Value : DBNull.Value);
                                command.Parameters.AddWithValue("lastruntime", task.LastRunTime.HasValue ? (object)task.LastRunTime.Value : DBNull.Value);
                                command.Parameters.AddWithValue("lasttaskresult", task.LastTaskResult != null ? task.LastTaskResult : (object)DBNull.Value);
                                command.Parameters.AddWithValue("author", task.Author != null ? task.Author : (object)DBNull.Value);

                                command.ExecuteNonQuery();
                                HelperClass.WriteLog(insertCommandText, "taskerrorlog");

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(":Failed, Error inserting records in  :" + ex.Message);
                                HelperClass.WriteLog(": Failed, Error inserting records in  :" + ex.Message, "taskerrorlog");

                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine(":not valid task for inserting  :");
                    }
                }
                transaction.Commit();
             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving scheduled tasks: {ex.Message}");
                HelperClass.WriteLog($"Error saving scheduled tasks: {ex.Message}", "taskerrorlog");

                transaction.Rollback();
            }
        }
    }

  }

