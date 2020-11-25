using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScannerDataWindowsService
{
    public class Log
    {
        public static void WriteLog(params object[] strList)
        {
            try
            {
                //如果传过strList无内容，直接返回，不写日志
                if (strList == null) return;
                if (strList.Count() == 0) return;
                //获取路径
                string strDicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
                //创建日志路径
                string strPath = Path.Combine(strDicPath, string.Format("{0:yyyy年-MM月-dd日}", DateTime.Now) + "日志记录.txt");
                //如果服务器路径不存在，就创建一个
                if (!Directory.Exists(strDicPath)) Directory.CreateDirectory(strDicPath);
                //如果日志文件不存在，创建一个
                if (!File.Exists(strPath)) using (FileStream fs = File.Create(strPath)) { };
                StringBuilder sb = new StringBuilder();
                //将错误信息写入sb
                foreach (var item in strList)
                {
                    sb.Append("\r\n" + DateTime.Now.ToString() + "-----" + item + "");
                }
                //将错误信息写入txt
                File.AppendAllText(strPath, sb.ToString() + "\r\n\r\n");
            }
            catch (Exception ex)
            {
                WriteLog(ex);
            }
        }
    }
}
