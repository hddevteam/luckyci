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
        /// <param name="project">传入的项目对象</param>
        /// <param name="property">传入的筛选条件</param>
        /// <param name="nodePath">路径</param>
        /// <param name="xmlConfigPath">节点名字</param>
        /// <returns></returns>
        public string ActiveClose(Dictionary<string,string> project, Dictionary<string,string> property, string nodePath, string xmlConfigPath)
        {
            try
            {
                XElement xElement = dao.SelectOneXElement(project, xmlConfigPath, nodePath);
                dao.XNodeAttributes(property, xElement, xmlConfigPath);
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
            string result = "";
            Dictionary<string,string> projectNode = new Dictionary<string, string>();
            projectNode.Add("Projects",null);
            XElement xElement = dao.AddXElement(projectNode, property, xmlConfigPath);
            result = dao.AddXNode(childNodes, xElement, xmlConfigPath);
            return result;
        }

        /// <summary>
        /// 执行删除项目的操作
        /// </summary>
        /// <param name="projectPath">项目的路径</param>
        /// <param name="projectName">执行将要删除的项目名</param>
        /// <param name="xmlConfigPath">要删除项目的xml文件组</param>
        /// <returns></returns>
        public string DeleteProject(string projectPath,string projectName,string[] xmlConfigPath)
        {
            try
            {

                return dao.XElementDelete(projectPath, projectName, xmlConfigPath);
            }
            catch (Exception ex)
            {
                return "failed";
            }           
        }

        /// <summary>
        /// 执行修改项目动态内容的操作(现有的,只修改不增加)
        /// </summary>
        /// <param name="projects">需要修改的项目的键值对</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string ModifyProject(Dictionary<string, string> projects, Dictionary<string, string> property,
            string xmlPath, string nodePath)
        {
            string result = "";
            XElement xElement = dao.SelectOneXElement(property, xmlPath, nodePath);
            result = dao.ModifyXNode(projects, xElement, xmlPath);
            return result;
        }

        /// <summary>
        /// 存储Log(按版本号增加Log节点
        /// </summary>
        /// <param name="projectInfo">需要存储的项目对象</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string SaveLog(ProjectInfo projectInfo, Dictionary<string, string> property, string xmlPath,
            string nodePath)
        {
            var logInfo = new Dictionary<string,string>();
            var logProperty = new Dictionary<string,string>();
            logInfo.Add("Log",projectInfo.Log);
            logProperty.Add("Revision",projectInfo.Revision);
            logProperty.Add("Result",projectInfo.Result);
            logProperty.Add("Time",DateTime.Now.ToString());
            string result = "";
            XElement xElement = dao.SelectOneXElement(property, xmlPath, nodePath);
            dao.AddXNode(logInfo, xElement, xmlPath);
            xElement = dao.SelectOneXElement(property, xmlPath, nodePath);
            result = dao.XNodeAttributes(logProperty, xElement.Elements("Log").Last(), xmlPath);
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
            //检测infostatics中有没有包括提交的项目，分别赋值
            if (xmlPath.Contains("InfoStatics.xml")) {
                XmlNodeList xmlNodeList = dao.XmlQuery(dataPath, xmlPath);
                if (xmlNodeList.Count!=0) {
                    foreach (XmlNode xmlNode in xmlNodeList) {
                        ProjectInfo projectInfo = new ProjectInfo();
                        projectInfo.Nameproperty = xmlNode.Attributes["Name"].Value;
                        projectInfos.Add(projectInfo);
                    }
                }
                return projectInfos;
            }
            else
            {
                if (b)
                {
                    try
                    {

                        XmlNodeList xmlNodeList = dao.XmlQuery(dataPath, xmlPath);
                        foreach (XmlNode xmlNode in xmlNodeList)
                        {
                            ProjectInfo projectInfo = new ProjectInfo();
                            projectInfo.Statusproperty = xmlNode.Attributes["Status"].Value;
                            projectInfo.Nameproperty = xmlNode.Attributes["Name"].Value;
                            projectInfo.BuildCommand = xmlNode.SelectSingleNode("BuildCommand").InnerText;
                            projectInfo.RepositoryPath = xmlNode.SelectSingleNode("RepositoryPath").InnerText;
                            projectInfo.WorkDirectory = xmlNode.SelectSingleNode("WorkingDirectory").InnerText;
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
                            projectInfo.SelectResult = xmlNode.SelectSingleNode("SlackResult").InnerText;
                            projectInfo.SelectCommit = xmlNode.SelectSingleNode("SlackCommit").InnerText;
                            projectInfo.SelectUpdate = xmlNode.SelectSingleNode("SlackUpdate").InnerText;
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
                    XmlNodeList xmlNodeList = dao.XmlQuery(dataPath, xmlPath);
                    projectInfo.Nameproperty = xmlNodeList[0].SelectSingleNode("projectName").InnerText;
                    projectInfo.BuildTime = xmlNodeList[0].SelectSingleNode("buildTime").InnerText;
                    projectInfo.Duration = xmlNodeList[0].SelectSingleNode("duration").InnerText;
                    projectInfo.Result = xmlNodeList[0].SelectSingleNode("result").InnerText;
                    projectInfo.Index = xmlNodeList[0].SelectSingleNode("index").InnerText;
                    projectInfos.Add(projectInfo);
                    return projectInfos;
                }
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
            try
            {
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
            }
            catch (Exception ex)
            {
                return logs;
            }
            return logs;
        }

        /// <summary>
        /// 提交次数以及编译结果的统计
        /// </summary>
        /// <param name="status">执行此方法时的程序状态</param>
        /// <param name="property">筛选XElement的选项</param>
        /// <param name="nodePath">节点的路径</param>
        /// <param name="xmlPath">修改的文件的路径</param>
        public void CommitStat(string status, Dictionary<string, string> property, string nodePath, string xmlPath)
        {
            XmlNodeList xmlNodeList = dao.XmlQuery(nodePath, xmlPath);
            XElement xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
            Dictionary<string, string> value = new Dictionary<string, string>();
            Dictionary<string, string> memberWeekSuccess = new Dictionary<string, string>();
            Dictionary<string, string> memberWeekFailed = new Dictionary<string, string>();
            Dictionary<string, string> tempMonth = new Dictionary<string, string>();
            XElement weekTotalxElement = dao.SelectOneXElement(null, xmlPath, "WeekTotal");
            try
            {
                switch (status)
                {
                    case "update":


                        //总次数加1（CommitTimes）                    
                        Dictionary<string, string> weekTotalCommit = new Dictionary<string, string>();
                        string weekTotalCommitBefor = weekTotalxElement.Element("CommitTimes").Value;
                        int weekTotalCommitAfter = int.Parse(weekTotalCommitBefor) +1;
                        weekTotalCommit.Add("CommitTimes", weekTotalCommitAfter.ToString());
                        dao.ModifyXNode(weekTotalCommit, weekTotalxElement, xmlPath);


                        //提交人员的本周次数加1（Week节点）                    
                        string weekCount = xElement.Element("Week").Value;
                        //数据表中初始值必须设置为0，才可以用此方法
                        int memberWeekTimes = int.Parse(weekCount)+1;                                      
                        value.Add("Week",memberWeekTimes.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(value, xElement, xmlPath);
                          break;


                    case "success":
                        //编译成功总次数加1
                        Dictionary<string, string> weekTotalBuildSuccess = new Dictionary<string, string>();
                        string weekTotalBuildSuccessBefor = weekTotalxElement.Element("BuildSuccessTimes").Value;
                        int weekTotalBuildSuccessAfter = int.Parse(weekTotalBuildSuccessBefor)+1;
                        weekTotalBuildSuccess.Add("BuildSuccessTimes", weekTotalBuildSuccessAfter.ToString());
                        dao.ModifyXNode(weekTotalBuildSuccess, weekTotalxElement, xmlPath);


                        //提交成员的编译成功次数加1  
                        Dictionary<string, string> memberWeekSuccessTimes = new Dictionary<string, string>();
                        string memberSuccessTimesBefor = xElement.Element("Success").Value;
                        //数据表中初始值必须设置为0，才可以用此方法
                        int memberSuccessTimesAfter = int.Parse(memberSuccessTimesBefor) + 1;
                        memberWeekSuccessTimes.Add("Success", memberSuccessTimesAfter.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(memberWeekSuccessTimes, xElement, xmlPath);
                        break;

                        case "failure":
                        //编译失败总次数加1
                        Dictionary<string, string> weekTotalBuildFailed = new Dictionary<string, string>();
                        string weekTotalBuildFailedBefor = weekTotalxElement.Element("BuildFailedTimes").Value;
                        int weekTotalBuildFailedAfter = int.Parse(weekTotalBuildFailedBefor) + 1;
                        weekTotalBuildFailed.Add("BuildFailedTimes", weekTotalBuildFailedAfter.ToString());
                        dao.ModifyXNode(weekTotalBuildFailed, weekTotalxElement, xmlPath);

                        //提交成员的编译失败次数加1
                        //  memberWeekFailed.Add("Failure", (Int32.Parse(xElement.Element("Week").Attribute("Failure").Value) + 1).ToString());
                        //  dao.XNodeAttributes(memberWeekFailed, xElement.Element("Week"), xmlPath);
                        Dictionary<string, string> memberWeekFailedTimes = new Dictionary<string, string>();
                        string memberFailedTimesBefor = xElement.Element("Failure").Value;
                        //数据表中初始值必须设置为0，才可以用此方法
                        int memberFailedTimesAfter = int.Parse(memberFailedTimesBefor) + 1;
                        memberWeekFailedTimes.Add("Failure", memberFailedTimesAfter.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(memberWeekFailedTimes, xElement, xmlPath);
                        break;

                        case "projectsCommit":
                        string weekSingleProjectCountBefor = xElement.Element("Commit").Value;
                        //数据表中初始值必须设置为0，才可以用此方法
                        int weekSingleProjectCountAfter = int.Parse(weekSingleProjectCountBefor) + 1;
                        value.Add("Commit", weekSingleProjectCountAfter.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(value, xElement, xmlPath);
                        break;


                        case "projectSuccess":
                        string weekSingleProjectSuccessBefor = xElement.Element("Success").Value;                      
                        int weekSingleProjectSucccessAfter = int.Parse(weekSingleProjectSuccessBefor) + 1;
                        value.Add("Success", weekSingleProjectSucccessAfter.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(value, xElement, xmlPath);
                        break;

                        case "projectFailed":
                        string weekSingleProjectFailedBefor = xElement.Element("Failed").Value;
                        int weekSingleProjectFailedAfter = int.Parse(weekSingleProjectFailedBefor) + 1;
                        value.Add("Failed", weekSingleProjectFailedAfter.ToString());
                        xElement = dao.SelectOneXElement(property, xmlPath, nodePath.Split('/')[1]);
                        dao.ModifyXNode(value, xElement, xmlPath);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("很抱歉,运行出错,原因： " + ex.Message);
            }
        }

        /// <summary>
        /// 获取一周总提交（编译）次数，以及编辑成功，失败次数
        /// </summary>
        /// <param name="nodePath"></param>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public  Dictionary<string, string> GetTotal(string nodePath, string xmlPath)
        {
            Dictionary<string, string> projectStat = new Dictionary<string, string>();
            XmlNodeList xmlNodeList = dao.XmlQuery(nodePath, xmlPath);
            try
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {   
                    projectStat.Add("CommitTimes", xmlNode.SelectSingleNode("CommitTimes").InnerText);
                    projectStat.Add("BuildSuccessTimes", xmlNode.SelectSingleNode("BuildSuccessTimes").InnerText);
                    projectStat.Add("BuildFailedTimes", xmlNode.SelectSingleNode("BuildFailedTimes").InnerText);
                 //   getValue.Add(xmlNode.Attributes[0].Value, projectStat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("很抱歉,运行出错,出错原因: " + ex.Message);
                return projectStat;
            }
            return projectStat;
        }
        /// <summary>
        /// 进行周清 
        /// </summary>
        /// <param name="nodePath">节点路径</param>
        /// <param name="infoStaticsPath">xml文件路径</param>
        /// <param name="value">设置为0</param>
        /// <param name="flag">判断进行total，还是member清空</param>
        /// <returns></returns>
        public string ClearTimesAfterSendReport(string nodePath, string infoStaticsPath, Dictionary<string, string> value,string flag)
        {
            if (flag == "memberTimes")
            {
                XmlNodeList memberList = dao.XmlQuery(nodePath, infoStaticsPath);
                foreach (XmlNode member in memberList)
                {
                    Dictionary<string, string> memberName = new Dictionary<string, string>();
                    memberName.Add("Name", member.Attributes["Name"].Value);
                    dao.ModifyXNode(value, dao.SelectOneXElement(memberName, infoStaticsPath, nodePath.Split('/')[1]), infoStaticsPath);
                }
            }
           else if (flag == "totalTimes")
            {
                dao.ModifyXNode(value, dao.SelectOneXElement(null, infoStaticsPath, nodePath.Split('/')[1]), infoStaticsPath);
            }
            else
            {
                XmlNodeList memberList = dao.XmlQuery(nodePath, infoStaticsPath);
                string[] xmlpath =new[] {infoStaticsPath};
                foreach (XmlNode memberName in memberList)
                {
                    DeleteProject(nodePath,memberName.Attributes["Name"].Value,xmlpath);
                }
            }
            return "successful";
        }

        /// <summary>
        /// 获取项目信息
        /// </summary>
        /// <param name="nodePath"></param>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, string>> GetProjectData(string nodePath, string xmlPath)
        {
            Dictionary<string, Dictionary<string, string>> getValue = new Dictionary<string, Dictionary<string, string>>();
            XmlNodeList xmlNodeList = dao.XmlQuery(nodePath, xmlPath);
            try
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    Dictionary<string, string> projectStat = new Dictionary<string, string>();
                    projectStat.Add("Commit", xmlNode.SelectSingleNode("Commit").InnerText);
                    projectStat.Add("Success", xmlNode.SelectSingleNode("Success").InnerText);
                    projectStat.Add("Failed", xmlNode.SelectSingleNode("Failed").InnerText);
                    getValue.Add(xmlNode.Attributes[0].Value, projectStat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("很抱歉,运行出错,出错原因: " + ex.Message);
                return getValue;
            }
            return getValue;
        }
        /// <summary>
        /// 获取人员信息
        /// </summary>
        /// <param name="nodePath"></param>
        /// <param name="xmlPath"></param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, string>> GetStatData(string nodePath, string xmlPath)
        {
            Dictionary<string, Dictionary<string, string>> getValue = new Dictionary<string, Dictionary<string, string>>();
            XmlNodeList xmlNodeList = dao.XmlQuery(nodePath, xmlPath);
            try
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    Dictionary<string, string> memberStat = new Dictionary<string, string>();
                    memberStat.Add("Week", xmlNode.SelectSingleNode("Week").InnerText);
                    memberStat.Add("Success", xmlNode.SelectSingleNode("Success").InnerText);
                    memberStat.Add("Failure", xmlNode.SelectSingleNode("Failure").InnerText);
                    getValue.Add(xmlNode.Attributes[0].Value, memberStat);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("很抱歉,运行出错,出错原因: " + ex.Message);
                return getValue;
            }
            return getValue;
        } 
    }
}
