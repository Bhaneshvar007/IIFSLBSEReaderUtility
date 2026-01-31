using System;
using Microsoft.Win32.TaskScheduler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuningTaskUtility
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TaskReader.ExecuteUtility();
        }
    }
}
