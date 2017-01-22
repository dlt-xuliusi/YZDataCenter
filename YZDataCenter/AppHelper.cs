using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.IO;

namespace YZDataCenter
{
    public class AppHelper
    {
        public static DateTime IntToLocalTime(long nTime)
        {
            DateTime time = new DateTime(1970, 1, 1).AddSeconds(nTime);
            time = time.ToLocalTime();
            return time;
        }

        public static string DateTimeToStr(DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string DateTimeNow
        {
            get
            {
                return DateTimeToStr(DateTime.Now);
            }
        }

        public static string DateToStr(DateTime time)
        {
            return time.ToString("yyyy-MM-dd");
        }

        public static bool IsTimeConform(DateTime startTime, DateTime endTime)
        {
            DateTime now = DateTime.Now;
            DateTime startTime2 = new DateTime(now.Year, now.Month, now.Day, startTime.Hour, startTime.Minute, startTime.Second);
            DateTime endTime2 = new DateTime(now.Year, now.Month, now.Day, endTime.Hour, endTime.Minute, endTime.Second);
            return now >= startTime2 && now < endTime2;
        }

        public static int DateTime_DaySpan(DateTime startTime, DateTime endTime)
        {
            TimeSpan span = startTime - endTime;
            int n = (int)span.TotalDays;
            return n;
        }

        public static byte[] ByteCombine(params byte[][] bytes)
        {
            int len = 0;
            foreach (byte[] item in bytes)
            {
                len += item.Length;
            }

            byte[] result = new byte[len];
            int index = 0;
            foreach (byte[] item in bytes)
            {
                Array.Copy(item, 0, result, index, item.Length);
                index += item.Length;
            }
            return result;
        }

        public static List<DateTime> GetDateTimeList(DateTime startTime, DateTime endTime)
        {
            Debug.Assert((endTime - startTime).TotalSeconds > 0);

            List<DateTime> result = new List<DateTime>();
            result.Add(startTime);

            DateTime begin = startTime;
            while (true)
            {
                begin = begin.AddDays(1);
                TimeSpan span = begin - endTime;
                int day = (int)span.TotalDays;
                if (day > 0)
                    break;
                result.Add(begin);
            }
            return result;
        }

        /// <summary>
        ///  根据日期的时间部分，计算时间差。（最多时间差为一天）
        /// </summary>
        /// <param name="timePartOfStart">日期的时间部分</param>
        /// <param name="endTime">结束日期</param>
        /// <returns></returns>
        public static int TimeSpanSecond(DateTime timePartOfStart, DateTime endTime)
        {
            DateTime dayPartOfStart = DateTime.Now;
            int startTimeCount = timePartOfStart.Hour * 3600 + timePartOfStart.Minute * 60 + timePartOfStart.Second;
            int endTimeCount = endTime.Hour * 3600 + endTime.Minute * 60 + endTime.Second;
            if(endTimeCount >= startTimeCount)
            {

            }
            else
            {
                //时间到了第二天，开始的日期从昨天算起
                dayPartOfStart = dayPartOfStart.AddDays(-1);
            }

            DateTime startTime = new DateTime(dayPartOfStart.Year, dayPartOfStart.Month, dayPartOfStart.Day, timePartOfStart.Hour, timePartOfStart.Minute, timePartOfStart.Second);
            return (int)(endTime - startTime).TotalSeconds;
        }

        public static DateTime GetMaxTimeOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
        }
        public static DateTime GetMinTimeOfDay(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        }


        internal static List<int> ParseInt(string txt, char separator)
        {
            List<int> result = new List<int>();
            List<string> strs = txt.Split(separator).ToList();
            strs.ForEach(o => o = o.Trim());
            result = strs.Where(o => AppHelper.IntParse(o) != 0).Select(o=> AppHelper.IntParse(o)).ToList();
            return result;
        }


        /// <summary>
        /// datetime 时间部分比较，不包含日期部分
        ///time1>time2;返回1
        ///time1=time2;返回0
        ///time1<time2;返回-1
        /// </summary>
        /// <param name="time1"></param>
        /// <param name="time2"></param>
        /// <returns></returns>
        public static int TimePartCompare(DateTime time1, DateTime time2)
        {
            if (time1.Hour > time2.Hour)
                return 1;
            if (time1.Hour < time2.Hour)
                return -1;

            if (time1.Minute > time2.Minute)
                return 1;
            if (time1.Minute < time2.Minute)
                return -1;

            if (time1.Second > time2.Second)
                return 1;
            if (time1.Second < time2.Second)
                return -1;
            return 0;
        }

        public static string GetTimeSpanStr(TimeSpan span)
        {
            string ret = string.Empty;
            if (span.Days > 0)
            {
                ret += string.Format("{0}天", span.Days);
            }
            if (span.Hours > 0)
            {
                ret += string.Format("{0}时", span.Hours);
            }
            if (span.Minutes > 0)
            {
                ret += string.Format("{0}分", span.Minutes);
            }
            if (span.Seconds > 0)
            {
                ret += string.Format("{0}秒", span.Seconds);
            }
            if (ret == string.Empty)
                return "0秒";
            return ret;
        }

        public static string GetPassword(PasswordBox passwordBox)
        {
            IntPtr p = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(passwordBox.SecurePassword);
            string password = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(p);
            return password;
        }

        public static int IntParse(string n, int defaultValue = 0)
        {
            int result;
            if(int.TryParse(n,out result))
            {
                return result;
            }
            return defaultValue;
        }

        public static UInt32 UintParse(string n, UInt32 defaultValue = 0)
        {
            UInt32 result;
            if (UInt32.TryParse(n, out result))
            {
                return result;
            }
            return defaultValue;
        }

        public static DateTime DateTimeParse(string n)
        {
            try
            {
                return DateTime.Parse(n);
            }
            catch (Exception)
            {

                return DateTime.Now;
            }
        }

        public static ushort UshotParse(string n, ushort defaultValue = 0)
        {
            ushort result;
            if(ushort.TryParse(n, out result))
            {
                return result;
            }
            return defaultValue;
        }

        public static HashSet<int> KeysToHashSet(Dictionary<int, int> group)
        {
            //生成返回值
            HashSet<int> ret = new HashSet<int>();
            foreach (int n in group.Keys)
            {
                ret.Add(n);
            }
            return ret;
        }

        public static bool IsSameDay(DateTime d1, DateTime d2)
        {
            return d1.Day == d2.Day && d1.Month == d2.Month && d1.Year == d2.Year;
        }

        public static void AddValueOne(Dictionary<int, int> group, int key)
        {
            if (group.ContainsKey(key))
            {
                int value = group[key];
                group.Remove(key);
                group.Add(key, value + 1);
            }
            else
            {
                group.Add(key, 1);
            }
        }

        public static int IpToInt(string ip)
        {
            string[] items = ip.Split('.');
            if (items.Length != 4)
                return 0;
            int result = 0;

            result += (int.Parse(items[3]) << 24);
            result += (int.Parse(items[2]) << 16);
            result += (int.Parse(items[1]) << 8);
            result += int.Parse(items[0]);
            return result;
        }

        public static bool IsDigit(string str)
        {
            foreach (char ch in str)
            {
                if (!char.IsDigit(ch))
                    return false;
            }
            return true;
        }

        public static void OneCharToTwo(byte[] src, int offset, int len, byte[] dest)
        {
            int n;
            int index = 0;
            for (int i = offset; i < offset + len; i++)
            {
                byte val = src[i];
                n = (val % 16);
                if (n <= 9)
                    dest[2 * index + 1] = (byte)('0' + n);
                else
                    dest[2 * index + 1] = (byte)('A' + n - 10);

                n = (val >> 4);
                if (n <= 9)
                    dest[2 * index] = (byte)('0' + n);
                else
                    dest[2 * index] = (byte)('A' + n - 10);
                index++;
            }
        }

        public static int AreaCodeToInt(byte[] src, int offset)
        {
            byte[] temp = new byte[5];
            OneCharToTwo(src, offset, 2, temp);
            string str = ASCII_GetString(temp, 0, 5);
            return IntParse(str);
        }

        public static string ASCII_GetString(byte[] bytes, int offset, int count)
        {
            int realCount = count;
            for (int i = 0; i < count; i++)
            {
                if (bytes[offset + i] == 0)
                {
                    realCount = i;
                    break;
                }
            }
            string result = Encoding.ASCII.GetString(bytes, offset, realCount);
            return result;
        }

        public static string ValueCountToStr(Dictionary<int, int> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach(int key in data.Keys)
            {
                sb.AppendFormat("{0}:{1};", key, data[key]);
            }
            return sb.ToString();
        }

        public static string ValueToStr(List<int> data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int item in data)
            {
                sb.AppendFormat("{0};", item);
            }
            return sb.ToString();
        }

        public static byte[] GetByte_ASCII(string item)
        {
            byte[] result = System.Text.Encoding.ASCII.GetBytes(item);
            return result;
        }

        public static string ByteToString_ASCII(byte[] data, int offset, int count)
        {
            int i = 0;
            for (; i < count; i++)
            {
                if (data[offset + i] == 0)
                {
                    count = i;
                    break;
                }
            }
            string result = System.Text.Encoding.ASCII.GetString(data, offset, count);
            return result;
        }

        public static DateTime GetTimeFromSignalFile(string fileName)
        {
            //20160830_0145ZF01_new5.tmp
            try
            {
                fileName = Path.GetFileName(fileName);
                int year = int.Parse(fileName.Substring(0, 4));
                int month = int.Parse(fileName.Substring(4, 2));
                int day = int.Parse(fileName.Substring(6, 2));
                int hour = int.Parse(fileName.Substring(9, 2));
                int minute = int.Parse(fileName.Substring(11, 2));
                return new DateTime(year, month, day, hour, minute, 0);
            }
            catch (Exception ex)
            {
                return DateTime.MinValue;
            }
        }
    }

    public class MyTimeSpan
    {
        Stopwatch MyWatch;
        public MyTimeSpan()
        {
            MyWatch = new Stopwatch();
            MyWatch.Start();
        }

        public void Restart()
        {
            MyWatch.Restart();
        }

        //public long ElapsedMilliseconds
        //{
        //    get
        //    {
        //        MyWatch.Stop();
        //        return MyWatch.ElapsedMilliseconds;
        //    }
        //}

        public double ElapsedMilliseconds
        {
            get
            {
                MyWatch.Stop();
                double result = ((double)MyWatch.ElapsedTicks) / Stopwatch.Frequency;
                result = result * 1000;
                return result;
            }
        }
    }
}
