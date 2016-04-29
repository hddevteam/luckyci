using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.DTO
{
   
   public class ConfigInfo
    {
        //------------------ConfigInfo类封装的私有属性---------------------------------------
        //svn本地路径
        private string svnPath;
        //自动更新间隔时间
        private string updateInterval;
        //邮件发送者
        private string mailFrom;
        //邮件发送者密码
        private string mailPassword;
        //标准输出
        private string standarOutput;
        //选择运行方式
        private string serviceSwitch;

        //周邮件发送配置
        private string reportFrom;
        private string password;
        private string smtpServer;
        private string reportTo;
        //---------------------------------------------------------

        //------------------ConfigInfo类的无参数构造方法---------------------------------------
        public ConfigInfo()
        {
            this.svnPath = "";
            this.updateInterval = "";
            this.mailFrom = "";
            this.mailPassword = "";
            this.standarOutput = "";
            this.serviceSwitch = "";
            this.reportFrom = "";
            this.reportTo = "";
            this.password = "";
            this.smtpServer = "";
            
        }
        //---------------------------------------------------------

        //------------------ConfigInfo类对外提供的用于访问私有属性的public方法---------------------------------------
        public string Svnpath
        {
            get { return svnPath; }
            set { svnPath = value; }
        }

        public string Updateinterval
        {
            get { return updateInterval; }
            set { updateInterval = value; }
        }

        public string Mailfrom
        {
            get { return mailFrom; }
            set { mailFrom = value; }
        }

        public string Mailpassword
        {
            get { return mailPassword; }
            set { mailPassword = value; }
        }

        public string StandarOutput
        {
            get
            {
                return standarOutput;
            }

            set
            {
                standarOutput = value;
            }
        }

        public string ServiceSwitch
        {
            get
            {
                return serviceSwitch;
            }

            set
            {
                serviceSwitch = value;
            }
        }

        public string ReportFrom
        {
            get
            {
                return reportFrom;
            }

            set
            {
                reportFrom = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
            }
        }

        public string SmtpServer
        {
            get
            {
                return smtpServer;
            }

            set
            {
                smtpServer = value;
            }
        }

        public string ReportTo
        {
            get
            {
                return reportTo;
            }

            set
            {
                reportTo = value;
            }
        }
        //---------------------------------------------------------
    }
}
