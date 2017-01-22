using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// 负责读取信令文件，并对读取速度控制
    /// </summary>
    class SignalFileManage
    {
        bool _threadStart = false;

        SignalFileParse _signalFileParse = new SignalFileParse();

        Dictionary<string, SignalFileParam> _signalFilegroup = new Dictionary<string, SignalFileParam>();

        SortSignalManage _sortSignalManage = new SortSignalManage();

        public event Action<SignalItem> EventSignalItem;

        public long SignalReadCount { get; set; }
        public long SignalSortPool
        {
            get
            {
                return _sortSignalManage.PoolCount;
            }
        }

        public long SignalSortPutCount
        {
            get
            {
                return _sortSignalManage.PutSignalCount;
            }
        }

        public SortSignalManage SortSignalManage
        {
            get
            {
                return _sortSignalManage;
            }
        }

        public long SignalFilePoolCount
        {
            get
            {
                return _signalReadPool.CurPoolCount;
            }
        }

        public void Init()
        {
            _sortSignalManage.PoolTimeSpan = TimeSpan.FromMinutes(5);

            _signalFileParse.PutSignal += PutSignalItem;
            _signalFileParse.EventReadFileEnd += EventReadFileEnd;
            _signalFileParse.Init();

            StartThread();
            AppLog.Log("信令文件读取模块启动");
        }

        void StartThread()
        {
            if (!_threadStart)
            {
                _threadStart = true;
                Thread thread1 = new Thread(new ThreadStart(ThreadProc));
                thread1.Start();
            }
        }

        private void ThreadProc()
        {
            AppMain.AreaCodeMap.ReloadFile(AppParam.AreaCodeFile);

            int deal;
            while (_threadStart)
            {
                deal = 0;
                deal += ReadSignalFromFilePool();
                deal += ReadSignalFileFromDir();
                deal += DealSignalPool();

                if (deal == 0)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void EventReadFileEnd(ReadFileStat stat)
        {
            lock (_signalFilegroup)
            {
                TimeSpan span = stat.EndRead - stat.StartRead;
                int speed = 0;
                if (span.TotalSeconds != 0)
                {
                    speed = (int)(stat.LineCount / span.TotalSeconds);
                }

                AppLog.Log(string.Format("读取文件完毕；{0}，个数:{1} 耗时:{2:N2}毫秒 速度:{3}",
                   Path.GetFileName(stat.FileName), stat.LineCount, span.TotalMilliseconds, speed));

                if (!_signalFilegroup.ContainsKey(stat.FileName))
                {
                    Debug.Assert(false);
                    return;
                }

                //文件移动到别的目录
                string newFileName = Path.Combine(AppParam.SignalReadDirEnd, Path.GetFileName(stat.FileName));
                try
                {
                    File.Delete(newFileName);
                    File.Move(stat.FileName, newFileName);
                }
                catch (Exception ex)
                {
                    AppLog.Log(ex.Message);
                    File.Delete(stat.FileName);
                }

                if (!File.Exists(stat.FileName))
                {
                    _signalFilegroup.Remove(stat.FileName);
                }
                else //本地磁盘文件无法删除？
                {
                    _signalFilegroup[stat.FileName].FileStat = EN_FileDealStat.en_处理完毕;
                }

            }
        }

        ObjectPool<SignalItem> _signalReadPool = new ObjectPool<SignalItem>();
        public bool PutSignalItem(SignalItem item)
        {
            if (AppParam.SignalReadSpeedThisSecond <= 0)
                return false;

            AppParam.SignalReadSpeedThisSecond--;

            SignalReadCount++;
            _signalReadPool.PutObj(item);
            return true;
        }

        private int ReadSignalFromFilePool()
        {
          //  return 0;
            int n = 0;
            while (true)
            {
                SignalItem item = _signalReadPool.GetObj();
                if (item == null)
                {
                    return n;
                }

                n++;
               if(!_sortSignalManage.PutSignalItem(item))
                {
                    OnReadUnsortSignal(item);
                }

                if (n > 2000)
                    break;
            }
            return n;
        }

        //无法参与排序的信令
        private void OnReadUnsortSignal(SignalItem item)
        {
            EventSignalItem?.Invoke(item);
        }

        DateTime _lastDealSignalTime = DateTime.MinValue;
        private int DealSignalPool()
        {
            int n = 0;
            while (true)
            {
                List <SignalItem> items = _sortSignalManage.GetSignalItem();
                if (items == null)
                {
                    return n;
                }

                n++;
                foreach (SignalItem item in items)
                {
                    EventSignalItem?.Invoke(item);
                }
            }
        }

        DateTime _lastGetSignalFile = DateTime.MinValue;
        /// <summary>
        /// 从目录下读取文件
        /// </summary>
        int ReadSignalFileFromDir()
        {
            int n = 0;
            if (_signalFileParse.HaveFileInDeal)
                return n;

            DirectoryInfo directory = new DirectoryInfo(AppParam.SignalReadDir);
            FileInfo[] fileInfoArray = directory.GetFiles().
                Where(o=>!o.Name.EndsWith(SignalZipFileManage.FileDoingFlag)).
                OrderBy(o=>AppHelper.GetTimeFromSignalFile(o.Name)).ToArray();

            lock (_signalFilegroup)
            {
                foreach (FileInfo file in fileInfoArray)
                {
                    if (!_signalFilegroup.ContainsKey(file.FullName))
                    {
                        SignalFileParam param = new SignalFileParam();
                        param.FileName = file.FullName;
                        param.FileDate = DateTime.Now;
                        param.FileStat = EN_FileDealStat.en_正在处理;
                        _signalFilegroup.Add(file.FullName, param);

                        _signalFileParse.ParseFileAsyn(file.FullName);
                        n++;
                    }
                }
            }

            _lastGetSignalFile = DateTime.Now;
            return n;
        }

        class SignalFileParam
        {
            public string FileName;
            public DateTime FileDate;//文件名指示的日期
            public EN_FileDealStat FileStat;
        }
    }

    public delegate bool Delegate_PutItem(SignalItem item);

    public class ReadFileStat
    {
        public string FileName { get; set; }
        public int LineCount { get; set; }
        public DateTime StartRead { get; set; }
        public DateTime EndRead { get; set; }
    }

    enum EN_FileDealStat
    {
        en_未处理,
        en_正在处理,
        en_处理完毕,
    }
}
