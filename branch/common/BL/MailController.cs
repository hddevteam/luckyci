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
        public MailInfo EditBody(ProjectInfo projectInfo, Dictionary<string, Dictionary<string, string>> allStat, string mailPath)
        {
            string path = string.Empty;
            System.IO.StreamReader sr = new System.IO.StreamReader(mailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();
            MailInfo mailInfo = new MailInfo();
            try
            {
                Dictionary<string, string> selectStat = allStat[projectInfo.Author.Split('\\')[1]];
                int[] allUpdate = new int[allStat.Count];
                int i = 0;
                int selectWeek = 0, selectSuccess = 0, selectFailure = 0;
                foreach (var key in allStat.Keys)
                {
                    allUpdate[i] = Int32.Parse(allStat[key]["Week"]);
                    if (key == projectInfo.Author.Split('\\')[1])
                    {
                        selectWeek = Int32.Parse(allStat[key]["Week"]);
                        selectSuccess = Int32.Parse(allStat[key]["Success"]);
                        selectFailure = Int32.Parse(allStat[key]["Failure"]);
                    }
                    i++;
                }
                //计算个人本周编译次数，成功失败次数，以及其所占有的比率
                double successRate = (double)selectSuccess / selectWeek;
                double failureRate = (double)selectFailure / selectWeek;

                Array.Sort(allUpdate);
                Array.Reverse(allUpdate);
                var emoji = projectInfo.Result == "successful"
                    ? Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x98, 0x83})
                    : Encoding.UTF8.GetString(new byte[] {0xF0, 0x9F, 0x91, 0xBF});
                var header = emoji + projectInfo.Nameproperty + " revision" + projectInfo.Revision + " build " +
                             projectInfo.Result + ". This week " + projectInfo.Author.Split('\\')[1] + " build " +
                             selectWeek.ToString() + " times, " +selectSuccess.ToString()+" passed, "+selectFailure.ToString()+" failed.";
                            
                body = body.Replace("$PROJECTNAME$", projectInfo.Nameproperty);
                body = body.Replace("$LOG$", projectInfo.Log);
                body = body.Replace("$VERSION$", projectInfo.Revision);
                body = body.Replace("$AUTHOR$", projectInfo.Author);
                body = body.Replace("$DATE$", projectInfo.Changetime);
                body = body.Replace("$RESULT$", "build " + projectInfo.Result);
                body = body.Replace("$DURATION$", projectInfo.Duration);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                body = projectInfo.Log == "\n"
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
            catch (Exception ex)
            {
                return mailInfo;
            }
        }
        /// <summary>
        /// 发送每周邮件报告
        /// </summary>
        /// <param name="reportMailPath"></param>
        /// <param name="allStatics"></param>
        /// <param name="allProjectStatics"></param>
        /// <returns></returns>
        public bool SendReport(string reportMailPath,Dictionary<string, Dictionary<string, string>> allStatics,Dictionary<string, Dictionary<string, string>> allProjectStatics, Dictionary<string, string> totalTimes) {
            string totalWeekTimes = "";
            string weekByDeveloper = "";
            string buildByDeveloper = "";
            string weekByProject = "";
            string buildByProject = "";
            int memberSort = 1;
            int projectSort = 1;
            System.IO.StreamReader sr = new System.IO.StreamReader(reportMailPath);
            string body = string.Empty;
            body = sr.ReadToEnd();

            if (int.Parse(totalTimes["CommitTimes"]) != 0)
            {
                totalWeekTimes += "Commit " + totalTimes["CommitTimes"] + " times<br/>" +
                                "Build " + totalTimes["CommitTimes"] + " times<br/>" +
                                 totalTimes["BuildSuccessTimes"] + " passed(" + (((double.Parse(totalTimes["BuildSuccessTimes"])) / (double.Parse(totalTimes["CommitTimes"])))).ToString("0%") + ")<br/>" +
                                 totalTimes["BuildFailedTimes"] + " failed(" + (((double.Parse(totalTimes["BuildFailedTimes"])) / (double.Parse(totalTimes["CommitTimes"])))).ToString("0%") + ")";



                
                while (true) {
                    if (allStatics.Count==0) { break; };
                    KeyValuePair<string, Dictionary<string, string>> temp = new KeyValuePair<string, Dictionary<string, string>>();
                    foreach (var member in allStatics)
                    {
                        temp = member;
                        foreach (var memberCopy in allStatics)
                        {
                            if (int.Parse(temp.Value["Week"]) < int.Parse(memberCopy.Value["Week"]))
                            {
                                temp = memberCopy;
                            }                          
                        }
                        break;
                    }
                        if (int.Parse(temp.Value["Week"]) == 0) { continue; }
                        weekByDeveloper += memberSort + ". " + temp.Key + " , " + temp.Value["Week"] + "<br/>";
                        buildByDeveloper += memberSort + ". " + temp.Key + " , " + temp.Value["Week"] + " builds, " +
                             temp.Value["Success"] + " passed(" + (((double.Parse(temp.Value["Success"])) / (double.Parse(temp.Value["Week"])))).ToString("0%") + "), "
                            + temp.Value["Failure"] + " failed(" + (((double.Parse(temp.Value["Failure"])) / (double.Parse(temp.Value["Week"])))).ToString("0%") + ") <br/>";
                        memberSort++;
                    allStatics.Remove(temp.Key);
                    
                }



                while (true)
                {
                    if (allProjectStatics.Count==0) { break; };
                    KeyValuePair<string, Dictionary<string, string>> projectTemp = new KeyValuePair<string, Dictionary<string, string>>();
                    foreach (var project in allProjectStatics)
                    {
                        projectTemp = project;
                        foreach (var projectCopy in allProjectStatics)
                        {
                            if (int.Parse(projectTemp.Value["Commit"]) < int.Parse(projectCopy.Value["Commit"]))
                            {
                                projectTemp = projectCopy;
                            }                            
                        }
                        break;
                    }
                        weekByProject += projectSort + ". " + projectTemp.Key + " , " + projectTemp.Value["Commit"] + "<br/>";
                        buildByProject += projectSort + ". " + projectTemp.Key + " , " + projectTemp.Value["Commit"] + " builds, " +
                             projectTemp.Value["Success"] + " passed(" + (((double.Parse(projectTemp.Value["Success"])) / (double.Parse(projectTemp.Value["Commit"])))).ToString("0%") + "), "
                            + projectTemp.Value["Failed"] + " failed(" + (((double.Parse(projectTemp.Value["Failed"])) / (double.Parse(projectTemp.Value["Commit"])))).ToString("0%") + ") <br/>";
                        projectSort++;
                    allProjectStatics.Remove(projectTemp.Key);
                }
            



                body = body.Replace("$WEEKTOTAL$", totalWeekTimes);
                body = body.Replace("$COMMIT_DEVELOPER$", weekByDeveloper);
                body = body.Replace("$COMMIT_PROJECT$", weekByProject);
                body = body.Replace("$BUILD_DEVELOPER$", buildByDeveloper);
                body = body.Replace("$BUILD_PROJECT$", buildByProject);
                body = body.Replace("$SERVER_VERSION$", "Send by LuckyCI v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SmtpClient smtpClient = new SmtpClient();
                MailMessage mailMessage = new MailMessage();
                smtpClient.Host = "exch.henu.edu.cn";
                smtpClient.UseDefaultCredentials = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                string thisMonday = DateTime.Now.ToString("yyyy/MM/dd");
                string lastMonday = DateTime.Now.AddDays(-7).ToString("yyyy/MM/dd");

                mailMessage.From = new MailAddress("jxm@it.henu.edu.cn");
                mailMessage.To.Add("svn-notify@it.henu.edu.cn");
                mailMessage.Subject = "Lucky CI Weekly Report " + lastMonday + "-" + thisMonday;
                mailMessage.Body = body;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.Priority = MailPriority.High;
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
            }
            return true;
        }

    }
}
