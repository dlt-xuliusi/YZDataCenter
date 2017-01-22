using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// 对信令文件解析
    /// </summary>
    class SignalFileParse
    {
        bool _threadStart = false;

        public ObjectPool<string> _listFileName = new ObjectPool<string>();

        public event Action<ReadFileStat> EventReadFileEnd;
        public Delegate_PutItem PutSignal = null;

        AutoResetEvent _fileDealEvent = new AutoResetEvent(false);

        public void Init()
        {
            StartThread();
        }

        public void ParseFileAsyn(string fileName)
        {
            _listFileName.PutObj(fileName);
            _fileDealEvent.Set();
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

        public bool HaveFileInDeal
        {
            get
            {
                return _inParseFile || _listFileName.CurPoolCount > 0;
            }
        }

        bool _inParseFile;
        private void ThreadProc()
        {
            string fileName;
            while (_threadStart)
            {
                while (true)
                {
                    fileName = _listFileName.GetObj();
                    if (string.IsNullOrEmpty(fileName))
                    {
                        break;
                    }
                    _inParseFile = true;
                    ParseFile(fileName);
                    _inParseFile = false;
                }

                _fileDealEvent.WaitOne(TimeSpan.FromSeconds(1));
            }
        }

        void ParseFile(string fileName)
        {
            int lineCount = 0;

            ReadFileStat stat = new ReadFileStat() { FileName = fileName, StartRead = DateTime.Now };
            string line;
            try
            {
                using (StreamReader sr = new StreamReader(fileName, Encoding.ASCII))
                {
                    while (true)
                    {
                        line = sr.ReadLine();
                        if (line == null)
                            break;
                        SignalItem item = SignalItem.FromTxtLine(line);
                        if (item == null)
                        {
                            Debug.Assert(false);
                            continue;
                        }

                        lineCount++;
                        //防止短时间读取大量数据
                        while (!PutSignal(item))
                        {
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                stat.LineCount = lineCount;
                stat.EndRead = DateTime.Now;
                EventReadFileEnd?.Invoke(stat);
            }
        }
    }
}
