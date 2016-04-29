using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckyCIWebAPI.Models
{
    public class LogInfo
    {
        public string CILogId { get; set; }
        public string ProjectName { get; set; }
        public string CommitVersion { get; set; }
        public string Submitter { get; set; }
        public string CIResult { get; set; }
        public string CITimeSpent { get; set; }
        public string CIStartTime { get; set; }
        public string CIEndTime { get; set; }
        public string CILogTime { get; set; }
        public string Branch { get; set; }
    }
}