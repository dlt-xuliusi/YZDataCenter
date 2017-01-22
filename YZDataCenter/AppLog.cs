using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YZDataCenter
{
    class AppLog
    {
        static FileStream logStream;
        static StreamWriter logWriter;

        static object fileLock = new object();

        static ObjectPool<string> logPool = new ObjectPool<string>();

        public static void LogToFile(string txt)
        {
            try
            {
                lock (fileLock)
                {
                    if (logStream == null)
                    {
                        logStream = new FileStream("AppLog.txt", FileMode.OpenOrCreate);
                        logStream.Position = logStream.Length;
                        logWriter = new StreamWriter(logStream);
                    }
                    logWriter.Write(txt);
                    //清空缓冲区
                    logWriter.Flush();
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void Log(string txt, bool onlyToFile = false)
        {
            if (onlyToFile)
            {
                LogToFile(string.Format("{0}\t{1}\r\n", AppHelper.DateTimeToStr(DateTime.Now), txt));
                return;
            }
            LogToFile(string.Format("{0}\t{1}\r\n", AppHelper.DateTimeToStr(DateTime.Now), txt));
            logPool.PutObj(txt);
        }

        public static string GetLog()
        {
            string str = logPool.GetObj();
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            return str;
        }
    }

}
