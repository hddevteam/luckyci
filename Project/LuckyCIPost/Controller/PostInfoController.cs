using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using LuckyCIPost.Model;
using common.DTO;
using common.BL;
using System.Xml;
using common.DAO;
using System.Net;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LuckyCIPost.Controller
{
    public class PostInfoController : ApiController
    {
        ConfigController _configController = new ConfigController();
        ProjectController _projectController = new ProjectController();
        GitController _gitController = new GitController();
       

        public string PostInfoFunction([FromBody]PostInfo postInfo)
        {
            string config = System.AppDomain.CurrentDomain.BaseDirectory;          
            //try
            //{
            //    int local1 = config.LastIndexOf("\\");
            //    string parent1 = config.Substring(0, local1 - 12);
            //    //配置信息路径
            //    string CIConfigPath1 = parent1 + "\\common\\res\\CIconfig.xml";
     
            //    FileStream fs = new FileStream(parent1 + "\\common\\res\\log.txt", FileMode.Append, FileAccess.Write);
            //    StreamWriter sw = new StreamWriter(fs); // 创建写入流
            //    string outputLog = CIConfigPath1 + "\n";
            //    sw.WriteLine(outputLog); // 写入
            //    sw.Close(); //关闭文件
            //}
            //catch (Exception e)
            //{
            //    FileStream fs = new FileStream(config + "log.txt", FileMode.Append, FileAccess.Write);
            //    StreamWriter sw = new StreamWriter(fs); // 创建写入流
            //    string outputLog = e.ToString();
            //    sw.WriteLine(outputLog); // 写入
            //    sw.Close();
            //}

            //获取提交的分支
            //string[] postRef = postInfo.@ref.Split('/');
            //string pushBranch = postRef[2];
            //int local = config.LastIndexOf("\\");
            //string parent = config.Substring(0, local - 12);
            ////配置信息路径
            //string CIConfigPath = parent + "\\common\\res\\CIconfig.xml";
            ////数据库路径
            //string dbPath = parent + "\\common\\res\\CILog.mdb";
            ////log日志路径
            //string logPath = parent + "\\log\\log.txt";
            ////邮件模板路径
            //string mailPath = parent + "\\common\\SendMail.html";
            //string mailWeekReportPath = parent + "\\common\\WeeklyReport.html";
            ////获取config中项目的配置信息
            //List<ProjectInfo> projectInfos = _projectController.ProjectQuery("/config/Projects", true,
            //    CIConfigPath);


            ////项目进行匹配，然后进行编译操作
            //foreach (var projectInfo in projectInfos)
            //{
            //    if (projectInfo.Statusproperty == "true" && projectInfo.Nameproperty == postInfo.project.name)
            //    {
            //        //编译项目，并且对要存储的数据进行更新，赋值
            //        ProjectInfo buildProjectInfo = buildProject(projectInfo,pushBranch,CIConfigPath);
            //        buildProjectInfo.GitVersion = postInfo.after;
            //        buildProjectInfo.Author = postInfo.user_name;
            //        buildProjectInfo.Branch = postInfo.@ref;
            //        //存储信息到数据库当中mongodb
            //        SaveInfoToDataBase(buildProjectInfo,dbPath,logPath);


            //        ConfigInfo configInfo = _configController.ConfigQuery("config/preferences", CIConfigPath);
            //        SendMailSlack sendInfo = new SendMailSlack(buildProjectInfo,mailPath, CIConfigPath, dbPath,configInfo, mailWeekReportPath);
            //        sendInfo.SendMail();
            //        sendInfo.SendSlack();
            //        //sendInfo.SendWeeklyReportFromMongodb();
            //        break;
            //    }
            //}

            var factory = new ConnectionFactory();
            factory.HostName = "localhost";
            factory.UserName = "ccy";
            factory.Password = "cuichongyang_123";
             using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("CIQueues", false, false, false, null);
                    var sendMessage = "ProjectName:" + postInfo.project.name + ";CommitVerson:" + postInfo.after + ";Submitter:" + postInfo.user_name + ";pushBranch:" + postInfo.@ref;
                    var body = Encoding.UTF8.GetBytes(sendMessage);
                    channel.BasicPublish("", "CIQueues", null, body);
                }
            }
            return "successful";

        }
        /// <summary>
        /// 收到post信息，首先对分支进行切换，然后git pull拉取本分支的最新代码，最后再进行项目的编译工作
        /// </summary>
        /// <param name="projectInfo">项目信息</param>
        /// <param name="pushBranch">操作的分支</param>
        /// <param name="CIConfigPath">项目config路径</param>
        /// <returns></returns>
        public ProjectInfo buildProject(ProjectInfo projectInfo,string pushBranch,string CIConfigPath)
        {
            //进行分支切换操作
            bool switchBranch = _gitController.git_checkout("git checkout " + pushBranch, projectInfo.WorkDirectory);
            //若是分支切换成功，进行项目编译
            if (switchBranch)
            {

                //获取gitlab信息
                GitInfo gitlabInfo = _configController.GitInfoQuery("config/GitLabInfo", CIConfigPath);

                Boolean gitPullResult = _gitController.Libgit2_GitPull(projectInfo.WorkDirectory, gitlabInfo.Username, gitlabInfo.Password, gitlabInfo.Emailaddress);
                if (gitPullResult)
                {
                    string buildResult; //存储编译是否成功
                    string error; //存储编译的日志文件
                    string log; //存储编译的所有信息
                    string time;
                    ConfigInfo configInfo = _configController.ConfigQuery("config/preferences",
         CIConfigPath);
                    projectInfo.BuildTime = DateTime.Now.ToString();
                    projectInfo.StartTime = DateTime.Now.ToString();
                    if (projectInfo.Nameproperty == "LuckyCI") { projectInfo.WorkDirectory += "\\Project"; }
                    log = _projectController.Build(projectInfo.BuildCommand, projectInfo.WorkDirectory,
           out buildResult, out error, out time);
                    projectInfo.Duration = time;
                    projectInfo.EndTime = DateTime.Now.ToString();
                    projectInfo.Log = (configInfo.StandarOutput == "true")
                        ? ("\n" + log + "\n" + error)
                        : ("\n" + error);
                    projectInfo.Result = buildResult;
                }
            }
            return projectInfo;
        }
        /// <summary>
        /// 编译完成之后，对本次项目信息进行存储
        /// </summary>
        /// <param name="projectInfo">项目信息</param>
        /// <param name="dbPath">数据库路径</param>
        /// <param name="logPath">日志路径</param>
        /// <returns></returns>
        public bool SaveInfoToDataBase(ProjectInfo projectInfo, string dbPath, string logPath)
        {
            return _projectController.saveLogToDataBase(projectInfo, dbPath, logPath);
        }
    }
    //发送邮件，发送slack
    public class SendMailSlack
    {
        ConfigController _configController = new ConfigController();
        ProjectController _projectController = new ProjectController();
        MailController _mailController = new MailController();
        DataDao dataDao = new DataDao();
        private ProjectInfo projectInfo;
        private ConfigInfo configInfo;
        private string mailPath;
        private string mailWeekReportPath;
        private string mdbPath;
        private XmlNodeList _slackPeople;
        private XmlNodeList _mailPeople;

        /// <summary>
        /// 构造函数，在线程启动之前，进行赋值
        /// </summary>
        /// <param name="projectInfo">项目信息</param>
        /// <param name="ifSlack">是否发送slack</param>
        /// <param name="ifMail">是否发送邮件</param>
        public SendMailSlack(ProjectInfo projectInfo, string mailPath, string CIConfigParh, string mdbPath, ConfigInfo configInfo,string mailWeekReportPath)
        {
            if (configInfo != null) { this.configInfo = configInfo; }
            if (CIConfigParh != null)
            {
                _slackPeople = _configController.AcquireSlackPeople("/config/SlackPeople/People", CIConfigParh);
                _mailPeople = _configController.AcquireSlackPeople("/config/MailPeople/People", CIConfigParh);
            }
            this.projectInfo = projectInfo;
            this.mailPath = mailPath;
            this.mdbPath = mdbPath;
            this.mailWeekReportPath = mailWeekReportPath;
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        public void SendMail()
        {
            string[] statisticsTimes = dataDao.DataSearch(projectInfo.Author);
            string shortName = "";
            if (projectInfo.ProjectType == "git")
            {
                //修改发送邮件名字 ，改为简写
                foreach (XmlNode people in _mailPeople)
                {
                    if (projectInfo.Author == people.Attributes["Name"].Value)
                    {
                        shortName = people.InnerText;
                        break;
                    }
                }
            }
            //svn名字本来就是简写，不用转换
            if (projectInfo.ProjectType == "svn")
            {
                shortName = projectInfo.Author;
            }


            try
            {
                if (projectInfo.IfMail == "true")
                {
                    MailInfo mailInfo = _mailController.NewEditBody(projectInfo,statisticsTimes ,
                        mailPath, shortName);
                    _mailController.SendMail(mailInfo);
                }
            }
            catch (Exception ex)
            {
             
            }

        }
        /// <summary>
        /// 发送slack
        /// </summary>
        /// <returns></returns>
        public void SendSlack()
        {
            string[] statisticsTimes = dataDao.DataSearch(projectInfo.Author);
            try
            {
         
                    WebClient webClient = new WebClient();
                    webClient.Encoding = Encoding.GetEncoding("utf-8");
                    string slackBody = projectInfo.SlackContent;
                    slackBody = slackBody.Replace("projectName", projectInfo.Nameproperty);
                    //slackBody = slackBody.Replace("revision", "");
                    slackBody = slackBody.Replace("versionNum", projectInfo.ProjectType == "git" ? (projectInfo.GitVersion.Substring(0, 8)) : (projectInfo.GitVersion));
                    slackBody = slackBody.Replace("buildtime", projectInfo.Duration);
                    string[] contentItem = slackBody.Split('#');
                    string slackEmoji = projectInfo.Result == "successful"
                        ? contentItem[1].Split(':')[0]
                        : contentItem[1].Split(':')[1];
                    contentItem[contentItem.Length - 3] = projectInfo.Result == "successful"
                        ? contentItem[contentItem.Length - 3].Split(':')[0]
                        : contentItem[contentItem.Length - 3].Split(':')[1];
                    slackBody = "";
                    for (int i = 2; i < contentItem.Length - 1; i++)
                    {
                        slackBody += contentItem[i] + " ";

                    }
                    if (projectInfo.Result == "successful")
                    {
                        slackBody += "This week " + projectInfo.Author + " build " + statisticsTimes[1] +
                                     " times, " + statisticsTimes[2] + " passed, " + statisticsTimes[3] + " failed.";
                    }
                    else if (projectInfo.Result == "failed")
                    {
                        slackBody += "This week " + projectInfo.Author + " build " + statisticsTimes[1] +
                                    " times, " + statisticsTimes[2] + " passed, " + statisticsTimes[3] + " failed.";
                    }
                    string userName = projectInfo.SlackUser == "#author#"
                        ? projectInfo.Author
                        : projectInfo.SlackUser.Replace("#", "");
                    //将传输信息写入json
                    //svn项目，需要将名字改为全称
                    if (projectInfo.ProjectType == "svn")
                    {
                        foreach (XmlNode people in _slackPeople)
                        {
                            if (userName == people.Attributes["Name"].Value)
                            {
                                userName = people.InnerText;
                                break;
                            }
                        }
                    }
                    var payLoad = new
                    {
                        channel = projectInfo.SlackChannel,
                        text = ":" + slackEmoji + ":" + slackBody,
                        username = userName
                    };
                    string json = JsonConvert.SerializeObject(payLoad);
                    string slackResult = webClient.UploadString(projectInfo.SlackUrl, json);
                    string notify = "@" + projectInfo.Author;
                    if (projectInfo.Result == "failed")
                    {
                        var inform = new
                        {
                            channel = notify,
                            username = projectInfo.SlackChannel
                        };
                        json = JsonConvert.SerializeObject(inform);
                        string notifyResult = webClient.UploadString(projectInfo.SlackUrl, json);
                    }       
            }
            catch (Exception e)
            {
              

            }

        }
        /// <summary>
        /// 从数据库中检索数据，发送周报告
        /// </summary>
        /// <returns></returns>
        public bool SendWeeklyReportFromMongodb()
        {
            _mailController.SendWeekilReportFromMongodb("mongodb://localhost:27017", mailWeekReportPath, configInfo);
            return true;
        }
    }
}