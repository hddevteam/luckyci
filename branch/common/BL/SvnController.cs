using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SharpSvn;
using System.Windows.Media;
using common.TOOL;
using System.Windows;
using common.DTO;
using common.DAO;
using System.Xml;
using System.Collections.ObjectModel;

namespace common.BL
{
    public class SvnController
    {
        /// <summary>
        /// 执行检出操作
        /// </summary>
        /// <param name="repositoryPath">svn服务器路径</param>
        /// <param name="workDirectory">工程本地工作路径</param>
        /// <param name="svnPath">本地svn路径</param>
        /// <param name="checkResult">检出操作的结果</param>
        /// <returns>返回检出操作的日志</returns>
        public string CheckOut(string repositoryPath, string workDirectory, out string checkResult, string xmlConfigPath)
        {
            string err;
            string time;
            XmlDao xmlDao = new XmlDao();
            XmlNodeList xmlNodeList = xmlDao.XmlQuery("config/preferences/SvnPath", xmlConfigPath);
            string svnPath = xmlNodeList[0].InnerText;
            using (SvnClient client = new SvnClient())
            {
                Tools tools = new Tools();
                string checkOutLog = "";
                try
                {
                    client.CheckOut(new Uri(repositoryPath), workDirectory);
                    string args = "checkout " + repositoryPath + " " + workDirectory;
                    checkOutLog = tools.BuildProject(svnPath, args, null, out err, out time);
                    checkResult = "successful";
                    return checkOutLog;
                }
                catch (Exception ex)
                {
                    checkResult = " failed";
                    checkOutLog = ex.Message;
                    return checkOutLog;
                }
            }
        }

        /// <summary>
        /// 执行更新操作
        /// </summary>
        /// <param name="workDirectory">工程本地工作路径</param>
        /// <param name="svnPath">svn程序的路径</param>
        /// <param name="updateResult">更新操作的结果</param>
        /// <returns>返回更新操作的日志</returns>
        public string Update(string workDirectory, out string updateResult, string xmlConfigPath)
        {
            string err;
            string time;
            Tools tools = new Tools();
            XmlDao xmlDao = new XmlDao();
            XmlNodeList xmlNodeList = xmlDao.XmlQuery("config/preferences/SvnPath", xmlConfigPath);
            string svnPath = xmlNodeList[0].InnerText;
            string updateLog = "";
            try
            {
                string args = "update --accept tf" + " " + workDirectory;
                updateLog = tools.BuildProject(svnPath, args, null, out err, out time);
                updateResult = "successful";
                return updateLog;
            }
            catch (Exception ex)
            {
                updateResult = "failed";
                updateLog = ex.Message;
                return updateLog;
            }
        }

        /// <summary>
        /// 执行清理操作
        /// </summary>
        /// <param name="workingDirectory">工程本地工作路径</param>
        /// <param name="svnPath">svn程序的路径</param>
        /// <param name="cleanResult">清理操作的结果</param>
        /// <returns>返回清理操作的日志</returns>
        public string CleanUp(string workingDirectory, string svnPath, out string cleanResult)
        {
            string err;
            string time;
            Tools tools = new Tools();
            string cleanLog = "";
            try
            {
                string args = "cleanup" + " " + workingDirectory;
                cleanLog = tools.BuildProject(svnPath, args, null, out err, out time);
                cleanResult = "successful";
                return cleanLog;
            }
            catch (Exception ex)
            {
                cleanResult = "failed";
                cleanLog = ex.Message;
                return cleanLog;
            }
        }

        /// <summary>
        /// 执行获取本地项目svn信息的操作
        /// </summary>
        /// <param name="projectInfo">传入想要获取信息的projectInfo实例对象</param>
        /// <returns>获取完信息的projectInfo的实例对象</returns>
        public ProjectInfo GetLocalInfo(ProjectInfo projectInfo)
        {
            using (SvnClient svnClient = new SvnClient())
            {
                try
                {
                    SvnInfoEventArgs clientInfo;
                    SvnPathTarget local = new SvnPathTarget(projectInfo.WorkDirectory);
                    svnClient.GetInfo(local, out clientInfo);
                    string author = clientInfo.LastChangeAuthor;
                    string revision = clientInfo.LastChangeRevision.ToString();
                    projectInfo.Author = author;

                    SvnLogArgs getLogMessage = new SvnLogArgs();
                    Collection<SvnLogEventArgs> col;
                    getLogMessage.Start = int.Parse(revision);
                    getLogMessage.End = int.Parse(revision);
                    bool gotLog = svnClient.GetLog(new Uri(projectInfo.RepositoryPath), getLogMessage, out col);
                    if (gotLog)
                    {
                        projectInfo.LogMessage = col[0].LogMessage;
                    }
                    return projectInfo;
                }
                catch (Exception ex)
                {
                    return projectInfo;
                }
            }
        }
    }
}
