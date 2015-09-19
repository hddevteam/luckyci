using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using common.DTO;
using System.Text.RegularExpressions;

namespace common.BL
{
  public  class MailController
    {
        /// <summary>
        /// 执行发送邮件的动作
        /// </summary>
        /// <param name="mailInfo">mail相关信息的实例</param>
        /// <returns>Successful表示发送成功</returns>
        /// <returns>Failed表示发送失败</returns>
        public string SendMail(MailInfo mailInfo)
        {
            try
            {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage mailMessage = new MailMessage();
            smtpClient.Host = mailInfo.Host;
            smtpClient.UseDefaultCredentials = true;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            if (mailInfo.Mailfrom != "" && mailInfo.Mailpassword != "")
            {
                smtpClient.Credentials = new NetworkCredential(mailInfo.Mailfrom,
                    mailInfo.Mailpassword);
            }
            mailMessage.From = new MailAddress(mailInfo.Mailfrom);
            mailMessage.To.Add(mailInfo.Mailto);
            mailMessage.Subject = mailInfo.Subject;
            mailMessage.Body = mailInfo.Body;
            mailMessage.SubjectEncoding = Encoding.UTF8;
            mailMessage.BodyEncoding = Encoding.UTF8;
            mailMessage.Priority = MailPriority.High;
            mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
                return "successful";
            }
            catch (Exception e)
            {
                return "failed";
            }
        }

        /// <summary>
        /// 执行编辑邮件相关信息的操作
        /// </summary>
        /// <param name="projectInfo">传入projectInfo的实例对象</param>
        /// <returns>返回编辑后的MailInfo实例对象</returns>
        public MailInfo EditBody(ProjectInfo projectInfo,string mailPath)
        {
            string path = string.Empty;
            System.IO.StreamReader sr = new System.IO.StreamReader(mailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();
            MailInfo mailInfo = new MailInfo();
            string header = "";
            try
            {
                header = projectInfo.Result == "successful"
                    ? Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x98, 0x83})
                    : Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x91, 0xBF});
                header += projectInfo.Nameproperty + " revision" + projectInfo.Revision + " build " + projectInfo.Result;        
                body = body.Replace("$PROJECTNAME$", projectInfo.Nameproperty);
                body = body.Replace("$LOG$", projectInfo.Log);
                body = body.Replace("$VERSION$", projectInfo.Revision);
                body = body.Replace("$AUTHER$", projectInfo.Author);
                body = body.Replace("$DATE$", projectInfo.Changetime);
                body = body.Replace("$RESULT$", "Build " + projectInfo.Result);
                body = body.Replace("$DURATION$", projectInfo.Duration);
                body = projectInfo.Log == ""
                    ? body.Replace("$LOGLABEL$", "")
                    : body.Replace("$LOGLABEL$", "Build Log");
                mailInfo.Subject = header;
                mailInfo.Body = body;
                mailInfo.Mailto = projectInfo.MailTo;
                mailInfo.Host = projectInfo.MailHost;
                if (projectInfo.UserName.Contains("#author#"))
                {
                    mailInfo.Mailfrom = projectInfo.Author.Remove(0, 3) + Regex.Match(projectInfo.UserName, "(?:@)(.+)").Value;
                }
                else
                {
                    mailInfo.Mailfrom = projectInfo.UserName.Replace("#","");
                    mailInfo.Mailpassword = projectInfo.Password;
                }
                return mailInfo;
            }
            catch (Exception exception)
            {
                return mailInfo;
            }
        }
    }
}
