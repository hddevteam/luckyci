using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common.TOOL;
using System.Windows;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace common.BL
{
    public class GitController
    {
        Tools tools = new Tools();
        /// <summary>
        /// 添加项目时候，第一次进行clone
        /// </summary>
        /// <param name="localRepository">本地git库</param>
        /// <param name="gitlabPath">gitlab远程库</param>
        /// <param name="cloneResult">返回的结果</param>
        /// <returns></returns>
        public string GitClone(string localRepository, string gitlabPath,out string cloneResult)
        {
            string checkOutLog="";
            string err="";
            string time="";      
            string args = "/C git clone " + gitlabPath + " " + localRepository;
            try {
                checkOutLog = tools.BuildProject("cmd.exe", args, null, out err, out time);
                cloneResult = "successful";
            }
            catch (Exception e) {
                cloneResult = "filed";
                checkOutLog = e.ToString();
            }
            return checkOutLog;
        }
        /// <summary>
        /// 获取gitlab远程库的代码，并且自动合并到本地。相当于svn的update
        /// </summary>
        /// <param name="localRepository"></param>
        /// <param name="gitPullResult"></param>
        /// <returns></returns>
        public string GitPull(string localRepository,out string  gitPullResult){
            string args = "/C git pull";

            string err = "";
            string time = "";
            string gitPullLog = "";
            try {
                 gitPullLog = tools.BuildProject("cmd.exe", args, localRepository, out err, out time).Replace("<br/>", "");
                gitPullResult = "successful";
                if (gitPullLog=="Already up-to-date.") { }
            }
            catch (Exception ex)
            {
                gitPullResult = "failed";
                gitPullLog = ex.ToString();
                return gitPullLog;
            }    
            return gitPullLog;
        }
        //获取上次提交者的名字
        public string GirLogFundAuthor(string workDirectory) {
            string err = "";
            string time = "";
            //string  result = tools.BuildProject("cmd.exe", "/C git log -1", @"C:\Users\ccy_god\Desktop\123", out err, out time);
            string result = tools.BuildProject("cmd.exe", "/C git log -1", workDirectory, out err, out time);
              string[] checkAuthor=result.Replace("<br/>", "\\").Split('\\');
            for (int i=0;i<checkAuthor.Length;i++)
            {
                if (checkAuthor[i].Contains("Author")) { result = checkAuthor[i];break; }
            }
            return result.Split(':')[1].Split('<')[0];
        }

        //利用libgit2sharp进行git pull命令
        public Boolean Libgit2_GitPull(string localpath,string username,string password,string emailaddress)
        {
            try {
                using (var repo = new Repository(localpath))
                {
                    LibGit2Sharp.PullOptions options = new LibGit2Sharp.PullOptions();
                    options.FetchOptions = new FetchOptions();
                    options.FetchOptions.CredentialsProvider = new CredentialsHandler(
                        (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials()
                            {
                                Username = username,
                                Password = password
                            });
                    repo.Network.Pull(new LibGit2Sharp.Signature(username, emailaddress, new DateTimeOffset(DateTime.Now)), options);
                    return true;
                }
            }
            catch (Exception e) { return false; }
        }
        //利用libgit2sharp进行git log -1命令，并且仅仅获取版本号字符串
        public string libgit2_GitLog(string localpath)
        {
            string reversion = "";
            var repo = new LibGit2Sharp.Repository(localpath);
            foreach (Commit commit in repo.Commits)
            {
                foreach (var parent in commit.Parents)
                {
                    reversion= commit.Sha;
                    break;
                  
                }
                break;
            }
            return reversion;
        }

        /// <summary>
        /// 进行分支的切换操作
        /// </summary>
        /// <param name="checkoutCommand">切换分支命名</param>
        /// <param name="workDirectory">工作路径</param>
        /// <returns></returns>
        public Boolean git_checkout(string checkoutCommand,string workDirectory) {
            string err = "";
            string time = "";
            try {
                string result = tools.BuildProject("cmd.exe", "/C " + checkoutCommand, workDirectory, out err, out time);
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}
