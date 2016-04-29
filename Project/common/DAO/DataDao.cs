using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common.DTO;
namespace common.DAO
{
   public class DataDao
    {

        /// <summary>
        /// 检索本周本提交者提交编译的情况
        /// </summary>
        /// <param name="dbPath">数据库路径</param>
        /// <param name="username">本提交者</param>
        /// <returns></returns>
        public string[] DataSearch(string username) {
            string[] statisticsTimes = new string[4];
            statisticsTimes[0] = username;
            //总次数
            int allTimes = 0;
            //成功次数
            int successfulTimes = 0;
            //失败次数
            int failedTimes = 0;
            //string connstr = "Provider=Microsoft.Jet.OLEDB.4.0 ;Data Source=" + dbPath;
            //OleDbConnection tempconn = new OleDbConnection(connstr);
            //tempconn.Open();
            //OleDbDataAdapter da = new OleDbDataAdapter(@"select * from WeeklyReportData", tempconn);
            //DataSet ds = new DataSet();
            //da.Fill(ds, "WeeklyReportData");
            //int i = ds.Tables["WeeklyReportData"].Rows.Count;
            //for (int temp = 0; temp <= ds.Tables["WeeklyReportData"].Rows.Count - 1; temp++)
            //{
            //    if (ds.Tables["WeeklyReportData"].Rows[temp]["Submitter"].ToString()==username) {
            //        allTimes++;
            //        if (ds.Tables["WeeklyReportData"].Rows[temp]["BuildResult"].ToString() == "true") {
            //            successfulTimes++;
            //        }
            //    }
            //}
            
            var connectionString = "mongodb://localhost:27017";
            MongoClient client = new MongoClient(connectionString);
            var database = client.GetDatabase("CILog");
            var collection = database.GetCollection<WeeklyReportData>("WeeklyReportData");
            var weekCommit = collection.Find(x => x.Submitter == username).ToList();
            allTimes = weekCommit.Count;
            foreach (var singleTime in weekCommit)
            {
                if (singleTime.BuildResult == "true")
                {
                    successfulTimes++;
                }
            }

            failedTimes = allTimes - successfulTimes;
            statisticsTimes[1] = allTimes.ToString();
            statisticsTimes[2] = successfulTimes.ToString();
            statisticsTimes[3] = failedTimes.ToString();
            return statisticsTimes;
            }
        }
    }

