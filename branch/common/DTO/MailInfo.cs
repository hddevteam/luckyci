using System;
using System.Text;
using System.Net.Mail;

namespace common.DTO
{
  public  class MailInfo
    {
        //------------------MailInfo类封装的私有属性---------------------------------------
        //mail所需服务器名
        private string host;
        //mail内容主体
        private string body;
        //mail主题
        private string subject;
        //mail发送者
        private string mailFrom;
        //mail发送者的密码
        private string mailPassword;
        //mail接受者
        private string mailTo; 
        //---------------------------------------------------------

        //------------------MailInfo类的无参数构造方法---------------------------------------
        public MailInfo()
        {
            this.host = "";
            this.body = "";
            this.subject = "";
            this.mailFrom = "";
            this.mailPassword = "";
            this.mailTo = "";
        }
        //---------------------------------------------------------

        //------------------MailInfo类对外提供的用于访问私有属性的public方法---------------------------------------
        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        public string Body
        {
            get
            {
                body = body.Replace("Ran lint on variant release:", "<span style='background-color:yellow'>Ran lint on variant release:");
                body = body.Replace("Ran lint on variant debug:", "<span style='background-color:yellow'>Ran lint on variant debug:");
                body = body.Replace("lint-results.xml", "lint-results.xml</span></span>");
                return body;
            }
            set { body = value; }
        }

        public string Subject
        {
            get { return subject; }
            set { subject = value; }
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

        public string Mailto
        {
            get { return mailTo; }
            set { mailTo = value; }
        }
        //---------------------------------------------------------
    }
}
