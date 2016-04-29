using common.BL;
using common.DTO;
using common.TOOL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace LuckyCI.pages
{
    /// <summary>
    /// EditProject.xaml 的交互逻辑
    /// </summary>
    public partial class EditProject
    {
        //是否发送一周报告
        bool _notSendReport = true;
        Tools tools = new Tools();
        GitController _gitlabController = new GitController();
        SvnController _svnController = new SvnController();
        ProjectController _projectController = new ProjectController();
        ConfigController _configController = new ConfigController();
        MailController _mailController = new MailController();
        private DispatcherTimer _timer = new DispatcherTimer();

        private DispatcherTimer _sendWeeklyReportTimer = new DispatcherTimer();

        private static string _latestRevision;
        private XmlNodeList _slackPeople;
        private static string updateLog;  //执行更新操作获取的日志信息
        public EditProject()
        {
            InitializeComponent();
            InitPage();
            _timer.Tick += timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();
            _slackPeople = _configController.AcquireSlackPeople("/config/SlackPeople/People", "../../../common/res/CIConfig.xml");

            _sendWeeklyReportTimer.Tick += _sendWeeklyReportTimer_Tick;
            _sendWeeklyReportTimer.Interval = TimeSpan.FromSeconds(10);
            _sendWeeklyReportTimer.Start();
        }
        //计时器：发送一周邮件报告，每周一早上八点半进行发送
        private void _sendWeeklyReportTimer_Tick(object sender, EventArgs e)
        {
            DayOfWeek today = DateTime.Now.DayOfWeek;
            bool orMonday = (today.ToString() == "Monday");
            bool timeNow = (DateTime.Now.ToShortTimeString() == "8:30");
            if (orMonday == false) { _notSendReport = true; }
            if (orMonday && timeNow && _notSendReport)
            {
                //发送一周总结报告邮件
                SendMailSlack sendReportMail = new SendMailSlack(null, "../../../common/WeeklyReport.html", null, null);
                bool sendSuccess = sendReportMail.SendWeeklyReport();
                _notSendReport = false;
                //进行数据清空

                //进行成员数据清空
                _projectController.ClearTimesAfterSendReport("config/Member", "../../../common/res/InfoStatics.xml", null, null);
                //Dictionary<string, string> clearMemberZero = new Dictionary<string, string>();
                //clearMemberZero.Add("Week", "0");
                //clearMemberZero.Add("Success", "0");
                //clearMemberZero.Add("Failure", "0");
                //_projectController.ClearTimesAfterSendReport("config/Member", "../../../common/res/InfoStatics.xml", clearMemberZero, "memberTimes");
                //进行总提交次数清空
                Dictionary<string, string> clearWeekTotalZero = new Dictionary<string, string>();
                clearWeekTotalZero.Add("CommitTimes", "0");
                clearWeekTotalZero.Add("BuildTimes", "0");
                clearWeekTotalZero.Add("BuildSuccessTimes", "0");
                clearWeekTotalZero.Add("BuildFailedTimes", "0");
                _projectController.ClearTimesAfterSendReport("config/WeekTotal", "../../../common/res/InfoStatics.xml", clearWeekTotalZero, "totalTimes");
                //projects进行清空
                _projectController.ClearTimesAfterSendReport("config/Projects", "../../../common/res/InfoStatics.xml", null, null);
            }

        }

        /// <summary>
        /// 初始化底栏,显示上次编译信息
        /// </summary>
        private void InitPage()
        {
            try
            {
                string image;
                ProjectInfo projectInfo = _projectController.ProjectQuery("config/lastest", false, "../../../common/res/LastestInfo.xml").First();
                ProjectName.Text = projectInfo.Nameproperty;
                Datetime.Text = projectInfo.BuildTime;
                BuildDuration.Text = projectInfo.Duration;
                var flag = projectInfo.Result;
                switch (flag)
                {
                    case "successful":
                        image = "../images/dot_green.png";
                        break;
                    case "running":
                        image = "../images/dot_yellow.png";
                        break;
                    case "failed":
                        image = "../images/dot_red.png";
                        break;
                    default:
                        image = "../images/dot_black.png";
                        break;
                }
                ImgSource.Source = new BitmapImage(new Uri(image, UriKind.Relative));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// 设置计时器触发的执行事件
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            //如果运行的是windows服务则刷新当前运行项目状态
            ConfigInfo serviceInfo = _configController.ConfigQuery("config/preferences",
                "../../../common/res/CIConfig.xml");
            //获取所有的项目数量
            List<ProjectInfo> projectInfoCount = _projectController.ProjectQuery("/config/Projects", true,
                   "../../../common/res/CIConfig.xml");
            string mailPath = "../../../common/SendMail.html";
            string xmlPath = "../../../common/res/CIConfig.xml";
            if (serviceInfo.ServiceSwitch == "service")
            {
                InitPage();
            }
            else
            {
                _timer.Stop();
                List<ProjectInfo> projectInfos = _projectController.ProjectQuery("/config/Projects", true,
                    "../../../common/res/CIConfig.xml");
                Task buildTask = new Task(() =>
                {
                    foreach (var projectInfo in projectInfos)
                    {
                        if (projectInfo.Statusproperty == "true")
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                ProjectName.Text = projectInfo.Nameproperty;
                                ImgSource.Source =
                                    new BitmapImage(new Uri("../images/dot_yellow.png", UriKind.Relative));
                            }));
                            //保存修改前的版本以便回滚
                            ProjectInfo lastestInfo = _projectController.ProjectQuery("config/lastest", false,
                                "../../../common/res/LastestInfo.xml").First();
                            ////将当前运行项目及运行状态存储
                            Dictionary<string, string> startValue = new Dictionary<string, string>();
                            startValue.Add("projectName", projectInfo.Nameproperty);
                            startValue.Add("result", "running");
                            _projectController.ModifyProject(startValue, null, "../../../common/res/LastestInfo.xml", "lastest");
                            //更新项目,若版本号未变则无需编译否则编译
                            //svn更行操作 svn update
                            //Boolean updateResult = UpdateProject(projectInfo);
                            //git更新操作 git pull
                            //如果无需编译,则结果会滚到改变前的版本
                            Boolean updateResult = GitPullProject(projectInfo);

                            //  测试用的语句
                            if (updateResult)
                            //test code
                            //if (false)
                            {
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    Dictionary<string, string> revertLastInfo = new Dictionary<string, string>();
                                    revertLastInfo.Add("projectName", lastestInfo.Nameproperty);
                                    revertLastInfo.Add("result", lastestInfo.Result);
                                    _projectController.ModifyProject(revertLastInfo, null,
                                        "../../../common/res/LastestInfo.xml", "lastest");
                                    InitPage();
                                }));
                            }
                            else
                            {
                                //发送更新信息
                                //获取上次提交者名字
                                string gitlabLastAuthor = _gitlabController.GirLogFundAuthor(projectInfo.WorkDirectory).Trim();
                                projectInfo.Author = gitlabLastAuthor;
                                //ProjectInfo projectInfoGetLocal = _svnController.GetLocalInfo(projectInfo);
                                //projectInfoGetLocal.Revision = _latestRevision;

                                //检查提交过信息的人员名单中是否有此author
                                //若是有则进行加一处理
                                //若是没有则进行创建，然后加一
                                List<ProjectInfo> infoStaticName = _projectController.ProjectQuery("/config/Member", true, "../../../common/res/InfoStatics.xml");
                                bool notCommitName = true;
                                foreach (ProjectInfo commitName in infoStaticName)
                                {
                                    if (commitName.Nameproperty == projectInfo.Author)
                                    {
                                        notCommitName = false;
                                        break;
                                    }
                                }
                                if (infoStaticName.Count == 0 || notCommitName)
                                {
                                    var memberChildNodes = new Dictionary<string, string>();
                                    var memberProperty = new Dictionary<string, string>();
                                    memberChildNodes.Add("Week", "0");
                                    memberChildNodes.Add("Success", "0");
                                    memberChildNodes.Add("Failure", "0");
                                    memberProperty.Add("Name", projectInfo.Author);
                                    _projectController.AddMember(memberChildNodes, memberProperty, "../../../common/res/InfoStatics.xml");
                                }

                                Dictionary<string, string> setValue = new Dictionary<string, string>();
                                setValue.Add("Name", gitlabLastAuthor);
                                _projectController.CommitStat("update", setValue, "config/Member", "../../../common/res/InfoStatics.xml");
                                //检查infoStatics文件夹中有没有提交的项目，若是有则进行相对应的节点数值加1处理，若是没有，则进行创建，并进行初始化
                                List<ProjectInfo> checkProjectInfos = _projectController.ProjectQuery("/config/Projects", true, "../../../common/res/InfoStatics.xml");
                                //进行创建
                                bool whetherCreateAtInfoStatics = true;
                                foreach (ProjectInfo proName in checkProjectInfos)
                                {
                                    if (proName.Nameproperty == projectInfo.Nameproperty)
                                    {
                                        whetherCreateAtInfoStatics = false;
                                        break;
                                    }
                                }

                                if (checkProjectInfos.Count == 0 || whetherCreateAtInfoStatics)
                                {
                                    var childNodes = new Dictionary<string, string>();
                                    var property = new Dictionary<string, string>();
                                    childNodes.Add("Commit", "1");
                                    childNodes.Add("Build", "0");
                                    childNodes.Add("Success", "0");
                                    childNodes.Add("Failed", "0");
                                    property.Add("Name", projectInfo.Nameproperty);
                                    _projectController.AddProject(childNodes, property, "../../../common/res/InfoStatics.xml");
                                }
                                //进行加1处理
                                else
                                {
                                    Dictionary<string, string> projectName = new Dictionary<string, string>();
                                    projectName.Add("Name", projectInfo.Nameproperty);
                                    _projectController.CommitStat("projectsCommit", projectName, "config/Projects", "../../../common/res/InfoStatics.xml");
                                }

                                //if (updateLog.Contains("Node remains in conflict"))
                                //{
                                //    SendMailSlack sendMailSlack = new SendMailSlack(projectInfo, null, updateLog, xmlPath);
                                //    Thread sendLogMessage = new Thread(sendMailSlack.SendConflictsMessage);
                                //    sendLogMessage.Start();
                                //}
                                //else
                                //{
                                    //SendMailSlack sendMailSlack = new SendMailSlack(projectInfo, null, updateLog, xmlPath);
                                    //Thread sendLogMessage = new Thread(sendMailSlack.SendLogMessage);
                                    //sendLogMessage.Start();

                                    //编译项目                                                                       
                                    ProjectInfo buildFinishedInfo = BuildProject(projectInfo);
                                    //存储编译信息
                                    SaveInfo(buildFinishedInfo);
                                    if (buildFinishedInfo.Result == "successful")
                                    {
                                        _projectController.CommitStat("success", setValue, "config/Member", "../../../common/res/InfoStatics.xml");
                                        //对于所编译的项目加1操作
                                        Dictionary<string, string> projectName = new Dictionary<string, string>();
                                        projectName.Add("Name", projectInfo.Nameproperty);
                                        _projectController.CommitStat("projectSuccess", projectName, "config/Projects", "../../../common/res/InfoStatics.xml");
                                    }
                                    else if (buildFinishedInfo.Result == "failed")
                                    {
                                        _projectController.CommitStat("failure", setValue, "config/Member", "../../../common/res/InfoStatics.xml");
                                        Dictionary<string, string> projectName = new Dictionary<string, string>();
                                        projectName.Add("Name", projectInfo.Nameproperty);
                                        _projectController.CommitStat("projectFailed", projectName, "config/Projects", "../../../common/res/InfoStatics.xml");
                                    }
                                    Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    InitPage();
                                }));
                                    //先从本地svn完善项目信息后自动发送邮件
                                    //  ProjectInfo projectInfoGetLocal = _svnController.GetLocalInfo(buildFinishedInfo);
                                    SendSlackMail(buildFinishedInfo, mailPath, xmlPath);
                                //}
                            }
                        }
                    }
                });

                buildTask.Start();
                Task nextBuild = buildTask.ContinueWith((t =>
                {

                    XmlNodeList xmlNodeLost = _configController.FindConfigInfo("/config/preferences/UpdateInterval",
                        "../../../common/res/CIConfig.xml");
                    int updateInterval = int.Parse(xmlNodeLost[0].InnerText);
                    Thread.Sleep(updateInterval * 60 * 1000);
                    _timer.Start(); //遍历编译完成，重新启动计时器
                }));
            }
        }

        /// <summary>
        /// 执行编译项目的操作
        /// </summary>
        /// <param name="projectInfo">当前编译的项目</param>
        /// <returns></returns>
        private ProjectInfo BuildProject(ProjectInfo projectInfo)
        {
            string buildResult; //存储编译是否成功
            string error;          //存储编译的日志文件
            string log;              //存储编译的所有信息
            string time;            //存储运行的时间
            ConfigInfo configInfo = _configController.ConfigQuery("config/preferences",
                "../../../common/res/CIConfig.xml");
            projectInfo.BuildTime = DateTime.Now.ToString();
            log = _projectController.Build(projectInfo.BuildCommand, projectInfo.WorkDirectory,
                out buildResult, out error, out time);
            projectInfo.Duration = time;
            //projectInfo.Duration = Regex.Match(log, "(?<=Total time:).*?(?=secs)").Value == ""
            //    ? ""
            //    : (Regex.Match(log, "(?<=Total time:).*?(?=secs)").Value + " secs");
            projectInfo.Revision = _latestRevision;
            projectInfo.Log = (configInfo.StandarOutput == "true")
                ? ("\n" + log + "\n" + error)
                : ("\n" + error);
            projectInfo.Result = buildResult;
            return projectInfo;
        }

        /// <summary>
        /// 存储编译的信息
        /// </summary>
        /// <param name="projectInfo">当前编译的项目</param>
        private void SaveInfo(ProjectInfo projectInfo)
        {
            Dictionary<string, string> lastProject = new Dictionary<string, string>();
            Dictionary<string, string> property = new Dictionary<string, string>();
            property.Add("Name", projectInfo.Nameproperty);
            //累加Log,存储在新的Log节点并执行添加属性的操作
            _projectController.SaveLog(projectInfo, property,
                "../../../common/res/BuildResultLogs.xml", "Projects");
            //修改存储最近完成项目信息的xml文件
            lastProject.Add("projectName", projectInfo.Nameproperty);
            lastProject.Add("buildTime", projectInfo.BuildTime);
            lastProject.Add("duration", projectInfo.Duration);
            lastProject.Add("result", projectInfo.Result);
            _projectController.ModifyProject(lastProject, null,
                "../../../common/res/LastestInfo.xml", "lastest");
        }

        /// <summary>
        /// 执行更新的操作
        /// </summary>
        /// <param name="projectInfo">所要更新的项目的相关信息</param>
        /// <param name="startValue">当前运行项目保存的信息</param>
        /// <returns>是否要更新</returns>
        private Boolean UpdateProject(ProjectInfo projectInfo)
        {
            string updateResult = ""; //执行更新操作的结果          
            updateLog = _svnController.Update(projectInfo.WorkDirectory, out updateResult, "../../../common/res/CIConfig.xml");
            //判断版本号
            _latestRevision = Regex.Match(updateLog, @"revision\s[0-9]+").Value.Replace("revision", "");
            updateLog = updateLog.Replace("<br/>", "\n");
            string[] updateLogSplit = updateLog.Split('\n');
            List<ProjectInfo> infos = new List<ProjectInfo>();
            infos = _projectController.ReadLog("/config/Projects",
                "../../../common/res/BuildResultLogs.xml");
            foreach (var info in infos)
            {
                if (info.Nameproperty.Equals(projectInfo.Nameproperty))
                {
                    if (info.Revision.Equals(_latestRevision) || updateLogSplit.Length <= 3)
                    {
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// 执行发送邮件的操作
        /// </summary>
        /// <param name="projectInfo">projectInfoGetLocal的信息</param>
        /// <param name="ifSlack">是否向slack发信息</param>
        /// <param name="ifMail">是否发送邮件</param>
        private void SendSlackMail(ProjectInfo projectInfo, string mailPath, string xmlPath)
        {
            SendMailSlack sendMailSlack = new SendMailSlack(projectInfo, mailPath, updateLog, xmlPath);
            Thread sendMail = new Thread(sendMailSlack.SendMail);
            Thread sendSlack = new Thread(sendMailSlack.SendSlack);
            //  Thread sendLogMessage = new Thread(sendMailSlack.SendLogMessage);
            sendMail.Start();
            sendSlack.Start();
            //  sendLogMessage.Start();
        }

        /// <summary>
        /// 执行跳转到PreferencesPage.xaml页面
        /// </summary>
        private void btnToPreferences_Click(object sender, RoutedEventArgs e)
        {
            tools.PageSource(FramePage, "PreferencesPage.xaml");
        }

        /// <summary>
        /// 执行跳转到AddPage.xaml页面
        /// </summary>
        private void btnToAdd_Click(object sender, RoutedEventArgs e)
        {
            tools.PageSource(FramePage, "AddPage.xaml");
        }

        /// <summary>
        /// 执行跳转到AllProjectsPage.xaml页面
        /// </summary>
        private void btnToAllProjects_Click(object sender, RoutedEventArgs e)
        {
            tools.PageSource(FramePage, "AllProjectsPage.xaml");
        }

        /// <summary>
        ///     执行跳转到Help.xaml页面
        /// </summary>
        private void btnToHelp_Click(object sender, RoutedEventArgs e)
        {
            tools.PageSource(FramePage, "Help.xaml");
        }
        /// <summary>
        /// 调用git pull操作，相等于svn更新操作
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <returns></returns>
        public Boolean GitPullProject(ProjectInfo projectInfo)
        {
            string updateResult = "";
            updateLog = _gitlabController.GitPull(projectInfo.WorkDirectory, out updateResult);

            if (updateLog == "Already up-to-date.")
            {
                return true;
            }
            else
                return false;
        }

        public class SendMailSlack
        {
            ConfigController _configController = new ConfigController();
            MailController _mailController = new MailController();
            ProjectController _projectController = new ProjectController();
            private ProjectInfo projectInfo;
            private string mailPath;
            private XmlNodeList _slackPeople;
            private XmlNodeList _mailPeople;
            private string updateNewLog;
            private Dictionary<string, Dictionary<string, string>> allStatics;
            private Dictionary<string, Dictionary<string, string>> allProjectStatics;
            private Dictionary<string, string> weekTotalData;
            /// <summary>
            /// 构造函数，在线程启动之前，进行赋值
            /// </summary>
            /// <param name="projectInfo">项目信息</param>
            /// <param name="ifSlack">是否发送slack</param>
            /// <param name="ifMail">是否发送邮件</param>
            public SendMailSlack(ProjectInfo projectInfo, string mailPath, string updateLog, string xmlParh)
            {
                if (xmlParh != null)
                {
                    _slackPeople = _configController.AcquireSlackPeople("/config/SlackPeople/People", xmlParh);
                    _mailPeople = _configController.AcquireSlackPeople("/config/MailPeople/People",xmlParh);
                }
                allStatics = _projectController.GetStatData("config/Member", "../../../common/res/InfoStatics.xml");
                allProjectStatics = _projectController.GetProjectData("config/Projects", "../../../common/res/InfoStatics.xml");
                weekTotalData = _projectController.GetTotal("config/WeekTotal", "../../../common/res/InfoStatics.xml");
                this.projectInfo = projectInfo;
                this.mailPath = mailPath;
                this.updateNewLog = updateLog;
            }
            /// <summary>
            /// 发送邮件
            /// </summary>
            public void SendMail()
            {
                string shortName = "";
                //修改发送邮件名字 ，改为简写
                foreach (XmlNode people in _mailPeople)
                {
                    if (projectInfo.Author == people.Attributes["Name"].Value)
                    {
                        shortName = people.InnerText;
                        break;
                    }
                }

                try
                {
                    if (projectInfo.IfMail == "true")
                    {
                        MailInfo mailInfo = _mailController.EditBody(projectInfo, allStatics,
                            mailPath,shortName);
                        _mailController.SendMail(mailInfo);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            }

            /// <summary>
            /// 发送slack
            /// </summary>
            /// <returns></returns>
            public void SendSlack()
            {
                try
                {
                    if (projectInfo.IfSlack == "true" && projectInfo.SelectResult == "true")
                    {
                        int[] allSuccess = new int[allStatics.Count];
                        int[] allFailure = new int[allStatics.Count];
                        int index = 0;
                        int selectSuccess = 0, selectError = 0, selectWeek = 0;
                        foreach (var key in allStatics.Keys)
                        {
                            allSuccess[index] = Int32.Parse(allStatics[key]["Success"]);
                            allFailure[index] = Int32.Parse(allStatics[key]["Failure"]);
                            if (key == projectInfo.Author)
                            {
                                selectWeek = Int32.Parse(allStatics[key]["Week"]);
                                selectSuccess = Int32.Parse(allStatics[key]["Success"]);
                                selectError = Int32.Parse(allStatics[key]["Failure"]);
                            }
                            index++;
                        }
                        Array.Sort(allSuccess);
                        Array.Sort(allFailure);
                        Array.Reverse(allSuccess);
                        Array.Reverse(allFailure);
                        WebClient webClient = new WebClient();
                        webClient.Encoding = Encoding.GetEncoding("utf-8");
                        string slackBody = projectInfo.SlackContent; 
                         slackBody = slackBody.Replace("projectName", projectInfo.Nameproperty);
                        slackBody = slackBody.Replace("revision", "");
                        slackBody = slackBody.Replace("versionNum","");
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
                            slackBody += ". This week " + projectInfo.Author + " build " + selectWeek.ToString() +
                                         " times, " + selectSuccess.ToString() + " passed, " + selectError.ToString() + " failed.";
                        }

                        else if (projectInfo.Result == "failed")
                        {
                            slackBody += "This week " + projectInfo.Author + " build " + selectWeek.ToString() +
                                          " times, " + selectSuccess.ToString() + " passed, " + selectError.ToString() + " failed.";
                        }
                        string userName = projectInfo.SlackUser == "#author#"
                            ? projectInfo.Author
                            : projectInfo.SlackUser.Replace("#", "");
                        //将传输信息写入json
                        //foreach (XmlNode people in _slackPeople)
                        //{
                        //    if (userName == people.Attributes["Name"].Value)
                        //    {
                        //        userName = people.InnerText;
                        //        break;
                        //    }
                        //}
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
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);

                }

            }

            /// <summary>
            /// 发送最近版本的log信息
            /// </summary>
            public void SendLogMessage()
            {
                try
                {
                    if (projectInfo.IfSlack == "true")
                    {
                        int[] allUpdate = new int[allStatics.Count];
                        int index = 0;
                        int selectWeek = 0;
                        foreach (var key in allStatics.Keys)
                        {
                            allUpdate[index] = Int32.Parse(allStatics[key]["Week"]);
                            if (key == projectInfo.Author)
                            {
                                selectWeek = Int32.Parse(allStatics[key]["Week"]);
                            }
                            index++;
                        }
                        Array.Sort(allUpdate);
                        Array.Reverse(allUpdate);
                        WebClient webClient = new WebClient();
                        webClient.Encoding = Encoding.GetEncoding("utf-8");
                        string userName = projectInfo.SlackUser == "#author#"
                            ? projectInfo.Author
                            : projectInfo.SlackUser.Replace("#", "");
                        string logMessage = "";
                        if (projectInfo.SelectCommit == "true")
                        {
                            logMessage = projectInfo.Nameproperty + " revision " + projectInfo.Revision + ": " +
                                         projectInfo.LogMessage;
                        }
                        logMessage += ". This week " + projectInfo + " commit " + selectWeek +
                                      " times.";
                        //将传输信息写入json
                        foreach (XmlNode people in _slackPeople)
                        {
                            if (userName == people.Attributes["Name"].Value)
                            {
                                userName = people.InnerText;
                                break;
                            }
                        }
                        //添加update信息
                        if (projectInfo.SelectUpdate == "true")
                        {
                            string[] update = updateNewLog.Split('\n');
                            for (int i = 1; i < update.Length - 2; i++)
                            {
                                logMessage += "\n" + update[i];
                            }
                        }
                        //发送信息到slack
                        var payLoad = new
                        {
                            channel = projectInfo.SlackChannel,
                            text = ":" + "triangular_flag_on_post" + ":" + logMessage,
                            username = userName
                        };
                        string json = JsonConvert.SerializeObject(payLoad);
                        string slackResult = webClient.UploadString(projectInfo.SlackUrl, json);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            /// <summary>
            /// 树冲突时发送警告信息
            /// </summary>
            public void SendConflictsMessage()
            {
                try
                {
                    if (projectInfo.IfSlack == "true")
                    {
                        WebClient webClient = new WebClient();
                        webClient.Encoding = Encoding.GetEncoding("utf-8");
                        string userName = projectInfo.SlackUser == "#author#" ? projectInfo.Author.Remove(0, 3)
                        : projectInfo.SlackUser.Replace("#", "");
                        // string logMessage = projectInfo.Nameproperty + " revision " + projectInfo.Revision + ": " + projectInfo.LogMessage;
                        string logMessage = "Warning:\n";
                        //将传输信息写入json
                        foreach (XmlNode people in _slackPeople)
                        {
                            if (userName == people.Attributes["Name"].Value)
                            {
                                userName = people.InnerText;
                                break;
                            }
                        }
                        //添加update信息
                        string[] update = updateNewLog.Split('\n');
                        for (int i = 1; i < update.Length - 4; i++)
                        {
                            logMessage += "\n" + update[i];
                        }
                        logMessage += update[update.Length - 4] + "\n" + update[update.Length - 3] + update[update.Length - 2];
                        //发送信息到slack
                        var payLoad = new
                        {
                            channel = projectInfo.SlackChannel,
                            text = ":" + "x" + ":" + logMessage,
                            username = userName
                        };
                        string json = JsonConvert.SerializeObject(payLoad);
                        string slackResult = webClient.UploadString(projectInfo.SlackUrl, json);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);

                }
            }
            /// <summary>
            /// 发送一周邮件报告
            /// </summary>
            public bool SendWeeklyReport()
            {
                return _mailController.SendReport(mailPath, allStatics, allProjectStatics, weekTotalData);
            }
        }
    }
}
