using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace common.TOOL
{
   public class Tools
    {
        public static string output = "";
        public static string error = "";
        /// <summary>
        /// 公共操作.process方法
        /// </summary>
        /// <param name="fileName">隐式运行的文件名</param>
        /// <param name="args">执行的语句</param>
        /// <param name="workDirectory">工程工作路径</param>
        /// <returns>执行后返回的日志</returns>
        public string BuildProject(string fileName, string args, string workDirectory,out string err,out string time)
        {
            output = "";
            error = "";
            time = "";
            Process process = new Process();
            process.StartInfo.WorkingDirectory = workDirectory;
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(process_Exited);
            //注：同时获取output以及error信息时，直接用process.StandardOutput.ReadToEnd();process.StandardError.ReadToEnd();
            //会产生死锁的状况，程序一直运行；所以用异步获取两个信息
            process.OutputDataReceived +=new DataReceivedEventHandler(OutPutOnDataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorOnDataReceived);         
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();  
            process.WaitForExit();
            TimeSpan timeSpan = process.ExitTime.Subtract(process.StartTime);
            time += timeSpan.Hours == 0 ? "" : timeSpan.Hours + " h";
            time += timeSpan.Minutes == 0 ? "" : timeSpan.Minutes + " min ";
            time += timeSpan.Seconds == 0 ? "0" : timeSpan.Seconds+"";
            time += timeSpan.Milliseconds == 0 ? " secs" : "." + timeSpan.Milliseconds.ToString() + " secs";
            process.Close();

            err = error.Replace("\n", "<br/>");
            return output.Replace("\n", "<br/>");
        }

        /// <summary>
        /// 异步进行获取output的信息
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private static void OutPutOnDataReceived(object Sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                output += e.Data+"\n";
            }
        }
        /// <summary>
        /// 异步获取error的信息
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private static void ErrorOnDataReceived(object Sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                error += e.Data+"\n";

            }
        }
        /// <summary>
        /// 退出时需要进行的操作
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        private static void process_Exited(object Sender, EventArgs e)
        {
            
        }
        /// <summary>
        /// Fram承载页面的资源加载
        /// </summary>
        /// <param name="fram">FramName</param>
        /// <param name="pageSource">要加载的资源</param>
        /// <returns></returns>
        public string PageSource(Frame frame,string pageSource)
        {
            try
            {
                frame.Source = new Uri(pageSource, UriKind.Relative);
                return "successful";
            }
            catch
            {
                return "false";
            }
        }
    }
}
