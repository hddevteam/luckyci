using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.DTO
{
   public class GitInfo
    {
        //------------------ConfigInfo类封装的私有属性---------------------------------------
        //gitlab上用户名
        private string username;
        //gitlab用户密码
        private string password;
        //关联邮件
        private string emailaddress;
        //版本号字符串
        private string gitreversion;

        public string Username
        {
            get
            {
                return username;
            }

            set
            {
                username = value;
            }
        }

        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
            }
        }

        public string Emailaddress
        {
            get
            {
                return emailaddress;
            }

            set
            {
                emailaddress = value;
            }
        }

        public string Gitreversion
        {
            get
            {
                return gitreversion;
            }

            set
            {
                gitreversion = value;
            }
        }

        //---------------------------------------------------------

        //------------------ConfigInfo类的无参数构造方法---------------------------------------
        public GitInfo()
        {
            this.Username = "";
            this.Password = "";
            this.Emailaddress = "";
            this.Gitreversion = "";
        }
    }
}
