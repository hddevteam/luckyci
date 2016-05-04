using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using common.DTO;
using RabbitMQ.Client.Events;
using System.Web.Script.Serialization;
using common.BL;
using System.Net;
using System.Xml;
using Newtonsoft.Json;
using common.DAO;
using System.Threading;

namespace collaborator
{
    public partial class CICollaborator : ServiceBase
    {
        ConnectionFactory factory = new ConnectionFactory();
        ConfigController _configController = new ConfigController();
        ProjectController _projectController = new ProjectController();
        public CICollaborator()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            
            Thread thread = new Thread(HandleMessage);
            thread.Start();
        }

        protected override void OnStop()
        {
        } 
        /// <summary>
        /// 代码连接RabbitMQ
        /// </summary>
        public void ConnectRabbitMQ()
        {
            factory.HostName = "localhost";
            factory.UserName = "guest";
            factory.Password = "guest";
        }
        /// <summary>
        /// 将数据存储到mongoDB中，并且输出Log信息
        /// </summary>
        /// <param name="projectInfo">从Rabbit取回的信息</param>
        /// <param name="dbPath">mongoDB数据库的路径</param>
        /// <param name="logPath">Log日志的文件路径</param>
        /// <returns></returns>
        public bool SaveInfoToDataBase(ProjectInfo projectInfo,string dbPath,string logPath)
        {
            return _projectController.saveLogToDataBase(projectInfo, dbPath, logPath);
        }
        /// <summary>
        /// 处理消息事件，统一放到此函数里面，此函数放到OnStart中，减少服务启动时的时间。
        /// </summary>
        public void HandleMessage()
        {
           
            //需要当作参数传递的路径
            string config = System.AppDomain.CurrentDomain.BaseDirectory;
            int p = config.LastIndexOf("\\");
            string parent = config.Substring(0, p - 23);
            string CIConfigPath = parent + "\\common\\res\\CIconfig.xml";
            string mailPath = parent + "\\common\\SendMail.html";
            string mailWeekReportPath = parent + "\\common\\WeeklyReport.html";
            string dbPath = parent + "\\common\\res\\CILog.mdb";
            string logPath = parent + "\\log\\log.txt";

            //连接Rabbit
            ConnectRabbitMQ();
            //取回一条数据，并且当此数据处理完成，发送给Rabbit一条确认机制，之后再取回另一条数据。
            while (true)
            {
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare("BuildResultInfo", false, false, false, null);

                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume("BuildResultInfo", false, consumer);
                        BasicDeliverEventArgs ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                        var body = ea.Body;
                        var message = Encoding.UTF8.GetString(body);
                        JavaScriptSerializer Serializer = new JavaScriptSerializer();
                        ProjectInfo projectInfoData = Serializer.Deserialize<ProjectInfo>(message);
                        channel.BasicAck(ea.DeliveryTag, false);

                        //数据存储到mongoDB数据库
                        SaveInfoToDataBase(projectInfoData, dbPath, logPath);


                        //发送Email以及slack
                        ConfigInfo configInfo = _configController.ConfigQuery("config/preferences", CIConfigPath);
                        SendMailSlack sendInfo = new SendMailSlack(projectInfoData, mailPath, CIConfigPath, dbPath, configInfo, mailWeekReportPath);
                        sendInfo.SendMail();
                        sendInfo.SendSlack();
                    }
                }
            }
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
