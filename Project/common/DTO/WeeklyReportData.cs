using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.DTO
{
 public   class WeeklyReportData
    {
        //周报告信息存储，每周清空一次
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
        public string ProjectName { get; set; }
        public string Submitter { get; set; }
        public string BuildResult { get; set; }

    }
}
