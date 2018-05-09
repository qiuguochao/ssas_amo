using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace sh3h.ProcessingCube
{
    public class CommonToos
    {
        public static string BeginLog()
        {
            Console.WriteLine("KPI- 多维数据集开始处理，完成将自动关闭");
            return string.Format("\r\n" + "------------------{0}------------------------\r\n", DateTime.Now.ToString());
        }
        public static string EndLog()
        {
            return  "-----------------------------------------------------------\r\n";
        }
        public static string DBLog(string DBName)
        {
            Console.WriteLine(string.Format("开始对数据库【{0}】进行处理", DBName));
            return string.Format("【{0}】\r\n", DBName);
        }
        public static string DimOrCubeLog(string name,TimeSpan ts)
        {
            Console.WriteLine(string.Format("【{0}】处理完成", name));
            return string.Format("{0:D10} - 耗时：{1}\r\n\r\n", name,ts);
        }
        public static string ErrorLog(string name, string msg)
        {
            Console.WriteLine(string.Format("【{0}】处理出错", name));
            return string.Format("{0:D10} - Error：{1}\r\n\r\n", name, msg);
        }
        public static string TotalTime(TimeSpan ts)
        {
            return string.Format("本次总耗时：{0}\r\n", ts);
        }
        public static string appendLine()
        {
            return "\r\n";
        }
        public static void WriteLogFile(string str)
        {
            Console.WriteLine("写入日志");
            string fileFullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "oneWeek.log");
            StreamWriter sw;

            if (DateTime.Now.ToString("dddd")=="星期一" || !File.Exists(fileFullPath))
            {
                sw = File.CreateText(fileFullPath);
            }
            else
            {
                sw = File.AppendText(fileFullPath);
            }
            sw.WriteLine(str.ToString());
            sw.Close();
        }

    }
}
