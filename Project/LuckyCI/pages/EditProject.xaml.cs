using common.BL;
using common.DTO;
using common.TOOL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        Tools tools = new Tools();
        SvnController _svnController = new SvnController();
        ProjectController _projectController = new ProjectController();
        ConfigController _configController = new ConfigController();
        MailController _mailController = new MailController();
        private DispatcherTimer _timer = new DispatcherTimer();
        private static string _latestRevision;
        
        public EditProject()
        {
            InitializeComponent();
            InitPage();
            _timer.Tick += timer_Tick;
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Start();
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
                                //将当前运行项目及运行状态存储
                                Dictionary<string, string> startValue = new Dictionary<string, string>();
                                startValue.Add("projectName", projectInfo.Nameproperty);
                                startValue.Add("result", "running");
                                _projectController.ModifyProject(startValue, null, "../../../common/res/LastestInfo.xml", "lastest");
                                //更新项目,若版本号未变则无需编译否则编译
                                Boolean updateResult = UpdateProject(projectInfo);
                                //如果无需编译,则结果会滚到改变前的版本
                                if (updateResult)
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
                                    //编译项目                                                                       
                                    ProjectInfo buildFinishedInfo = BuildProject(projectInfo);
                                    //存储编译信息
                                    SaveInfo(buildFinishedInfo);
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        InitPage();
                                    }));
                                    //先从本地svn完善项目信息后自动发送邮件
                                    ProjectInfo projectInfoGetLocal = _svnController.GetLocalInfo(buildFinishedInfo);
                                    SendSlackMail(projectInfoGetLocal, projectInfo.IfSlack, projectInfo.IfMail);
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
                Thread.Sleep(updateInterval*60*1000);
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
            log = _projectController.Build(projectInfo.Buildcommand, projectInfo.Workdirectory,
                out buildResult, out error,out time);
            projectInfo.Duration = time;
            //projectInfo.Duration = Regex.Match(log, "(?<=Total time:).*?(?=secs)").Value == ""
            //    ? ""
            //    : (Regex.Match(log, "(?<=Total time:).*?(?=secs)").Value + " secs");
            projectInfo.Revision = _latestRevision;
            projectInfo.Log = (configInfo.StandarOutput == "true")
                ? ("\n" + buildResult + "\n" + log + "\n" + error)
                : ("\n" + buildResult + "\n" + projectInfo.Duration + "\n" + error);
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
            _projectController.ModifyProject(lastProject,null,
                "../../../common/res/LastestInfo.xml","lastest");
        }

        /// <summary>
        /// 执行更新的操作
        /// </summary>
        /// <param name="projectInfo">所要更新的项目的相关信息</param>
        /// <param name="startValue">当前运行项目保存的信息</param>
        /// <returns>是否要更新</returns>
        private Boolean UpdateProject(ProjectInfo projectInfo)
        {
            string updateLog = "";     //执行更新操作获取的日志信息
            string updateResult = ""; //执行更新操作的结果
            updateLog = _svnController.Update(projectInfo.Workdirectory, out updateResult, "../../../common/res/CIConfig.xml");
            //判断版本号
            _latestRevision = Regex.Match(updateLog, @"revision\s[0-9]+").Value.Replace("revision", "");
            string[] updateLogSplit = updateLog.Split('\n');
            List<ProjectInfo> infos = new List<ProjectInfo>();
            infos = _projectController.ReadLog("/config/Projects",
                "../../../common/res/BuildResultLogs.xml");
            foreach (var info in infos)
            {
                if (info.Nameproperty.Equals(projectInfo.Nameproperty))
                {
                    if (info.Revision.Equals(_latestRevision)||updateLogSplit.Length<=3)
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
        private void SendSlackMail(ProjectInfo projectInfo, string ifSlack, string ifMail)
        {
            if (projectInfo.IfMail == "true")
            {
                MailInfo mailInfo = _mailController.EditBody(projectInfo,
                    "../../../common/SendMail.html");
                _mailController.SendMail(mailInfo);
            }
            try
            {
                if (ifSlack == "true")
                {
                    WebClient webClient = new WebClient();
                    string slackBody = projectInfo.SlackContent;
                    slackBody = slackBody.Replace("projectName", projectInfo.Nameproperty);
                    slackBody = slackBody.Replace("versionNum", projectInfo.Revision);
                    string[] contentItem = slackBody.Split('#');
                    string slackEmoji = projectInfo.Result == "successful"
                        ? contentItem[1].Split(':')[0]
                        : contentItem[1].Split(':')[1];
                    contentItem[contentItem.Length - 2] = projectInfo.Result == "successful"
                        ? contentItem[contentItem.Length - 2].Split(':')[0]
                        : contentItem[contentItem.Length - 2].Split(':')[1];
                    slackBody = "";
                    for (int i = 2; i < contentItem.Length - 1; i++)
                    {
                        slackBody += contentItem[i];
                    }
                    string userName = projectInfo.SlackUser=="#author#"?projectInfo.Author.Remove(0, 3)
                    :projectInfo.SlackUser.Replace("#","");
                    //将传输信息写入json
                    var payLoad = new
                    {
                        channel = projectInfo.SlackChannel,
                        text = ":" + slackEmoji + ":" + slackBody,
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
    }
}