using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using common.BL;
using common.DTO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Threading;
using common.TOOL;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using common.DAO;
using System.Runtime.Serialization.Json;
using System.IO;

namespace LuckyCIService
{
    public partial class CIService : ServiceBase
    {
        GitController _gitlabController = new GitController();
        SvnController _svnController = new SvnController();
        ConfigController _configController = new ConfigController();
        ProjectController _projectController = new ProjectController();
        MailController _mailController = new MailController();
        bool _notSendReport = true;
        System.Timers.Timer _sendWeeklyReportTimer = new System.Timers.Timer();//服务中用此计时器，其他计时器无法触发。另外，需要注意，此计时器是多线程计时器。
        public CIService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //暂时注销

            //string config = System.AppDomain.CurrentDomain.BaseDirectory;
            //int p = config.LastIndexOf("\\");
            //string parent = config.Substring(0, p - 25);
            //string xmlPath = parent + "\\common\\res\\CIconfig.xml";
            //_slackPeople = _configController.AcquireSlackPeople("/config/SlackPeople/People", xmlPath);
            Thread thread = new Thread(fun);
            thread.Start();

            _sendWeeklyReportTimer.Enabled = true;//每隔十秒触发一次
            _sendWeeklyReportTimer.Interval = 10000;//单位毫秒
            _sendWeeklyReportTimer.Elapsed += _sendWeeklyReportTimer_Elapsed;

        }

        /// <summary>
        /// 计时器触发，发送一周编译报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _sendWeeklyReportTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            string config = System.AppDomain.CurrentDomain.BaseDirectory;
            int p = config.LastIndexOf("\\");
            string parent = config.Substring(0, p - 25);
            string mailWeekReportPath = parent + "\\common\\WeeklyReport.html";
            string mdbPath = parent + "\\common\\res\\CILog.mdb";
            string xmlPath = parent + "\\common\\res\\CIconfig.xml";
            ConfigController configController = new ConfigController();
            ConfigInfo configInfo = configController.ConfigQuery("config/preferences", xmlPath);
            DayOfWeek today = DateTime.Now.DayOfWeek;
            bool orMonday = (today.ToString() == "Monday");
            bool timeNow = (DateTime.Now.ToShortTimeString() == "8:30");
            if (orMonday == false) { _notSendReport = true; }
            if (orMonday && timeNow && _notSendReport)
            {
                _notSendReport = false;
                //发送一周总结报告邮件
                SendMailSlack sendReportMail = new SendMailSlack(null, null, null, null, configInfo, mailWeekReportPath);
                bool sendSuccess = sendReportMail.SendWeeklyReportFromMongodb();
            }
        }


        private void fun()
        {
            var factory = new ConnectionFactory();
            factory.HostName = "localhost";
            factory.UserName = "guest";
            factory.Password = "guest";
            ProjectInfo buildProjectInfo = new ProjectInfo();
            while (true)
            {
                using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("CIQueues_newframework", false, false, false, null);

                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume("CIQueues_newframework", false, consumer);
                   
                        var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        string[] messageSplit = message.Split(';');
                        string projectName = messageSplit[0].Split(':')[1];
                        string projectCommitVersion = messageSplit[1].Split(':')[1];
                        string projectSubmitter = messageSplit[2].Split(':')[1];
                        string projectPushBranch = messageSplit[3].Split(':')[1].Split('/')[2];
                        //对项目进行编译
                       buildProjectInfo=HandleProject(projectName, projectCommitVersion, projectPushBranch, projectSubmitter);
                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                }

                //对象转化为json格式的数据
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(buildProjectInfo.GetType());
                MemoryStream stream = new MemoryStream();
                serializer.WriteObject(stream, buildProjectInfo);
                byte[] dataBytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(dataBytes, 0, (int)stream.Length);
                var factory_push = new ConnectionFactory();
                factory_push.HostName = "localhost";
                factory_push.UserName = "guest";
                factory_push.Password = "guest";
                using (var connection = factory_push.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare("BuildResultInfo", false, false, false, null);
                        var body = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(dataBytes));
                        channel.BasicPublish("", "BuildResultInfo", null, body);
                    }
                }
            }
         
        }


        /// <summary>
        /// 存储log信息到数据库
        /// </summary>
        /// <param name="projectInfo">项目信息</param>
        //public bool SaveInfoToDataBase(ProjectInfo projectInfo,string dbPath,string logPath)
        //{
        //    return _projectController.saveLogToDataBase(projectInfo,dbPath,logPath);
        //}

        /// <summary>
        /// 执行更新的操作
        /// </summary>
        /// <param name="projectInfo">所要更新的项目的相关信息</param>
        /// <param name="startValue">当前运行项目保存的信息</param>
        /// <returns>是否要更新</returns>
        //private Boolean UpdateProject(ProjectInfo projectInfo, string xmlPath, string buildXmlPath,out string newVersion)
        //{
        //    string updateResult = ""; //执行更新操作的结果
        //    updateLog = _svnController.Update(projectInfo.WorkDirectory, out updateResult, xmlPath);
        //    //判断版本号
        //    _latestRevision = Regex.Match(updateLog, @"revision\s[0-9]+").Value.Replace("revision", "");
        //    //List<ProjectInfo> infos = new List<ProjectInfo>();
        //    //infos = _projectController.ReadLog("/config/Projects",
        //    //    buildXmlPath);
        //    updateLog = updateLog.Replace("<br/>", "\n");
        //    string[] updateLogSplit = updateLog.Split('\n');
        //    //if (_latestRevision==projectInfo.GitVersion) { newVersion = ""; return true; }
        //    if (_latestRevision == projectInfo.GitVersion || updateLogSplit.Length <= 3) { newVersion = ""; return true; }
        //    //foreach (var info in infos)
        //    //{
        //    //    if (info.Nameproperty.Equals(projectInfo.Nameproperty))
        //    //    {
        //    //        if (info.Revision.Equals(_latestRevision) || updateLogSplit.Length <= 3)
        //    //        {
        //    //            return true;
        //    //        }
        //    //        break;
        //    //    }
        //    //}
        //    newVersion = _latestRevision.Trim();
        //    return false;
        //}

        ///// <summary>
        ///// 执行发送邮件的操作
        ///// </summary>
        ///// <param name="projectInfo">projectInfoGetLocal的信息</param>
        ///// <param name="ifSlack">是否向slack发信息</param>
        ///// <param name="ifMail">是否发送邮件</param>
        //private void SendSlackMail(ProjectInfo projectInfo, string mailPath, string xmlPath)
        //{
        //    SendMailSlack sendMailSlack = new SendMailSlack(projectInfo, mailPath, updateLog, xmlPath, infoStatXmlPath,null,null);
        //    Thread sendMail = new Thread(sendMailSlack.SendMail);
        //    Thread sendSlack = new Thread(sendMailSlack.SendSlack);
        //    //  Thread sendLogMessage = new Thread(sendMailSlack.SendLogMessage);
        //    sendMail.Start();
        //    sendSlack.Start();
        //    //  sendLogMessage.Start();
        //}
        ///// <summary>
        ///// 调用git pull操作，相等于svn更新操作
        ///// </summary>
        ///// <param name="projectInfo"></param>
        ///// <returns></returns>
        //public Boolean GitPullProject(ProjectInfo projectInfo)
        //{
        //    string updateResult = "";
        //    updateLog = _gitlabController.GitPull(projectInfo.WorkDirectory, out updateResult);

        //    if (updateLog == "Already up-to-date.")
        //    {
        //        return true;
        //    }
        //    else
        //        return false;
        //}


        protected override void OnStop()
        {
        }
        public ProjectInfo HandleProject(string projectName,string projectCommitVersion,string projectPushBranch,string projectSubmitter) {
            ProjectInfo buildProjectInfo = new ProjectInfo();
            string config = System.AppDomain.CurrentDomain.BaseDirectory;
            int p = config.LastIndexOf("\\");
            string parent = config.Substring(0, p - 25);
            string CIConfigPath = parent + "\\common\\res\\CIconfig.xml";
            string mailPath = parent + "\\common\\SendMail.html";
            string mailWeekReportPath = parent + "\\common\\WeeklyReport.html";
            string dbPath = parent + "\\common\\res\\CILog.mdb";
            string logPath = parent + "\\log\\log.txt";

            List<ProjectInfo> projectInfos = _projectController.ProjectQuery("/config/Projects", true,
                CIConfigPath);

            foreach (var projectInfo in projectInfos)
            {
                if (projectInfo.Statusproperty == "true" && projectInfo.Nameproperty == projectName)
                {
                    //编译项目，并且对要存储的数据进行更新，赋值
                    buildProjectInfo = buildProject(projectInfo, projectPushBranch, CIConfigPath);
                    buildProjectInfo.GitVersion = projectCommitVersion;
                    buildProjectInfo.Author = projectSubmitter;
                    buildProjectInfo.Branch = projectPushBranch;
                    //存储信息到数据库当中mongodb            
                    //SaveInfoToDataBase(buildProjectInfo, dbPath, logPath);
                    //ConfigInfo configInfo = _configController.ConfigQuery("config/preferences", CIConfigPath);
                    //SendMailSlack sendInfo = new SendMailSlack(buildProjectInfo, mailPath, CIConfigPath, dbPath, configInfo, mailWeekReportPath);
                    //sendInfo.SendMail();
                    //sendInfo.SendSlack();





                    break;
                }
            }
            return buildProjectInfo;

        }

        /// <summary>
        /// 收到post信息，首先对分支进行切换，然后git pull拉取本分支的最新代码，最后再进行项目的编译工作
        /// </summary>
        /// <param name="projectInfo">项目信息</param>
        /// <param name="pushBranch">操作的分支</param>
        /// <param name="CIConfigPath">项目config路径</param>
        /// <returns></returns>
        public ProjectInfo buildProject(ProjectInfo projectInfo, string pushBranch, string CIConfigPath)
        {
            //进行分支切换操作
            bool switchBranch = _gitlabController.git_checkout("git checkout " + pushBranch, projectInfo.WorkDirectory);
            //若是分支切换成功，进行项目编译
            if (switchBranch)
            {

                //获取gitlab信息
                GitInfo gitlabInfo = _configController.GitInfoQuery("config/GitLabInfo", CIConfigPath);

                Boolean gitPullResult = _gitlabController.Libgit2_GitPull(projectInfo.WorkDirectory, gitlabInfo.Username, gitlabInfo.Password, gitlabInfo.Emailaddress);
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
                    if (projectInfo.Nameproperty == "fundbook.rn") { projectInfo.WorkDirectory += "\\android"; }
                    if (projectInfo.Nameproperty == "FirstProject") { projectInfo.WorkDirectory += "\\android"; }
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
        public SendMailSlack(ProjectInfo projectInfo, string mailPath, string CIConfigParh, string mdbPath, ConfigInfo configInfo, string mailWeekReportPath)
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
                    MailInfo mailInfo = _mailController.NewEditBody(projectInfo, statisticsTimes,
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
