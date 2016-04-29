using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using common.DTO;
using System.Text.RegularExpressions;
using System.Data.OleDb;
using System.IO;
using System.Data;
using MongoDB.Driver;

namespace common.BL
{
  public  class MailController
    {
        /// <summary>
        /// 执行发送邮件的动作
        /// </summary>
        /// <param name="mailInfo">mail相关信息的实例</param>
        /// <returns>Successful表示发送成功</returns>
        /// <returns>Failed表示发送失败</returns>
        public string SendMail(MailInfo mailInfo)
        {
            try
            {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage mailMessage = new MailMessage();
            smtpClient.Host = mailInfo.Host;
            smtpClient.UseDefaultCredentials = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            if (mailInfo.Mailfrom != "" && mailInfo.Mailpassword != "")
            {
                smtpClient.Credentials = new NetworkCredential(mailInfo.Mailfrom,
                    mailInfo.Mailpassword);
            }
            mailMessage.From = new MailAddress(mailInfo.Mailfrom);
            mailMessage.To.Add(mailInfo.Mailto);
            mailMessage.Subject = mailInfo.Subject;
            mailMessage.Body = mailInfo.Body;
            mailMessage.SubjectEncoding = Encoding.UTF8;
            mailMessage.BodyEncoding = Encoding.UTF8;
            mailMessage.Priority = MailPriority.High;
            mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
                return "successful";
            }
            catch (Exception e)
            {
                return "failed";
            }
        }

        /// <summary>
        /// 执行编辑邮件相关信息的操作
        /// </summary>
        /// <param name="projectInfo">传入projectInfo的实例对象</param>
        /// <returns>返回编辑后的MailInfo实例对象</returns>
        public MailInfo EditBody(ProjectInfo projectInfo, Dictionary<string, Dictionary<string, string>> allStat, string mailPath,string shortName)
        {
            string path = string.Empty;
            System.IO.StreamReader sr = new System.IO.StreamReader(mailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();
            MailInfo mailInfo = new MailInfo();

            

            try
            {
                Dictionary<string, string> selectStat = allStat[projectInfo.Author];
                int[] allUpdate = new int[allStat.Count];
                int i = 0;
                int selectWeek = 0, selectSuccess = 0, selectFailure = 0;
                foreach (var key in allStat.Keys)
                {
                    allUpdate[i] = Int32.Parse(allStat[key]["Week"]);
                    if (key == projectInfo.Author)
                    {
                        selectWeek = Int32.Parse(allStat[key]["Week"]);
                        selectSuccess = Int32.Parse(allStat[key]["Success"]);
                        selectFailure = Int32.Parse(allStat[key]["Failure"]);
                    }
                    i++;
                }
                //计算个人本周编译次数，成功失败次数，以及其所占有的比率
                double successRate = (double)selectSuccess / selectWeek;
                double failureRate = (double)selectFailure / selectWeek;

                Array.Sort(allUpdate);
                Array.Reverse(allUpdate);
                var emoji = projectInfo.Result == "successful"
                    ? Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x98, 0x83})
                    : Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x91, 0xBF});
                var header = emoji + projectInfo.Nameproperty + " reversion " +(( projectInfo.ProjectType == "git") ?(projectInfo.GitVersion.Substring(0,8)):projectInfo.GitVersion)+ " build " +
                             projectInfo.Result + ". This week " + projectInfo.Author + " build " +
                             selectWeek.ToString() + " times, " +selectSuccess.ToString()+" passed, "+selectFailure.ToString()+" failed.";
                            
                body = body.Replace("$PROJECTNAME$", projectInfo.Nameproperty);
                body = body.Replace("$LOG$", projectInfo.Log);
                body = body.Replace("$VERSION$", projectInfo.Revision);
                body = body.Replace("$AUTHOR$", projectInfo.Author);
                body = body.Replace("$DATE$", projectInfo.Changetime);
                body = body.Replace("$RESULT$", "build " + projectInfo.Result);
                body = body.Replace("$DURATION$", projectInfo.Duration);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                body = projectInfo.Log == "\n"
                    ? body.Replace("$LOGLABEL$", "")
                    : body.Replace("$LOGLABEL$", "Build Log");
                mailInfo.Subject = header;
                mailInfo.Body = body;
                mailInfo.Mailto = projectInfo.MailTo;
                mailInfo.Host = projectInfo.MailHost;
                if (projectInfo.UserName.Contains("#author#"))
                {
                    mailInfo.Mailfrom =(shortName!="")?(shortName + Regex.Match(projectInfo.UserName, "(?:@)(.+)").Value) : (projectInfo.Author + Regex.Match(projectInfo.UserName, "(?:@)(.+)").Value);
                }
                else
                {
                    mailInfo.Mailfrom = projectInfo.UserName.Replace("#","");
                    mailInfo.Mailpassword = projectInfo.Password;
                }
                return mailInfo;
            }
            catch (Exception ex)
            {
                return mailInfo;
            }
        }
        /// <summary>
        /// 发送每周邮件报告
        /// </summary>
        /// <param name="reportMailPath"></param>
        /// <param name="allStatics"></param>
        /// <param name="allProjectStatics"></param>
        /// <returns></returns>
        public bool SendReport(string reportMailPath,Dictionary<string, Dictionary<string, string>> allStatics,Dictionary<string, Dictionary<string, string>> allProjectStatics, Dictionary<string, string> totalTimes) {
            string totalWeekTimes = "";
            string weekByDeveloper = "";
            string buildByDeveloper = "";
            string weekByProject = "";
            string buildByProject = "";
            int memberSort = 1;
            int projectSort = 1;
            System.IO.StreamReader sr = new System.IO.StreamReader(reportMailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();

            if (int.Parse(totalTimes["CommitTimes"]) != 0)
            {
                totalWeekTimes += "Commit " + totalTimes["CommitTimes"] + " times<br/>" +
                                "Build " + totalTimes["CommitTimes"] + " times<br/>" +
                                 totalTimes["BuildSuccessTimes"] + " passed(" + (((double.Parse(totalTimes["BuildSuccessTimes"])) / (double.Parse(totalTimes["CommitTimes"])))).ToString("0%") + ")<br/>" +
                                 totalTimes["BuildFailedTimes"] + " failed(" + (((double.Parse(totalTimes["BuildFailedTimes"])) / (double.Parse(totalTimes["CommitTimes"])))).ToString("0%") + ")";



                
                while (true) {
                    if (allStatics.Count==0) { break; };
                    KeyValuePair<string, Dictionary<string, string>> temp = new KeyValuePair<string, Dictionary<string, string>>();
                    foreach (var member in allStatics)
                    {
                        temp = member;
                        foreach (var memberCopy in allStatics)
                        {
                            if (int.Parse(temp.Value["Week"]) < int.Parse(memberCopy.Value["Week"]))
                            {
                                temp = memberCopy;
                            }                          
                        }
                        break;
                    }
                        if (int.Parse(temp.Value["Week"]) == 0) { allStatics.Remove(temp.Key);  continue; }
                        weekByDeveloper += memberSort + ". " + temp.Key + " , " + temp.Value["Week"] + "<br/>";
                        buildByDeveloper += memberSort + ". " + temp.Key + " , " + temp.Value["Week"] + " builds, " +
                             temp.Value["Success"] + " passed(" + (((double.Parse(temp.Value["Success"])) / (double.Parse(temp.Value["Week"])))).ToString("0%") + "), "
                            + temp.Value["Failure"] + " failed(" + (((double.Parse(temp.Value["Failure"])) / (double.Parse(temp.Value["Week"])))).ToString("0%") + ") <br/>";
                        memberSort++;
                    allStatics.Remove(temp.Key);
                    
                }



                while (true)
                {
                    if (allProjectStatics.Count==0) { break; };
                    KeyValuePair<string, Dictionary<string, string>> projectTemp = new KeyValuePair<string, Dictionary<string, string>>();
                    foreach (var project in allProjectStatics)
                    {
                        projectTemp = project;
                        foreach (var projectCopy in allProjectStatics)
                        {
                            if (int.Parse(projectTemp.Value["Commit"]) < int.Parse(projectCopy.Value["Commit"]))
                            {
                                projectTemp = projectCopy;
                            }                            
                        }
                        break;
                    }
                        weekByProject += projectSort + ". " + projectTemp.Key + " , " + projectTemp.Value["Commit"] + "<br/>";
                        buildByProject += projectSort + ". " + projectTemp.Key + " , " + projectTemp.Value["Commit"] + " builds, " +
                             projectTemp.Value["Success"] + " passed(" + (((double.Parse(projectTemp.Value["Success"])) / (double.Parse(projectTemp.Value["Commit"])))).ToString("0%") + "), "
                            + projectTemp.Value["Failed"] + " failed(" + (((double.Parse(projectTemp.Value["Failed"])) / (double.Parse(projectTemp.Value["Commit"])))).ToString("0%") + ") <br/>";
                        projectSort++;
                    allProjectStatics.Remove(projectTemp.Key);
                }
            



                body = body.Replace("$WEEKTOTAL$", totalWeekTimes);
                body = body.Replace("$COMMIT_DEVELOPER$", weekByDeveloper);
                body = body.Replace("$COMMIT_PROJECT$", weekByProject);
                body = body.Replace("$BUILD_DEVELOPER$", buildByDeveloper);
                body = body.Replace("$BUILD_PROJECT$", buildByProject);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SmtpClient smtpClient = new SmtpClient();
                MailMessage mailMessage = new MailMessage();
                smtpClient.Host = "exch.henu.edu.cn";
                smtpClient.UseDefaultCredentials = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                string thisMonday = DateTime.Now.ToString("yyyy/MM/dd");
                string lastMonday = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd");

                mailMessage.From = new MailAddress("jxm@it.henu.edu.cn");
                mailMessage.To.Add("svn-notify@it.henu.edu.cn");
                mailMessage.Subject = "Lucky CI Weekly Report " + lastMonday + "-" + thisMonday;
                mailMessage.Body = body;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.Priority = MailPriority.High;
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
            }
            return true;
        }
        /// <summary>
        /// 从access数据库中取得数据，发送周报告
        /// </summary>
        /// <param name="mdbPath"></param>
        /// <param name="reportMailPath"></param>
        /// <param name="configInfo"></param>
        /// <returns></returns>
        public bool StatisticsFromMdb(string mdbPath,string reportMailPath,ConfigInfo configInfo)
        {
            //需要统计的数据

            //一周提交总次数
            int allTimes=0;
            int allSuccessfulTimes = 0;
            int allFiledTimes = 0;

            //暂存作者
            List<string> memberList = new List<string>();
            List<int> memberSuccessfulTimes = new List<int>();
            List<int> memberFiledTimes = new List<int>();
            //暂存项目名称
            List<string> projectList = new List<string>();
            List<int> projectSuccessfulTimes = new List<int>();
            List<int> projectFiledTimes = new List<int>();
            //获取上周一的日期
            DateTime lastMondayDate = DateTime.Now.AddDays(-7);
            try {
                //连接数据库
                string connstr = "Provider=Microsoft.Jet.OLEDB.4.0 ;Data Source=" + mdbPath;
                OleDbConnection tempconn = new OleDbConnection(connstr);
                tempconn.Open();
                OleDbDataAdapter da = new OleDbDataAdapter(@"select * from CILog", tempconn);
                DataSet ds = new DataSet();
                da.Fill(ds, "CILog");
                //倒着遍历，与上周一时间相对比
                for (int i = ds.Tables["CILog"].Rows.Count - 1; i >= 0; i--) {
                    //总次数统计
                    if (DateTime.Parse(ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()) >= lastMondayDate)
                    {
                        //总次数加一
                        allTimes++;
                        //判断本次书否编译成功
                        if (bool.Parse(ds.Tables["CILog"].Rows[i]["CIResult"].ToString()))
                        {
                            //成功次数加一
                            allSuccessfulTimes++;
                        }
                        //失败次数加一
                        else allFiledTimes++;


                        //作者统计
                        bool memberFlag = false;
                        for (int member = 0; member < memberList.Count; member++)
                        {
                            if (ds.Tables["CILog"].Rows[i]["Submitter"].ToString() == memberList[member]) { memberFlag = true; break; }
                        }
                        if (memberFlag == false) { memberList.Add(ds.Tables["CILog"].Rows[i]["Submitter"].ToString()); }

                        //项目统计
                        bool projectFlag = false;
                        for (int project = 0; project < projectList.Count; project++)
                        {
                            if (ds.Tables["CILog"].Rows[i]["ProjectName"].ToString() == projectList[project]) { projectFlag = true; break; }
                        }
                        if (projectFlag == false) { projectList.Add(ds.Tables["CILog"].Rows[i]["ProjectName"].ToString()); }
                    }
                    else break;

                }

                //第二次遍历，统计每个人提交次数以及编译情况

                for (int j = 0; j < memberList.Count; j++)
                {
                    int tempTrue = 0;
                    int tempFalse = 0;
                    for (int i = ds.Tables["CILog"].Rows.Count - 1; i >= 0; i--)
                    {
                        if (DateTime.Parse(ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()) >= lastMondayDate && ds.Tables["CILog"].Rows[i]["Submitter"].ToString() == memberList[j] &&
                            ds.Tables["CILog"].Rows[i]["CIResult"].ToString() == "True")
                        {
                            tempTrue++;
                        }
                        if (DateTime.Parse(ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()) >= lastMondayDate && ds.Tables["CILog"].Rows[i]["Submitter"].ToString() == memberList[j] &&
                            ds.Tables["CILog"].Rows[i]["CIResult"].ToString() == "False")
                        {
                            tempFalse++;
                        }
                    }
                    memberSuccessfulTimes.Add(tempTrue);
                    memberFiledTimes.Add(tempFalse);
                }

                //第三次遍历，统计每个项目提交次数以及编译情况
                for (int j = 0; j < projectList.Count; j++)
                {
                    int tempTrue = 0;
                    int tempFalse = 0;
                    for (int i = ds.Tables["CILog"].Rows.Count - 1; i >= 0; i--)
                    {
                        if (DateTime.Parse(ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()) >= lastMondayDate && ds.Tables["CILog"].Rows[i]["ProjectName"].ToString() == projectList[j] &&
                            ds.Tables["CILog"].Rows[i]["CIResult"].ToString() == "True")
                        {
                            tempTrue++;
                        }
                        if (DateTime.Parse(ds.Tables["CILog"].Rows[i]["CILogTime"].ToString()) >= lastMondayDate && ds.Tables["CILog"].Rows[i]["ProjectName"].ToString() == projectList[j] &&
                            ds.Tables["CILog"].Rows[i]["CIResult"].ToString() == "False")
                        {
                            tempFalse++;
                        }
                    }
                    projectSuccessfulTimes.Add(tempTrue);
                    projectFiledTimes.Add(tempFalse);
                }
                //排版发送邮件内容
                string totalWeekTimes = "";
                string weekByDeveloper = "";
                string buildByDeveloper = "";
                string weekByProject = "";
                string buildByProject = "";
                System.IO.StreamReader sr = new System.IO.StreamReader(reportMailPath);
                string body = string.Empty;
                body = sr.ReadToEnd();

                //总次数信息报告
                totalWeekTimes += "Commit " + allTimes + " times<br/>" +
                                    "Build " + allTimes + " times<br/>" +
                                    allSuccessfulTimes + " passed(" + (((double.Parse(allSuccessfulTimes.ToString())) / (double.Parse(allTimes.ToString())))).ToString("0%") + ")<br/>" +
                                     allFiledTimes + " failed(" + (((double.Parse(allFiledTimes.ToString())) / (double.Parse(allTimes.ToString())))).ToString("0%") + ")";
                //作者信息报告
                for (int i = 0; i < memberList.Count; i++)
                {
                    weekByDeveloper += (i + 1).ToString() + ". " + memberList[i] + " , " + (memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString() + "<br/>";

                    buildByDeveloper += (i + 1).ToString() + ". " + memberList[i] + " , " + (memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString() + " builds, " +
                  memberSuccessfulTimes[i] + " passed(" + (((double.Parse(memberSuccessfulTimes[i].ToString())) / (double.Parse(((memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString()))))).ToString("0%") + "), "
                 + memberFiledTimes[i] + " failed(" + (((double.Parse(memberFiledTimes[i].ToString())) / (double.Parse((memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString())))).ToString("0%") + ") <br/>";
                }
                //项目信息报告
                for (int i = 0; i < projectList.Count; i++)
                {
                    weekByProject += (i + 1).ToString() + ". " + projectList[i] + " , " + (projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString() + "<br/>";
                    buildByProject += (i + 1).ToString() + ". " + projectList[i] + " , " + (projectFiledTimes[i] + projectSuccessfulTimes[i]).ToString() + " builds, " +
                         projectSuccessfulTimes[i].ToString() + " passed(" + (((double.Parse(projectSuccessfulTimes[i].ToString())) / (double.Parse((projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString())))).ToString("0%") + "), "
                        + projectFiledTimes[i].ToString() + " failed(" + (((double.Parse(projectFiledTimes[i].ToString())) / (double.Parse((projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString())))).ToString("0%") + ") <br/>";

                }
                //发送邮件报告
                body = body.Replace("$WEEKTOTAL$", totalWeekTimes);
                body = body.Replace("$COMMIT_DEVELOPER$", weekByDeveloper);
                body = body.Replace("$COMMIT_PROJECT$", weekByProject);
                body = body.Replace("$BUILD_DEVELOPER$", buildByDeveloper);
                body = body.Replace("$BUILD_PROJECT$", buildByProject);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SmtpClient smtpClient = new SmtpClient();
                MailMessage mailMessage = new MailMessage();
                smtpClient.Host = configInfo.SmtpServer;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                string thisMonday = DateTime.Now.ToString("yyyy/MM/dd");
                string lastMonday = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd");

                mailMessage.From = new MailAddress(configInfo.ReportFrom);
                mailMessage.To.Add(configInfo.ReportTo);
                mailMessage.Subject = "Lucky CI Weekly Report " + lastMonday + "-" + thisMonday;
                mailMessage.Body = body;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.Priority = MailPriority.High;
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex) { return false; }

        }

        /// <summary>
        /// 从mongodb中获取数据，发送周报告
        /// </summary>
        /// <param name="mongoConnectionPath"></param>
        /// <param name="reportMailPath"></param>
        /// <param name="configInfo"></param>
        /// <returns></returns>
        public bool SendWeekilReportFromMongodb(string mongoConnectionPath,string reportMailPath, ConfigInfo configInfo) {
            //需要统计的数据

            //一周提交总次数
            int allTimes = 0;
            int allSuccessfulTimes = 0;
            int allFiledTimes = 0;

            //暂存作者
            List<string> memberList = new List<string>();
            List<int> memberSuccessfulTimes = new List<int>();
            List<int> memberFiledTimes = new List<int>();
            //暂存项目名称
            List<string> projectList = new List<string>();
            List<int> projectSuccessfulTimes = new List<int>();
            List<int> projectFiledTimes = new List<int>();
            //获取上周一的日期
            DateTime lastMondayDate = DateTime.Now.AddDays(-7);
            try
            {
                //连接mongo数据库
                MongoClient client = new MongoClient(mongoConnectionPath);
                var database = client.GetDatabase("CILog");
                //var collection = database.GetCollection<User>("persion");
                var collection = database.GetCollection<WeeklyReportData>("WeeklyReportData");
                var list =  collection.Find(x => x.Submitter != "").ToList();
                allTimes = list.Count;
                collection.DeleteMany(x => x.Submitter != "");
                //遍历，总成功次数，以及总失败次数
                foreach (var singleTime in list)
                {
                    if (singleTime.BuildResult == "true")
                    {
                        allSuccessfulTimes++;
                    }
                    else { allFiledTimes++; }
                    //作者人名统计
                    bool memberFlag = false;
                    for(int i=0;i< memberList.Count;i++)
                    {
                        if (singleTime.Submitter==memberList[i]) {memberFlag=true; break; }                      
                    }
                    if (memberFlag == false) { memberList.Add(singleTime.Submitter); }
                    //项目名称统计
                    bool projectFlag = false;
                    for (int j=0;j< projectList.Count;j++)
                    {
                        if (singleTime.ProjectName == projectList[j]) { projectFlag = true;break; }
                    }
                    if (projectFlag == false) { projectList.Add(singleTime.ProjectName); }
                
                }
                //遍历，统计个人成功失败的次数
                for (int i=0;i<memberList.Count;i++)
                {
                    int tempTrue = 0;
                    int tempFalse = 0;
                    foreach (var singleTime in list)
                    {
                        if (memberList[i]==singleTime.Submitter&&singleTime.BuildResult=="true")
                        {
                            tempTrue++;
                        }
                        if (memberList[i] == singleTime.Submitter && singleTime.BuildResult == "false")
                        {
                            tempFalse++;
                        }
                    }
                    memberSuccessfulTimes.Add(tempTrue);
                    memberFiledTimes.Add(tempFalse);
                }
                //遍历，统计项目成功失败的次数
                for (int i=0;i<projectList.Count;i++)
                {
                    int tempTrue = 0;
                    int tempFalse = 0;
                    foreach (var singleTime in list)
                    {
                        if (projectList[i]==singleTime.ProjectName&&singleTime.BuildResult=="true")
                        {
                            tempTrue++;
                        }
                        if (projectList[i]==singleTime.ProjectName&&singleTime.BuildResult=="false")
                        {
                            tempFalse++;
                        }
                    }
                    projectSuccessfulTimes.Add(tempTrue);
                    projectFiledTimes.Add(tempFalse);
                }
                            
                //排版发送邮件内容
                string totalWeekTimes = "";
                string weekByDeveloper = "";
                string buildByDeveloper = "";
                string weekByProject = "";
                string buildByProject = "";
                System.IO.StreamReader sr = new System.IO.StreamReader(reportMailPath);
                string body = string.Empty;
                body = sr.ReadToEnd();

                //总次数信息报告
                totalWeekTimes += "Commit " + allTimes + " times<br/>" +
                                    "Build " + allTimes + " times<br/>" +
                                    allSuccessfulTimes + " passed(" + (((double.Parse(allSuccessfulTimes.ToString())) / (double.Parse(allTimes.ToString())))).ToString("0%") + ")<br/>" +
                                     allFiledTimes + " failed(" + (((double.Parse(allFiledTimes.ToString())) / (double.Parse(allTimes.ToString())))).ToString("0%") + ")";
                //作者信息报告
                for (int i = 0; i < memberList.Count; i++)
                {
                    weekByDeveloper += (i + 1).ToString() + ". " + memberList[i] + " , " + (memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString() + "<br/>";

                    buildByDeveloper += (i + 1).ToString() + ". " + memberList[i] + " , " + (memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString() + " builds, " +
                  memberSuccessfulTimes[i] + " passed(" + (((double.Parse(memberSuccessfulTimes[i].ToString())) / (double.Parse(((memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString()))))).ToString("0%") + "), "
                 + memberFiledTimes[i] + " failed(" + (((double.Parse(memberFiledTimes[i].ToString())) / (double.Parse((memberSuccessfulTimes[i] + memberFiledTimes[i]).ToString())))).ToString("0%") + ") <br/>";
                }
                //项目信息报告
                for (int i = 0; i < projectList.Count; i++)
                {
                    weekByProject += (i + 1).ToString() + ". " + projectList[i] + " , " + (projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString() + "<br/>";
                    buildByProject += (i + 1).ToString() + ". " + projectList[i] + " , " + (projectFiledTimes[i] + projectSuccessfulTimes[i]).ToString() + " builds, " +
                         projectSuccessfulTimes[i].ToString() + " passed(" + (((double.Parse(projectSuccessfulTimes[i].ToString())) / (double.Parse((projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString())))).ToString("0%") + "), "
                        + projectFiledTimes[i].ToString() + " failed(" + (((double.Parse(projectFiledTimes[i].ToString())) / (double.Parse((projectSuccessfulTimes[i] + projectFiledTimes[i]).ToString())))).ToString("0%") + ") <br/>";

                }
                //发送邮件报告
                body = body.Replace("$WEEKTOTAL$", totalWeekTimes);
                body = body.Replace("$COMMIT_DEVELOPER$", weekByDeveloper);
                body = body.Replace("$COMMIT_PROJECT$", weekByProject);
                body = body.Replace("$BUILD_DEVELOPER$", buildByDeveloper);
                body = body.Replace("$BUILD_PROJECT$", buildByProject);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SmtpClient smtpClient = new SmtpClient();
                MailMessage mailMessage = new MailMessage();
                smtpClient.Host = configInfo.SmtpServer;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                string thisMonday = DateTime.Now.ToString("yyyy/MM/dd");
                string lastMonday = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd");

                mailMessage.From = new MailAddress(configInfo.ReportFrom);
                mailMessage.To.Add(configInfo.ReportTo);
                mailMessage.Subject = "Lucky CI Weekly Report " + lastMonday + "-" + thisMonday;
                mailMessage.Body = body;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.Priority = MailPriority.High;
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
                return true;
            }
            catch (Exception ex) { return false; }
        }

        //成功失败次数直接封装
        /// <summary>
        /// 最新编辑邮件模板
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="statisticsTimes"></param>
        /// <param name="mailPath"></param>
        /// <param name="shortName"></param>
        /// <returns></returns>
        public MailInfo NewEditBody(ProjectInfo projectInfo,string[] statisticsTimes, string mailPath, string shortName)
        {
            string path = string.Empty;
            System.IO.StreamReader sr = new System.IO.StreamReader(mailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();
            MailInfo mailInfo = new MailInfo();
            try
            {
                
                //计算个人本周编译次数，成功失败次数，以及其所占有的比率
                double successRate = double.Parse(statisticsTimes[2]) / double.Parse(statisticsTimes[1]);
                double failureRate = double.Parse(statisticsTimes[3]) / double.Parse(statisticsTimes[1]);

                var emoji = projectInfo.Result == "successful"
                    ? Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x98, 0x83 })
                    : Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x91, 0xBF });
                var header = emoji + projectInfo.Nameproperty + " reversion " + ((projectInfo.ProjectType == "git") ? (projectInfo.GitVersion.Substring(0, 8)) : projectInfo.GitVersion) + " build " +
                             projectInfo.Result + ". This week " + projectInfo.Author + " build " +
                             statisticsTimes[1] + " times, " + statisticsTimes[2] + " passed, " + statisticsTimes[3] + " failed.";

                body = body.Replace("$PROJECTNAME$", projectInfo.Nameproperty);
                body = body.Replace("$LOG$", projectInfo.Log);
                body = body.Replace("$VERSION$", projectInfo.Revision);
                body = body.Replace("$AUTHOR$", projectInfo.Author);
                body = body.Replace("$DATE$", projectInfo.Changetime);
                body = body.Replace("$RESULT$", "build " + projectInfo.Result);
                body = body.Replace("$DURATION$", projectInfo.Duration);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                body = projectInfo.Log == "\n"
                    ? body.Replace("$LOGLABEL$", "")
                    : body.Replace("$LOGLABEL$", "Build Log");
                mailInfo.Subject = header;
                mailInfo.Body = body;
                mailInfo.Mailto = projectInfo.MailTo;
                mailInfo.Host = projectInfo.MailHost;
                if (projectInfo.UserName.Contains("#author#"))
                {
                    mailInfo.Mailfrom = (shortName != "") ? (shortName + Regex.Match(projectInfo.UserName, "(?:@)(.+)").Value) : (projectInfo.Author + Regex.Match(projectInfo.UserName, "(?:@)(.+)").Value);
                }
                else
                {
                    mailInfo.Mailfrom = projectInfo.UserName.Replace("#", "");
                    mailInfo.Mailpassword = projectInfo.Password;
                }
                return mailInfo;
            }
            catch (Exception ex)
            {
                return mailInfo;
            }
        }
    }
    }

