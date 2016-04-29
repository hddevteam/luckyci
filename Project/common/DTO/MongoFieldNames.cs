using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.DTO
{
    //mongodb数据库 CILog表字段
   public class MongoFieldNames
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }
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
