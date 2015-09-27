using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.DTO
{
   public class ProjectInfo
    {
        //------------------ProjectInfo类封装的私有属性---------------------------------------
        //project的name属性值
        private string nameProperty;
        //project的status属性值
        private string statusProperty;
        //project的svn服务器路径
        private string repositoryPath;
        //project的本地工作路径
        private string workDirectory;
        //project的编译语句
        private string buildCommand;
        //project的编译后返回的日志
        private string log;
        //邮件接收者
        private string mailTo;
       //邮件域名
        private string mailHost;
       //发件人
       private string userName;
       //密码
       private string password;
        //project的编译结果
        private string result;
        //project的当前版本上传人
        private string author;
        //project的当前版本号
        private string revision;
        //project的当前版本修改时间
        private string changeTime;
        //project的编译时间
        private string duration;
        //lastProject的编译时间
        private string buildTime;
       //是否在slack中传输信息
        private string ifSlack;
        //是否发送邮件
        private string ifMail;
        //slack的网址
        private string slackUrl;
        //slack的频道
        private string slackChannel;
        //slack的发送者(default)
        private string slackUser;
        //slack的发送内容
        private string slackContent;
        //project 索引
        private string index;
        //项目提交的logMessage
        private string logMessage;
        
        //---------------------------------------------------------

        //------------------ProjectInfo类的无参数构造方法---------------------------------------
        public ProjectInfo()
        {
            this.nameProperty = "";
            this.statusProperty = "";
            this.repositoryPath = "";
            this.workDirectory = "";
            this.buildCommand = "";
            this.log = "";
            this.result = "";
            this.author = "";
            this.revision = "";
            this.changeTime = "";
            this.duration = "";
            this.mailTo = "";
            this.IfMail = "";
            this.IfSlack = "";
            this.SlackUrl = "";
            this.SlackChannel = "";
        }
        //---------------------------------------------------------

        //------------------ProjectInfo类对外提供的用于访问私有属性的public方法---------------------------------------
        public string Nameproperty
        {
            get { return nameProperty; }
            set { nameProperty = value; }
        }

        public string Statusproperty
        {
            get { return statusProperty; }
            set { statusProperty = value; }
        }

        public string Repositorypath
        {
            get { return repositoryPath; }
            set { repositoryPath = value; }
        }

        public string Workdirectory
        {
            get { return workDirectory; }
            set { workDirectory = value; }
        }

        public string Buildcommand
        {
            get { return buildCommand; }
            set { buildCommand = value; }
        }

        public string Log
        {
            get { return log; }
            set { log = value; }
        }

        public string Result
        {
            get { return result; }
            set { result = value; }
        }

        public string Author
        {
            get { return author; }
            set { author = value; }
        }

        public string Revision
        {
            get { return revision; }
            set { revision = value; }
        }

        public string Changetime
        {
            get { return changeTime; }
            set { changeTime = value; }
        }

        public string MailTo
        {
            get { return mailTo;}
            set { mailTo = value;}
        }

        public string Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        public string BuildTime
        {
            get
            {
                return buildTime;
            }

            set
            {
                buildTime = value;
            }
        }

       public string IfSlack
       {
           get { return ifSlack; }
           set { ifSlack = value; }
       }

       public string IfMail
       {
           get { return ifMail; }
           set { ifMail = value; }
       }

       public string SlackUrl
       {
           get { return slackUrl; }
           set { slackUrl = value; }
       }

       public string SlackChannel
       {
           get { return slackChannel; }
           set { slackChannel = value; }
       }

       public string MailHost
       {
           get { return mailHost; }
           set { mailHost = value; }
       }

       public string UserName
       {
           get { return userName; }
           set { userName = value; }
       }

       public string Password
       {
           get { return password; }
           set { password = value; }
       }

        public string Index
        {
            get
            {
                return index;
            }

            set
            {
                index = value;
            }
        }

       public string SlackUser
       {
           get { return slackUser; }
           set { slackUser = value; }
       }

       public string SlackContent
       {
           get { return slackContent; }
           set { slackContent = value; }
       }

        public string LogMessage
        {
            get
            {
                return logMessage;
            }

            set
            {
                logMessage = value;
            }
        }

        //---------------------------------------------------------
    }
}
