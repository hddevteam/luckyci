using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using common.DAO;
using common.DTO;

namespace common.BL
{
   public  class ConfigController
    {
        /// <summary>
        /// 查询配置信息
        /// </summary>
        /// <param name="nodePath"></param>
        /// <returns></returns>
        public XmlNodeList FindConfigInfo(string nodePath,string xmlConfigPath)
        {
            XmlDao dao = new XmlDao();
            return dao.XmlQuery(nodePath, xmlConfigPath);
        }

        /// <summary>
        /// 执行保存已经修改的ConfigInfo
        /// </summary>
        /// <param name="configInfo">传入configInfo实例对象</param>
        /// <param name="xmlConfigPath">修改的xml文件</param>
        /// <returns></returns>
        public string SaveConfig(ConfigInfo configInfo,string xmlConfigPath)
        {
            string modifyPath = "preferences";
            string result = "";
            var value = new Dictionary<string, string>();
            value.Add("SvnPath",configInfo.Svnpath);
            value.Add("UpdateInterval",configInfo.Updateinterval);
            value.Add("StandarOutput",configInfo.StandarOutput);
            value.Add("ServiceSwitch", configInfo.ServiceSwitch);
            value.Add("ReportFrom",configInfo.ReportFrom);
            value.Add("Password",configInfo.Password);
            value.Add("MailHost",configInfo.SmtpServer);
            value.Add("ReportTo",configInfo.ReportTo);
            try
            {
                XmlDao xmlDao = new XmlDao();
                XElement xElement = xmlDao.SelectOneXElement(null, xmlConfigPath, modifyPath);
                result = xmlDao.ModifyXNode(value, xElement, xmlConfigPath);
                return result;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                result = "failed";
                return result;
            }
        }

        /// <summary>
        /// 执行恢复默认配置的操作
        /// </summary>
        /// <returns>返回configInfo的实例对象</returns>
        public ConfigInfo RestoreDefault()
        {
            ConfigInfo configInfo = new ConfigInfo();          
            configInfo.Updateinterval = "30";
            configInfo.Svnpath = "C:\\Program Files\\TortoiseSVN\\bin\\svn.exe";
            configInfo.StandarOutput = "true";
            configInfo.ServiceSwitch = "window";
            configInfo.ReportFrom = "Demo_From@gmail.com";
            configInfo.Password = "123456";
            configInfo.ReportTo = "Demo_To@gmail.com";
            configInfo.SmtpServer = "smtp.gmail.com";
            return configInfo;
        }

       /// <summary>
       /// 查询配置信息
       /// </summary>
       /// <param name="dataPath">节点路径</param>
       /// <param name="xmlConfigPath">查询的xml文件路径</param>
       /// <returns></returns>
        public ConfigInfo ConfigQuery(string dataPath,string xmlConfigPath)
        {
            ConfigInfo configInfo = new ConfigInfo();
            XmlDao xmlDao = new XmlDao();
            try
            {
                XmlNodeList xmlNodeList = xmlDao.XmlQuery(dataPath, xmlConfigPath);
                configInfo.Svnpath = xmlNodeList[0].SelectSingleNode("SvnPath").InnerText;
                configInfo.Updateinterval = xmlNodeList[0].SelectSingleNode("UpdateInterval").InnerText;
                configInfo.StandarOutput = xmlNodeList[0].SelectSingleNode("StandarOutput").InnerText;
                configInfo.ServiceSwitch = xmlNodeList[0].SelectSingleNode("ServiceSwitch").InnerText;
                configInfo.ReportFrom = xmlNodeList[0].SelectSingleNode("ReportFrom").InnerText;
                configInfo.ReportTo = xmlNodeList[0].SelectSingleNode("ReportTo").InnerText;
                configInfo.Password = xmlNodeList[0].SelectSingleNode("Password").InnerText;
                configInfo.SmtpServer = xmlNodeList[0].SelectSingleNode("MailHost").InnerText;
                return configInfo;
            }
            catch (Exception)
            {
                return configInfo;
            }
        }
        /// <summary>
        /// 获取人名对应表
        /// </summary>
        /// <param name="nodePath">人名对应表节点路径</param>
        /// <param name="xmlPath">存放对应表的xml文件路径</param>
        /// <returns></returns>
        public XmlNodeList AcquireSlackPeople(string nodePath, string xmlPath)
        {                      
                XmlDao dao = new XmlDao();
                return dao.XmlQuery(nodePath, xmlPath);           
        }
        /// <summary>
        /// 获取gitlab信息
        /// </summary>
        /// <param name="dataPath">节点路径</param>
        /// <param name="xmlConfigPath">xml路径</param>
        /// <returns></returns>
        public GitInfo GitInfoQuery(string dataPath, string xmlConfigPath)
        {
            GitInfo gitlabInfo = new GitInfo();
            XmlDao xmlDao = new XmlDao();
            try
            {
                XmlNodeList xmlNodeList = xmlDao.XmlQuery(dataPath, xmlConfigPath);
                gitlabInfo.Username = xmlNodeList[0].SelectSingleNode("Username").InnerText;
                gitlabInfo.Password = xmlNodeList[0].SelectSingleNode("Password").InnerText;
                gitlabInfo.Emailaddress = xmlNodeList[0].SelectSingleNode("Email").InnerText;
                gitlabInfo.Gitreversion = xmlNodeList[0].SelectSingleNode("GitReversion").InnerText;
                return gitlabInfo;
            }
            catch (Exception)
            {
                return gitlabInfo;
            }
        }
    }
}
