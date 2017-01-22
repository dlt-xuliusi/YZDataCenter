using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// 根据排序后的信令，确定手机号与cgi关系
    /// </summary>
    class SignalDataClean
    {
        bool _threadStart = false;

        ObjectPool<SignalItem> _listSignal = new ObjectPool<SignalItem>();

        MsisdnParseManage _msisdnManage = new MsisdnParseManage();
        SignalFileSave _signalFileSave = new SignalFileSave();

        public event Action<SignalItem> EventSignalItem;

        public void Init()
        {
            StartThread();
            _msisdnManage.EventSignalItem += _msisdnManage_EventSignalItem;
        }

        private void _msisdnManage_EventSignalItem(SignalItem item)
        {
            EventSignalItem?.Invoke(item);
        }

        public void PutSignal(SignalItem item)
        {
            _listSignal.PutObj(item);
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
            int dealCount = 0;
            while (_threadStart)
            {
                dealCount = 0;
                // _signalFileSave.CreateSaveFile();
                dealCount += DealSignal();

                if (dealCount == 0)
                    Thread.Sleep(1);
            }
        }

        private int DealSignal()
        {
            int n = 0;
            while (true)
            {
                SignalItem item = _listSignal.GetObj();
                if (item == null)
                    return n;
                n++;

                item.MsisdnAdjust();
                item.ParseAction();
                //  _signalFileSave.SaveSignal(item);
                _msisdnManage.DealSignal(item);
            }
        }
    }

    public class SignalParam
    {
        //信令在多久内有效。和每个lac的周期位置更新时间有关
        public static int SignalValidateSpan = 45 * 60;

        public static int SignalCutoutSpan = 2;

        public static bool IsSignalValidate(SignalItem signal,DateTime curTime)
        {
            return ((curTime - signal.timeStamp).TotalSeconds <= SignalValidateSpan);
        }


        public static bool IsSameCgi(SignalItem old, SignalItem cur)
        {
            //老信令是切出，无法判断是否同一个cgi
            if(old.action == EN_Action.切出)
            {
                return false;
            }
            return old.lac == cur.lac && old.ci == cur.ci;
        }

        //与当前时间相比，是不是有效的切出命令
        public static bool IsCutoutValidate(SignalItem signal, DateTime curTime)
        {
            if (signal.action != EN_Action.切出)
                return false;

            return ((curTime - signal.timeStamp).TotalSeconds <= SignalCutoutSpan);
        }
    }
}
