using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LuckyCIWebAPI.Models;
using System.Data.OleDb;
using System.Data;

namespace LuckyCIWebAPI.Controllers
{
    public class LogInfoController : ApiController
    {
        List<LogInfo> LogInfos = new List<LogInfo>();

        public LogInfoController()
        {
            string config = System.AppDomain.CurrentDomain.BaseDirectory;
            string[] pathTemp = config.Split('\\');
            string mdbPath = "";
            for (int i=0;i<=pathTemp.Length-3;i++)
            {
                mdbPath += pathTemp[i]+"\\";
            }
            string connstr = "Provider=Microsoft.Jet.OLEDB.4.0 ;Data Source="+mdbPath+"common\\res\\CILog.mdb";
            OleDbConnection tempconn = new OleDbConnection(connstr);
            tempconn.Open();
            OleDbDataAdapter da = new OleDbDataAdapter(@"select * from CILog", tempconn);
            DataSet ds = new DataSet();
            da.Fill(ds, "CILog");
            //定义数组的长度
            for (int i = 0; i < ds.Tables["CILog"].Rows.Count; i++)
            {
                LogInfos.Add(new LogInfo {
                    CILogId =ds.Tables["CILog"].Rows[i]["CILogId"].ToString(),
                    ProjectName= ds.Tables["CILog"].Rows[i]["ProjectName"].ToString(),
                    CommitVersion = ds.Tables["CILog"].Rows[i]["CommitVersion"].ToString(), 
                    Submitter = ds.Tables["CILog"].Rows[i]["Submitter"].ToString(),
                    CIResult = ds.Tables["CILog"].Rows[i]["CIResult"].ToString(),
                    CITimeSpent = ds.Tables["CILog"].Rows[i]["CITimeSpent"].ToString(),
                    CIStartTime = ds.Tables["CILog"].Rows[i]["CIStartTime"].ToString(),
                    CIEndTime = ds.Tables["CILog"].Rows[i]["CIEndTime"].ToString(),
                    CILogTime = ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()

                });
                 }
        }

        //获取所有数据接口
        public List<LogInfo> GetAllLogInfo()
        {
            return LogInfos;
        }
    }
}
