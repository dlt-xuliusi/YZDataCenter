using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace YZDataCenter
{
    public class AppParam
    {
        public static bool _autoStart = false;

        public static int _maxSignalReadSpeed = 10000;

        static string _signalReadDir = string.Empty;
        static string _signalReadDirEnd = string.Empty;

        public static int SignalReadSpeedThisSecond = 10000;

        public static string SignalSortSaveDir = string.Empty;

        public static ushort NetListenPort = 8088;

        public static string AreaCodeFile = string.Empty;

        public static string SignalZipFileDir = string.Empty;     
        //已处理的压缩文件
        public static string SignalZipFileDealedDir = string.Empty;     

        public static string SignalReadDir
        {
            get
            {
                return _signalReadDir;
            }
        }

        public static string SignalReadDirEnd
        {
            get
            {
                return _signalReadDirEnd;
            }
        }


        public static void Init()
        {
            _signalReadDir = GetConfigVauleRaw("SignalReadDir", string.Empty);
            _signalReadDirEnd = GetConfigVauleRaw("SignalReadDirEnd", string.Empty);
            SignalSortSaveDir = GetConfigVauleRaw("SignalSortSaveDir", string.Empty);
            AreaCodeFile = GetConfigVauleRaw("AreaCodeFile", string.Empty);
            SignalZipFileDir = GetConfigVauleRaw("SignalZipFileDir", string.Empty);
            SignalZipFileDealedDir = GetConfigVauleRaw("SignalZipFileDealedDir", string.Empty);
         _maxSignalReadSpeed = AppHelper.IntParse(GetConfigVauleRaw("MaxSignalReadSpeed", "10000"));

            NetListenPort = (ushort)AppHelper.IntParse(GetConfigVauleRaw("NetListenPort", "8088"));
        }

        public static string GetConfigVauleRaw(string name, string defaultvalue)
        {
            try
            {
                string value = ConfigurationManager.AppSettings[name];
                return value;
            }
            catch (Exception ex)
            {
                return defaultvalue;
            }
        }

        public static int GetConfigVauleInt(string name, int defaultvalue = 0)
        {
            try
            {
                string str = GetConfigVauleRaw(name, defaultvalue.ToString());
                return int.Parse(str);
            }
            catch (Exception ex)
            {
                return defaultvalue;
            }
        }

        public static bool SaveConfigValueRaw(string newKey, string newValue)
        {
            try
            {
                bool isModified = false;
                foreach (string key in ConfigurationManager.AppSettings)
                {
                    if (key == newKey)
                    {
                        isModified = true;
                    }
                }

                // Open App.Config of executable  
                Configuration config =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                // You need to remove the old settings object before you can replace it  
                if (isModified)
                {
                    config.AppSettings.Settings.Remove(newKey);
                }
                // Add an Application Setting.  
                config.AppSettings.Settings.Add(newKey, newValue);
                // Save the changes in App.config file.  
                config.Save(ConfigurationSaveMode.Modified);
                // Force a reload of a changed section.  
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //public static string GetAppVer()
        //{
        //    try
        //    {
        //        string ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        //        DateTime date = System.IO.File.GetLastWriteTime(typeof(AppParam).Assembly.Location);
        //        return string.Format("--版本:{0}--日期:{1}", ver, AppHelper.DateTimeToStr(date));
        //    }
        //    catch (Exception ex)
        //    {
        //        return string.Empty;
        //    }
        //}
    }
}
