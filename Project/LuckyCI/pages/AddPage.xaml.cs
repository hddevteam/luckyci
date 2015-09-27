using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using common.DTO;
using common.BL;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Navigation;
using System.Linq;
using System.Xml;

namespace LuckyCI.pages
{
    /// <summary>
    /// AddPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddPage : Page
    {
        ConfigController _configController = new ConfigController();
        NavigationService _navigationService;
        SvnController _svnController = new SvnController();
        ProjectInfo _projectInfo = new ProjectInfo();
        DispatcherTimer _timer = new DispatcherTimer();
        static string log;//运行日志                   
        static string duration;//编译用时  
        static string revision=null;//版本号   
        bool updateRevision=true;//判断版本更新
        string err = "";//错误信息输出
        private int index;//传过来要进行操作的项目的序号
        static ProjectInfo modifyProject;

        /// <summary>
        /// 构造函数，初始化
        /// </summary>
        public AddPage()
        {
            InitializeComponent();
            Loaded += InitPage;
            runProject.Click += btnSave_Click;
            runProject.Click += btnCheck_Click;
            runProject.Click += btnUpdate_Click;
            runProject.Click += btnBuild_Click;
            runProject.Click += btnSendMail_Click;
            _timer.Tick += timer_Tick;//注册计时器
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Start(); //启动计时器
        }

        /// <summary>
        /// 初始化事件,获取当前的uri路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitPage(object sender, RoutedEventArgs e)
        {
            try
            {
                quesMark.ToolTip = "1.##之间的内容若包含:则:左边为成功的显示右边为失败" +
                                   "\n2.smile&smiling_imp为emoji对照英文可按需修改" +
                                   "\n3.可按需添加语句";
                ProjectController projectController = new ProjectController();
                List<ProjectInfo> list = projectController.ProjectQuery("/config/lastest", false, "../../../common/res/LastestInfo.xml");
                lastRe.Text = list[0].Result;
                index = int.Parse(list[0].Index);
                if (index != -1)
                {
                    List<ProjectInfo> projectInfos = projectController.ProjectQuery("/config/Projects", true,
                "../../../common/res/CIConfig.xml");
                    modifyProject = projectInfos[index];
                    if (modifyProject != null)
                    {
                        respoPath.Text = modifyProject.Repositorypath;
                        projectName.Text = modifyProject.Nameproperty;
                        Workspace.Text = modifyProject.Workdirectory;
                        Mailto.Text = modifyProject.MailTo;
                        Buildcomand.Text = modifyProject.Buildcommand;
                        CheckMail.IsChecked = Convert.ToBoolean(modifyProject.IfMail);
                        CheckSlack.IsChecked = Convert.ToBoolean(modifyProject.IfSlack);
                        Host.Text = modifyProject.MailHost;
                        Username.Text = modifyProject.UserName;
                        Password.Text = modifyProject.Password;
                        SlackUrl.Text = modifyProject.SlackUrl;
                        SlackChannel.Text = modifyProject.SlackChannel;
                        buildRe.Text = modifyProject.Result;
                    }
                    Dictionary<string, string> initIndex = new Dictionary<string, string>();
                    initIndex.Add("index", "-1");
                    projectController.ModifyProject(initIndex, null, "../../../common/res/LastestInfo.xml", "lastest");
                }
              
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        /// <summary>
        /// 计时器触发函数timer_Tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {         
            string datePath = "config/lastest";
            ProjectController projectController = new ProjectController();
            List<ProjectInfo> lastestInfos = new List<ProjectInfo>();
            lastestInfos = projectController.ProjectQuery(datePath, false, "../../../common/res/LastestInfo.xml");
            if (lastestInfos[0].Result != "running")
            {
                lastRe.Text = lastestInfos[0].Result;
            }
            buildRe.Text = lastestInfos[0].Result;
        }

        /// <summary>
        /// 执行开启选中的工程节点,使之处于激活状态
        /// </summary>
        private void btnActive_Click(object sender, RoutedEventArgs e)
        {
            ProjectController projectController = new ProjectController();
            var name = new Dictionary<string, string>();
            var property = new Dictionary<string, string>();
            name.Add("Name",projectName.Text);
            property.Add("Status", "true");
            MessageBox.Show("Active " + projectController.ActiveClose(name, property, "Projects", "../../../common/res/CIConfig.xml"));
        }

        /// <summary>
        /// 执行关闭选中的工程节点,使之处于未激活状态
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            ProjectController projectController = new ProjectController();
            var name = new Dictionary<string, string>();
            var property = new Dictionary<string, string>();
            name.Add("Name",projectName.Text);
            property.Add("Status", "false");           
            MessageBox.Show("Close " + projectController.ActiveClose(name, property, "Projects", "../../../common/res/CIConfig.xml"));
        }

        /// <summary>
        ///  执行删除选中的工程节点
        /// </summary>
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            ProjectController projectController = new ProjectController();
            var xmlConfigPath = new[] { "../../../common/res/CIConfig.xml", "../../../common/res/BuildResultLogs.xml" };
            var result = projectController.DeleteProject("/config/Projects", projectName.Text, xmlConfigPath);
            MessageBox.Show("Delete " + result);
            if (result == "successful")
            {
                index = new int();
                modifyProject = null;
                respoPath.Text = "";
                projectName.Text = "";
                Workspace.Text = "";
                Mailto.Text = "";
                Buildcomand.Text = "";
                CheckMail.IsChecked = false;
                CheckSlack.IsChecked = false;
                Host.Text = "";
                Username.Text = "";
                Password.Text = "";
                SlackUrl.Text = "";
                SlackChannel.Text = "";
                buildRe.Text = "";
            }
        }

        /// <summary>
        /// 检索文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {

            SvnInfo svnInfo = new SvnInfo();
            svnInfo.Repositorypath = respoPath.Text;
            svnInfo.Workdirectory = Workspace.Text;
            string checkResult;
            string filePath = Workspace.Text;
            if (respoPath.Text == "" || projectName.Text == "" || Workspace.Text == "" || Host.Text == "" ||
             Mailto.Text == "" || Buildcomand.Text == "")
            {
                MessageBox.Show("请先填写完整的信息！");
            }
            else if (check.IsChecked.ToString() == "False")
            {
                if (Directory.Exists(filePath))
                {
                    MessageBox.Show("项目已存在,如果要覆盖请勾选force overwrite");
                }
                else
                {
                    logs.Text = "检索信息：" + _svnController.CheckOut(svnInfo.Repositorypath, svnInfo.Workdirectory, out checkResult, "../../../common/res/CIConfig.xml") + "\n" + this.logs.Text + "\n";
                    currentRe.Text = checkResult;
                }
            }
            else
            {
                logs.Text = "检索信息：" + _svnController.CheckOut(svnInfo.Repositorypath, svnInfo.Workdirectory, out checkResult, "../../../common/res/CIConfig.xml") + "\n" + this.logs.Text + "\n";
                currentRe.Text = checkResult;
            }
        }

        /// <summary>
        /// 更新文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            SvnInfo svnInfo = new SvnInfo();
            svnInfo.Workdirectory = Workspace.Text;
            string updateResult;
            string updateLog;
            if (respoPath.Text == "" || 
                projectName.Text == "" || 
                Workspace.Text == "" ||              
                Mailto.Text == "" || 
                Host.Text == "" ||
                Buildcomand.Text == "")
            {
                MessageBox.Show("请先填写完整的信息！");
            }
            else
            {
                updateLog = _svnController.Update(svnInfo.Workdirectory, out updateResult, "../../../common/res/CIConfig.xml");
                logs.Text="更新信息："+updateLog+"\n"+this.logs.Text+"\n";
                revision = Regex.Match(updateLog, @"revision\s[0-9]+").Value.Replace("revision", "");
            }
        }

        /// <summary>
        /// 手动编译
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBuild_Click(object sender, RoutedEventArgs e)
        {
            ProjectController projectController = new ProjectController();
            if (respoPath.Text == "" || projectName.Text == "" || Workspace.Text == "" || 
                Host.Text == "" || Mailto.Text == "" || Buildcomand.Text == "")
            {
                MessageBox.Show("请先填写完整的信息！");
            }
            else
            {
                ProjectInfo projectInfo = new ProjectInfo();
                projectInfo.Workdirectory = Workspace.Text;
                projectInfo.Repositorypath = respoPath.Text;
                projectInfo.MailTo = Mailto.Text;
                projectInfo.Buildcommand = Buildcomand.Text;
                string buildResult = "";
                string time = "";
                Task buildTask = new Task(() =>
                {
                    projectInfo.BuildTime = DateTime.Now.ToString();
                    log = projectController.Build(projectInfo.Buildcommand, projectInfo.Workdirectory, out buildResult,
                        out err,out time);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        duration = time;
                        logs.Text = "编译信息" + log + "\n" + this.logs.Text + "\n";
                        lastRe.Text = buildResult;
                        //检验是否存储信息(最近一次编译信息，以及后台存储信息)
                        Dictionary<string, string> lastProject = new Dictionary<string, string>();
                        //修改存储最近完成项目信息的xml文件
                        lastProject.Add("projectName", projectName.Text);
                        lastProject.Add("buildTime", projectInfo.BuildTime);
                        lastProject.Add("duration", projectInfo.Duration);
                        lastProject.Add("result", buildResult);
                        projectController.ModifyProject(lastProject, null,
                            "../../../common/res/LastestInfo.xml", "lastest");
                    }));
                });
                buildTask.Start();
            }
        }

        /// <summary>
        /// 点击事件进行发送邮件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendMail_Click(object sender, RoutedEventArgs e)
        {
            if (Username.Text == "#author#@domain.com")
            {
                MessageBox.Show("请修改默认发件人");
            }
            else
            {
                ConfigInfo configInfo = _configController.ConfigQuery("config/preferences", "../../../common/res/CIConfig.xml");
                _projectInfo.Workdirectory = Workspace.Text;
                _projectInfo = _svnController.GetLocalInfo(_projectInfo);
                _projectInfo.Nameproperty = projectName.Text;
                _projectInfo.Log = (configInfo.StandarOutput == "true") ? ((log + err).Replace("\n", "<br/>")) : (err.Replace("\n", "<br/>"));
                _projectInfo.Result = lastRe.Text;
                _projectInfo.Duration = duration;
                _projectInfo.Revision = revision;
                _projectInfo.MailTo = Mailto.Text;
                _projectInfo.MailHost = Host.Text;
                _projectInfo.UserName = Username.Text;
                _projectInfo.Password = Password.Text;
                MailController mailController = new MailController();
                MailInfo mailInfo = mailController.EditBody(_projectInfo, "../../../common/SendMail.html");
                sendRe.Text = mailController.SendMail(mailInfo);
            }
        }

        /// <summary>
        /// 保存信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ProjectController projectController = new ProjectController();
            if (respoPath.Text == ""
                || projectName.Text == ""
                || Workspace.Text == ""
                || Mailto.Text == ""
                || Buildcomand.Text == "")
            {
                MessageBox.Show("请先填写完整的信息！");
            }
            else if (Username.Text == "#author#@domain.com")
            {
                MessageBox.Show("请修改默认发件人");
            }
            else
            {
                //存储前台输入的信息
                var childNodes = new Dictionary<string, string>();
                var property = new Dictionary<string, string>();
                childNodes.Add("RepositoryPath", respoPath.Text);
                childNodes.Add("WorkingDirectory", Workspace.Text);
                childNodes.Add("MailTo", Mailto.Text);
                childNodes.Add("BuildCommand", Buildcomand.Text);
                childNodes.Add("IfSlack", CheckSlack.IsChecked.ToString().ToLower());
                childNodes.Add("IfMail", CheckMail.IsChecked.ToString().ToLower());
                childNodes.Add("SlackUrl", SlackUrl.Text);
                childNodes.Add("SlackChannel", SlackChannel.Text);
                childNodes.Add("MailHost",Host.Text);
                childNodes.Add("UserName",Username.Text);
                childNodes.Add("Password",Password.Text);
                childNodes.Add("SlackUser",SLackUser.Text);
                childNodes.Add("SlackContent",SlackContent.Text);
                property.Add("Name", projectName.Text);
                property.Add("Status", "true");
                bool ifExist = false;
                //查询项目名称是否已经存在
                List<ProjectInfo> projectInfos = projectController.ProjectQuery("/config/Projects", true,
                    "../../../common/res/CIConfig.xml");
                if (projectInfos.Any(project => project.Nameproperty == projectName.Text))
                {
                    ifExist = true;
                }
                //没有存在，进行创建添加       
                if (!ifExist)
                {
                    projectController.AddProject(childNodes, property, "../../../common/res/CIConfig.xml");
                    property.Clear();
                    property.Add("Name", projectName.Text);
                    MessageBox.Show("Add  " + projectController.AddProject(null, property, "../../../common/res/BuildResultLogs.xml"));
                }
                //项目名已经存在，进行覆盖
                else
                {
                    MessageBox.Show("Modify  " + projectController.ModifyProject(childNodes, property, "../../../common/res/CIConfig.xml",
                        "Projects"));
                }
            }
        }

        /// <summary>
        /// 取消按钮，清空
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancle_Click(object sender, RoutedEventArgs e)
        {
            projectName.Clear();
            respoPath.Clear();
            Workspace.Clear();
            Username.Clear();
            Password.Clear();
            Mailto.Clear();
            Buildcomand.Clear();
        }

    }
}
