using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using common.DTO;

namespace common.DAO
{
    public class XmlDao
    {

        private XmlDocument xmlDoc = new XmlDocument();
        private XElement xdoc;

        /// <summary>
        /// 添加项目
        /// </summary
        /// <param name="nodes">增加节点集</param>
        /// <param name="property">节点的属性集</param>
        /// <param name="xmlConfigPath">添加节点的xml文件</param>
        /// <returns></returns>
        public XElement AddXElement(Dictionary<string,string> nodes , Dictionary<string,string> property , string xmlConfigPath)
        {
            xdoc = XElement.Load(xmlConfigPath);
            try
            {
                foreach (var item in nodes)
                {
                    XElement newElement = new XElement(item.Key);
                    foreach (var attribute in property)
                    {
                        newElement.SetAttributeValue(attribute.Key,attribute.Value);
                    }
                    xdoc.Add(newElement);
                }
                return xdoc.Elements().Last();
            }
            catch(Exception exception)
            {
                MessageBox.Show(exception.Message);
                return xdoc;
            }
        }

        /// <summary>
        /// 查询单个节点数据
        /// </summary>
        /// <param name="nodePath">要查询数据的节点</param>
        /// <param name="num">要查询数据的索引号</param>
        /// <returns>返回数据字符串</returns>
        public string XmlRead(string nodePath, int num, string xmlConfigPath)
        {
            try
            {
                xmlDoc.Load(xmlConfigPath);
                XmlNodeList nodeList = xmlDoc.SelectSingleNode(nodePath).ChildNodes;
                return nodeList[num].InnerText;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        /// <summary>
        /// 对节点的子节点进行修改
        /// </summary>
        /// <param name="value">需要修改的子节点的键值对</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string ModifyXNode(Dictionary<string, string> value, XElement xElement, string xmlPath)
        {
            try
            {
                foreach (var item in value)
                {
                    xElement.Element(item.Key).SetValue((item.Value));
                }
                xdoc.Save(xmlPath);
                return "successful";
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return "failed";
            }
        }

        /// <summary>
        /// 增加节点
        /// </summary>
        /// <param name="value">需要修改的子节点的键值对</param>
        /// <param name="property">需要筛选的属性值的键值对</param>
        /// <param name="xmlPath">xml文件的路径</param>
        /// <param name="nodePath">节点的路径</param>
        /// <returns></returns>
        public string AddXNode(Dictionary<string, string> value, XElement xElement, string xmlPath)
        {
            try
            {
                if (value != null)
                {
                    foreach (var item in value)
                    {
                        XElement newXElement = new XElement(item.Key, item.Value);
                        xElement.Add(newXElement);
                    }
                }
                xdoc.Save(xmlPath);
                return "successful";
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return "failed";
            }
        }

        /// <summary>
        /// 筛选所需要的唯一节点元素
        /// </summary>
        /// <param name="property">需要满足的属性</param>
        /// <param name="xmlPath">读取的xml文件路径</param>
        /// <param name="nodePath">节点路径</param>
        /// <returns></returns>
        public XElement SelectOneXElement(Dictionary<string, string> property, string xmlPath,
            string nodePath)
        {
            //若键值对不为0,则需要对属性值进行筛选
            xdoc = XElement.Load(xmlPath);
            if (property != null)
            {
                IEnumerable<XElement> xElements = xdoc.Elements(nodePath);
                xElements = property.Aggregate(xElements,
                    (current, item) =>
                        (from e in current where e.Attributes(item.Key).First().Value.Equals(item.Value) select e));
                XElement xElement = xElements.First();
                return xElement;
            }
            else
            {
                XElement xElement = xdoc.Element(nodePath);
                return xElement;
            }
        }

        /// <summary>
        /// 修改节点元素的属性
        /// </summary>
        /// <param name="property">修改属性的键值对</param>
        /// <param name="xElement">所要修改的节点元素</param>
        /// <param name="xmlPath">保存的xml文件路径</param>
        /// <returns></returns>
        public string XNodeAttributes(Dictionary<string, string> property,
            XElement xElement, string xmlPath)
        {
            try
            {
                foreach (var item in property)
                {
                    xElement.SetAttributeValue(item.Key, item.Value);
                }
                xdoc.Save(xmlPath);
                return "successful";
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return "failed";
            }
        }

        /// <summary>
        /// 节点删除操作
        /// </summary>
        /// <param name="nodePath">要删除的节点</param>
        /// <param name="num">节点的索引</param>
        /// <param name="xmlConfigPath">所需删除节点的一系列xml文件</param>
        public string XElementDelete(string nodePath, string nodeName, string[] xmlConfigPath)
        {
            string result = "";
            foreach (string t in xmlConfigPath)
            {
                xmlDoc.Load(t);
                try
                {
                    var xmlNode = xmlDoc.SelectNodes(nodePath);
                    for (int i = 0; i < xmlNode.Count; i++)
                    {
                        result = "failed";
                        if (xmlNode[i].Attributes["Name"].InnerText == nodeName)
                        {
                            xmlNode[i].ParentNode.RemoveChild(xmlNode[i]);
                            xmlDoc.Save(t);
                            result = "successful";
                            break;
                        }
                    }                 
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                    result = "failed";
                }
            }
            return result;
        }

        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="nodePath">节点路径</param>
        /// <param name="xmlPath">读取的xml文件的路径</param>
        /// <returns>返回节点list</returns>
        public XmlNodeList XmlQuery(string nodePath, string xmlPath)
        {
            xmlDoc.Load(xmlPath);
            XmlNodeList xmlNodeList = xmlDoc.SelectNodes(nodePath);
            return xmlNodeList;
        }
    }
}