using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common.TOOL;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using common.DTO;
using common.DAO;


namespace common.BL
{
    public class ProjectController
    {
        XmlDao dao = new XmlDao();

        /// <summary>
        /// 执行编译程序
        /// </summary>
        /// <param name="buildcommand">编译语句</param>
        /// <param name="workdirectory">程序工作路径</param>
        /// <param name="buildResult">记录程序运行的结果</param>
        /// <param name="err">输出错误信息</param>
        /// <returns>程序编译结果的日志</returns>
        public string Build(string buildcommand, string workdirectory, out string buildResult,out string err,out string time)
        {
            Tools tools = new Tools();
            string args = "/C" + " " + buildcommand;
            string filename = "cmd.exe";
            string buildLog = "";
            buildLog = tools.BuildProject(filename, args, workdirectory, out err,out time);
            try
            {
                if (buildLog.Contains("BUILD SUCCESSFUL") || buildLog.Contains("已成功生成") ||
                    buildLog.Contains("build succeed"))
                {
                    buildResult = "successful";
                }
                else
                {
                    buildResult = "failed";
                }
                return buildLog;
            }
            catch(Exception ex)
            {
                buildResult = "failed";
                buildLog = ex.Message;
                return buildLog;
            }
        }

        /// <summary>
        /// 激活或者关闭项目
        /// </summary>
        /// <param name="nodePath">路径</param>
        /// <param name="name">项目名字</param>
        /// <param name="value">修改后的名字</param>
        /// <returns></returns>
        public string ActiveClose(Dictionary<string,string> project,Dictionary<string,string> property,string nodePath,string xmlConfigPath)
        {
            XmlDao xmlDao = new XmlDao();
            try
            {
                XElement xElement = xmlDao.SelectOneXElement(project, xmlConfigPath, nodePath);
                xmlDao.XNodeAttributes(property, xElement, xmlConfigPath);
                return "successful";
            }
            catch(Exception exception)
            {
                MessageBox.Show(exception.Message);
                return "failed";
            }

        }

        /// <summary>
        /// 执行添加项目操作
        /// </summary>
        /// <param name="childNodes">Projects节点下的节点集</param>
        /// <param name="property">Projects的节点属性集</param>
        /// <param name="xmlConfigPath">添加Projects的xml文件</param>
        /// <returns></returns>
        public string AddProject(Dictionary<string,string> childNodes  ,Dictionary<string,string> property,
            string xmlConfigPath)
        {           
            XmlDao xmlDao = new XmlDao();
            string result = "";
            Dictionary<string,string> projectNode = new Dictionary<string, string>();
            projectNode.Add("Projects",null);
            XElement xElement = xmlDao.AddXElement(projectNode, property, xmlConfigPath);
            result = xmlDao.AddXNode(childNodes, xElement, xmlConfigPath);
            return result;
        }

        /// <summary>
        /// 执行删除项目的操作
        /// </summary>
        /// <param name="projectName">执行将要删除的项目名</param>
        /// <param name="num">项目的序号</param>
        /// <param name="xmlConfigPath">要删除项目的xml文件组</param>
        /// <returns></returns>
        public string DeleteProject(string projectPath,string projectName,string[] xmlConfigPath)
        {
            try
            {

                return dao.XElementDelete(projectPath, projectName, xmlConfigPath);
            }
            catch (Exception exception)
            {
                return "failed";
            }           
        }

        /// <summary>
        /// 执行修改项目动态内容的操作(现有的,只修改不增加)
        /// </summary>
        /// <param name="value">需要修改的子节点的键值对</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string ModifyProject(Dictionary<string, string> projects, Dictionary<string, string> property,
            string xmlPath, string nodePath)
        {
            XmlDao xmlDao = new XmlDao();
            string result = "";
            XElement xElement = xmlDao.SelectOneXElement(property, xmlPath, nodePath);
            result = xmlDao.ModifyXNode(projects, xElement, xmlPath);
            return result;
        }

        /// <summary>
        /// 存储Log(按版本号增加Log节点
        /// </summary>
        /// <param name="value">需要修改的子节点的键值对</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string SaveLog(ProjectInfo projectInfo, Dictionary<string, string> property, string xmlPath,
            string nodePath)
        {
            XmlDao xmlDao = new XmlDao();
            var logInfo = new Dictionary<string,string>();
            var logProperty = new Dictionary<string,string>();
            logInfo.Add("Log",projectInfo.Log);
            logProperty.Add("Revision",projectInfo.Revision);
            logProperty.Add("Result",projectInfo.Result);
            logProperty.Add("Time",DateTime.Now.ToString());
            string result = "";
            XElement xElement = xmlDao.SelectOneXElement(property, xmlPath, nodePath);
            xmlDao.AddXNode(logInfo, xElement, xmlPath);
            xElement = xmlDao.SelectOneXElement(property, xmlPath, nodePath);
            result = xmlDao.XNodeAttributes(logProperty, xElement.Elements("Log").Last(), xmlPath);
            return result;
        }

        /// <summary>
        /// 执行获取所有符合要求的项目信息列表
        /// </summary>
        /// <param name="dataPath">查询的节点路径</param>
        /// <param name="b">true:项目信息；false:最近一次编译信息</param>
        /// <returns>返回查寻完毕的信息列表</returns>
        public List<ProjectInfo> ProjectQuery(string dataPath,bool b,string xmlPath)
        {
            List<ProjectInfo> projectInfos = new List<ProjectInfo>();
            XmlDao xmlDao = new XmlDao();
            if (b)
            {
                try
                {

                    XmlNodeList xmlNodeList = xmlDao.XmlQuery(dataPath,xmlPath);
                    foreach (XmlNode xmlNode in xmlNodeList)
                    {
                        ProjectInfo projectInfo = new ProjectInfo();
                        projectInfo.Statusproperty = xmlNode.Attributes["Status"].Value;
                        projectInfo.Nameproperty = xmlNode.Attributes["Name"].Value;
                        projectInfo.Buildcommand = xmlNode.SelectSingleNode("BuildCommand").InnerText;
                        projectInfo.Repositorypath = xmlNode.SelectSingleNode("RepositoryPath").InnerText;
                        projectInfo.Workdirectory = xmlNode.SelectSingleNode("WorkingDirectory").InnerText;
                        projectInfo.MailTo = xmlNode.SelectSingleNode("MailTo").InnerText;
                        projectInfo.IfMail = xmlNode.SelectSingleNode("IfMail").InnerText;
                        projectInfo.IfSlack = xmlNode.SelectSingleNode("IfSlack").InnerText;
                        projectInfo.SlackUrl = xmlNode.SelectSingleNode("SlackUrl").InnerText;
                        projectInfo.MailHost = xmlNode.SelectSingleNode("MailHost").InnerText;
                        projectInfo.UserName = xmlNode.SelectSingleNode("UserName").InnerText;
                        projectInfo.Password = xmlNode.SelectSingleNode("Password").InnerText;
                        projectInfo.SlackChannel = xmlNode.SelectSingleNode("SlackChannel").InnerText;
                        projectInfo.SlackUser = xmlNode.SelectSingleNode("SlackUser").InnerText;
                        projectInfo.SlackContent = xmlNode.SelectSingleNode("SlackContent").InnerText;
                        projectInfos.Add(projectInfo);
                    }
                    return projectInfos;
                }
                catch (Exception)
                {
                    return projectInfos;
                }
            }
            else
            {
                ProjectInfo projectInfo = new ProjectInfo();
                XmlNodeList xmlNodeList = xmlDao.XmlQuery(dataPath, xmlPath);
                projectInfo.Nameproperty = xmlNodeList[0].SelectSingleNode("projectName").InnerText;
                projectInfo.BuildTime = xmlNodeList[0].SelectSingleNode("buildTime").InnerText;
                projectInfo.Duration = xmlNodeList[0].SelectSingleNode("duration").InnerText;
                projectInfo.Result = xmlNodeList[0].SelectSingleNode("result").InnerText;
                projectInfo.Index = xmlNodeList[0].SelectSingleNode("index").InnerText;
                projectInfos.Add(projectInfo);
                return projectInfos;
            }
        }

        /// <summary>
        /// 获取logs的信息
        /// </summary>
        /// <param name="nodePath">获取节点的路径</param>
        /// <param name="xmlBuildPath">读取的xml文件的路径</param>
        /// <returns></returns>
        public List<ProjectInfo> ReadLog(string nodePath,string xmlBuildPath)
        {
            List<ProjectInfo> logs = new List<ProjectInfo>();
            XmlNodeList nodeList = dao.XmlQuery(nodePath,xmlBuildPath);
            foreach (XmlNode node in nodeList)
            {
                ProjectInfo projectInfo = new ProjectInfo();
                XmlNodeList xmlNodeList = node.SelectNodes("Log");
                int count = xmlNodeList.Count;
                if (count != 0)
                {
                    projectInfo.Log = xmlNodeList[count - 1].InnerText;
                    projectInfo.Nameproperty = node.Attributes["Name"].Value;
                    projectInfo.Revision = xmlNodeList[count - 1].Attributes["Revision"].Value;
                    projectInfo.Result = xmlNodeList[count - 1].Attributes["Result"].Value;
                    logs.Add(projectInfo);
                }
                else
                {
                    projectInfo.Nameproperty = node.Attributes["Name"].Value;
                    logs.Add(projectInfo);
                }
               
            }
            return logs;
        }
    }
}
