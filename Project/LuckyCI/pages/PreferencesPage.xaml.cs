using System.Collections.Generic;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using common.BL;
using common.DTO;

namespace LuckyCI.pages
{
    /// <summary>
    /// PreferencesPage.xaml 的交互逻辑
    /// </summary>
    public partial class PreferencesPage : Page
    {
        public PreferencesPage()
        {
            InitializeComponent();
            InitUserConfig();         
        }

        /// <summary>
        /// 执行保存已修改数据的方法
        /// </summary>
        private void InitUserConfig()
        {
            string dataPath = "config/preferences";
            ConfigController configController = new ConfigController();
            ConfigInfo configInfo = configController.ConfigQuery(dataPath, "../../../common/res/CIConfig.xml");         
            Svnpath.Text = configInfo.Svnpath;
            Updateinterval.Text = configInfo.Updateinterval;
            switch (configInfo.StandarOutput)
            {
                case "true":
                    standaroutput.IsChecked = true;
                    break;
                case "false":
                    standaroutput.IsChecked = false;
                    break;
                default:
                    standaroutput.IsChecked = false;
                    break;
            }
            switch (configInfo.ServiceSwitch)
            {
                case "window":
                    windowprocess.IsChecked = true;
                    break;
                case "service":
                    serviceprocess.IsChecked = true;
                    break;
                default:
                    windowprocess.IsChecked = false;
                    serviceprocess.IsChecked = false;
                    break;
            }
            ReportFrom.Text = configInfo.ReportFrom;
            ReportTo.Text = configInfo.ReportTo;
            Password.Text = configInfo.Password;
            SmtpServer.Text = configInfo.SmtpServer;
        }

        /// <summary>
        /// 执行保存已修改数据的方法
        /// </summary>
        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            ConfigController configController = new ConfigController();
            ConfigInfo configInfo = new ConfigInfo();
            //global
            configInfo.StandarOutput = standaroutput.IsChecked == true ? "true" : "false";
            configInfo.Svnpath = Svnpath.Text;
            configInfo.Updateinterval = Updateinterval.Text;
            configInfo.ServiceSwitch = (windowprocess.IsChecked == true) ? "window" : "service";
            //config report
            configInfo.ReportFrom = ReportFrom.Text;
            configInfo.Password = Password.Text;
            configInfo.SmtpServer = SmtpServer.Text;
            configInfo.ReportTo = ReportTo.Text;
            var result = configController.SaveConfig(configInfo, "../../../common/res/CIConfig.xml");
            MessageBox.Show(result);
        }

        /// <summary>
        /// 执行取消修改数据方法,恢复到未保存的初始状态·
        /// </summary>
        private void btnCancle_Click(object sender, RoutedEventArgs e)
        {
            InitUserConfig();
        }

        /// <summary>
        /// 执行恢复默认数据的方法
        /// </summary>
        private void btnDefault_Click(object sender, RoutedEventArgs e)
        {
            ConfigController configController = new ConfigController();
            ConfigInfo configInfo = configController.RestoreDefault();
            Svnpath.Text = configInfo.Svnpath;
            Updateinterval.Text = configInfo.Updateinterval;
            standaroutput.IsChecked = true;
            windowprocess.IsChecked = true;
            serviceprocess.IsChecked = false;
            ReportFrom.Text = configInfo.ReportFrom;
            ReportTo.Text = configInfo.ReportTo;
            Password.Text = configInfo.Password;
            SmtpServer.Text = configInfo.SmtpServer;
        }

        /// <summary>
        /// 执行允许用户在本地选择文件的方法
        /// </summary>
        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            if (open.ShowDialog() == true)
            {
                Svnpath.Text = open.FileName;
            }
        }
    }
}
