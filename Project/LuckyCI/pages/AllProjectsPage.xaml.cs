using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using common.BL;
using common.DTO;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LuckyCI.pages
{
    /// <summary>
    /// AllProjectsPage.xaml 的交互逻辑
    /// </summary>
    public partial class AllProjectsPage
    {
        readonly DispatcherTimer _timer = new DispatcherTimer();
        private List<ProjectInfo> _projectInfos;
        public AllProjectsPage()
        {
            InitializeComponent();
            InitAllProjects();
            //一次重启等价于先关闭再开启
            butReset.Click += btnOff_Click;
            butReset.Click += btnOn_Click;
            _timer.Tick += timer_Tick;//注册计时器
            _timer.Interval = TimeSpan.FromMilliseconds(300);
            _timer.Stop();
            _timer.Start();//启动计时器
        }

        /// <summary>
        /// 初始化界面所有项目,并显示日志
        /// </summary>
        private void InitAllProjects()
        {
            InitDetailLogs();
            InitProjectList();
            InitServiceStatus();
        }

        /// <summary>
        /// 初始化日志显示
        /// </summary>
        private void InitDetailLogs()
        {
            AllLog.Text = "";
            ProjectController projectController = new ProjectController();
            //获取log信息
            List<ProjectInfo> logs = projectController.ReadLog("/config/Projects", "../../../common/res/BuildResultLogs.xml");
            foreach (var proLog in logs)
            {
                AllLog.Text += proLog.Nameproperty + "编译日志" + "\n" + proLog.Log + "\n";
            }
        }

        /// <summary>
        /// 初始化项目列表显示
        /// </summary>
        public void InitProjectList()
        {
            ListView.Items.Clear();
            ProjectController projectController = new ProjectController();
            _projectInfos = projectController.ReadLog("/config/Projects", "../../../common/res/BuildResultLogs.xml");
            ProjectInfo lastestInfo = projectController.ProjectQuery("config/lastest", false,
                "../../../common/res/LastestInfo.xml").First();
            foreach (var projectInfo in _projectInfos)
            {
                var flag = projectInfo.Result;
                //如果是最近一次编译的项目,则以最近一次编译的结果为准
                if (projectInfo.Nameproperty.Equals(lastestInfo.Nameproperty))
                {
                    flag = lastestInfo.Result;
                }
                string imageSource;
                switch (flag)
                {
                    case "successful":
                        imageSource = "../images/dot_green.png";
                        break;
                    case "running":
                        imageSource = "../images/dot_yellow.png";
                        break;
                    case "failed":
                        imageSource = "../images/dot_red.png";
                        break;
                    default: imageSource = "../images/dot_black.png";
                        break;
                }
                // 每一项ListViewItem包含的控件
                var img = new Image();
                var tb = new TextBlock();
                var stp = new StackPanel();
                var lvi = new ListViewItem();

                //设置控件相关属性
                img.Width = 12;
                img.Height = 12;
                img.Source = new BitmapImage(new Uri(imageSource, UriKind.Relative));
                img.Margin = new Thickness(2, 0, 5, 0);

                tb.Text = projectInfo.Nameproperty;

                tb.Height = 12;
                stp.Orientation = Orientation.Horizontal;
                var convertFromString = ColorConverter.ConvertFromString("#999");
                if (convertFromString != null)
                    lvi.BorderBrush = new SolidColorBrush((Color)convertFromString);
                lvi.BorderThickness = new Thickness(2, 0, 2, 2);
                lvi.Height = 30;
                var fromString = ColorConverter.ConvertFromString("#eee");
                if (fromString != null)
                lvi.Background = new SolidColorBrush((Color)fromString);   

                stp.Children.Add(img);
                stp.Children.Add(tb);
                lvi.Content = stp;
                ListView.Items.Add(lvi);
            }
        }

        /// <summary>
        /// 初始化项目状态显示
        /// </summary>
        public void InitServiceStatus()
        {
            var statusImg = "";
            var dataPath = "config/preferences";
            ConfigController configController = new ConfigController();
            ConfigInfo configInfo = configController.ConfigQuery(dataPath, "../../../common/res/CIConfig.xml");
            ServiceController[] service = ServiceController.GetServices();
            for (int i = 0; i < service.Length; i++)
            {
                if (service[i].DisplayName.Equals("LuckyCIService"))
                {
                    switch (service[i].Status)
                    {
                        case ServiceControllerStatus.Running:
                            statusImg = "../images/dot_yellow.png";
                            ServiceStatus.Content = "Running";
                            break;
                        case ServiceControllerStatus.Stopped:
                            statusImg = "../images/dot_black.png";
                            ServiceStatus.Content = "Stopped";
                            break;
                    }
                    StatusFlag.Source = new BitmapImage(new Uri(statusImg, UriKind.Relative));
                    break;
                }
                else
                {
                    statusImg = "../images/dot_black.png";
                    ServiceStatus.Content = "Not Install";
                    StatusFlag.Source = new BitmapImage(new Uri(statusImg, UriKind.Relative));
                }
            }
            //serviceswitch为service，service按钮可用，否则不可用
            if (configInfo.ServiceSwitch == "service")
            {
                butOn.IsEnabled = true;
                butOff.IsEnabled = true;
                butReset.IsEnabled = true;
            }
            else
            {
                butOn.IsEnabled = false;
                butOff.IsEnabled = false;
                butReset.IsEnabled = false;
            }
        }

        /// <summary>
        /// 时间刷新执行时间
        /// </summary>
        private void timer_Tick(object sender, EventArgs e)
        {
            InitProjectList();
            InitServiceStatus();
        }

        /// <summary>
        /// 执行开启服务操作
        /// </summary>
        private void btnOn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (ServiceController service in services)
                {
                    if (service.ServiceName == "LuckyCIService")
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 30));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            //注释为
            //string args = "/user:Administrator \"cmd /K " + "net start LuckyCIService" + "\"";
            //p.StartInfo.UseShellExecute = true;
            //p.StartInfo.Verb = "RunAs";
        }

        /// <summary>
        /// 执行关闭服务操作
        /// </summary>
        private void btnOff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServiceController[] services = ServiceController.GetServices();
                foreach (ServiceController service in services)
                {
                    if (service.ServiceName == "LuckyCIService")
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 30));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        //当点击listviewitem时(鼠标离开listview)执行停止timer操作
        private void ListView_OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            //_timer.Stop();
            ////用户在5s之内选择执行操作否则进行listview的刷新
            //Task waitSecs = new Task(() =>
            //{
            //    ListView.LostMouseCapture -= ListView_OnLostMouseCapture;
            //    Thread.Sleep(5000);               
            //});
            //waitSecs.Start();
            //Task task = waitSecs.ContinueWith(t =>
            //{
            //    ListView.LostMouseCapture += ListView_OnLostMouseCapture;
            //    _timer.Stop();
            //    _timer.Start();
            //});

            ProjectController projectController = new ProjectController();
            var index = ListView.SelectedIndex;
            Dictionary<string, string> indexValue = new Dictionary<string, string>();
            indexValue.Add("index", index.ToString());
            projectController.ModifyProject(indexValue, null, "../../../common/res/LastestInfo.xml", "lastest");
            //跳转指定的page
            var navigationService = NavigationService;
            if (navigationService != null)
                navigationService.Navigate(new Uri("/LuckyCI;component/pages/AddPage.xaml", UriKind.Relative));
        }
    }
}