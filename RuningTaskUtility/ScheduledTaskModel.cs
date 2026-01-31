using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuningTaskUtility
{
    public class ScheduledTaskModel
    {

        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        public string LastTaskResult { get; set; }
        public string Author { get; set; }
        public string taskid { get; set; }
    }
}
