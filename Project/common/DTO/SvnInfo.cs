using SharpSvn;
using System;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows;

namespace common.DTO
{
  public  class SvnInfo
    {
        //------------------SvnInfo类封装的私有属性---------------------------------------
        //svn在本机中的启动目录
        private string svnPath;
        //svn检索文件路径
        private string repositoryPath;
        //svn工作文件夹
        private string workDirectory;
        //svn登陆用户名
        private string userName;
        //svn登陆密码
        private string userPassword;
        //---------------------------------------------------------

        //------------------SvnInfo类的无参数构造方法---------------------------------------
        public SvnInfo()
        {
            this.svnPath = "";
            this.repositoryPath = "";
            this.workDirectory = "";
            this.userName = "";
            this.userPassword = "";
        }
        //---------------------------------------------------------

        //------------------SvnInfo类对外提供的用于访问私有属性的public方法---------------------------------------
        public string Svnpath
        {
            get { return svnPath; }
            set { svnPath = value; }
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

        public string Username
        {
            get { return userName; }
            set { userName = value; }
        }

        public string Userpassword
        {
            get { return userPassword; }
            set { userPassword = value; }
        }
        //---------------------------------------------------------
    }
}
