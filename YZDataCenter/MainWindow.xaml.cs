using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {

        }

        DateTime _startTime = DateTime.Now;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (object item in Enum.GetValues(typeof(EN_Action)))
            {
                comboAction.Items.Add(item);
            };
            comboAction.SelectedIndex = 0;

            AppMain.Init();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(1000);   //间隔1秒
            _timer.Tick += new EventHandler(TimeUp);
            _timer.Start();
        }

        private void TimeUp(object sender, EventArgs e)
        {
            MsisdnParseStat.RecalStat();

            AppParam.SignalReadSpeedThisSecond = AppParam._maxSignalReadSpeed;

            txtAppTime.Text = AppHelper.GetTimeSpanStr(DateTime.Now- _startTime);
            ShowLog();
            ShowStat();
        }


        long _signalReadCount = 0;
        long _signalSortPutCount = 0;

        long _netSendToPeer = 0;

        private void ShowStat()
        {
            //状态日志
            ListStatInfo.Items.Clear();
            ListItemValue stat = new ListItemValue();
            stat.Str1 = "信令读取个数:速度";

            long speed = AppMain.SignalFileManage.SignalReadCount - _signalReadCount;
            _signalReadCount = AppMain.SignalFileManage.SignalReadCount;
            stat.Str2 = string.Format("{0}:{1}", _signalReadCount, speed);
            ListStatInfo.Items.Add(stat);

            stat = new ListItemValue();
            stat.Str1 = "文件读取信令缓冲：排序缓冲";
            stat.Str2 = string.Format("{0} : {1}",
                 AppMain.SignalFileManage.SignalFilePoolCount, AppMain.SignalFileManage.SignalSortPool);
            ListStatInfo.Items.Add(stat);

            stat = new ListItemValue();
            stat.Str1 = "排序处理个数:速度";
            speed = AppMain.SignalFileManage.SignalSortPutCount - _signalSortPutCount;
            _signalSortPutCount = AppMain.SignalFileManage.SignalSortPutCount;
            stat.Str2 = string.Format("{0}:{1}", _signalSortPutCount, speed);
            ListStatInfo.Items.Add(stat);

            stat = new ListItemValue();
            stat.Str1 = "排序缓冲信令 起止时间";
            DateTime dtStart = AppMain.SignalFileManage.SortSignalManage.SignalStartTime;
            DateTime dtEnd = AppMain.SignalFileManage.SortSignalManage.SignalEndTime;
            stat.Str2 = string.Format("{0} : {1}", SignalDefine.DateTimeToStr_Second(dtStart), SignalDefine.DateTimeToStr_Second(dtEnd));
            ListStatInfo.Items.Add(stat);

            stat = new ListItemValue();
            stat.Str1 = "发包个数:速度 -- 发包失败";
            speed = AppMain.NetServer.SendToPeerCount - _netSendToPeer;
            _netSendToPeer = AppMain.NetServer.SendToPeerCount;
            stat.Str2 = string.Format("{0}:{1} -- {2}",
                _netSendToPeer,speed, AppMain.NetServer.SendToPeerFailCount);
            ListStatInfo.Items.Add(stat);

            //   public static long SortSignalThisDay = 0;
            //public static long SignalNotNeedParseThisDay = 0;
            //public static long SignalNeedParseThisDay = 0;
            //public static long SignalParseOkThisDay = 0;

            stat = new ListItemValue();
            stat.Str1 = "当天排序信令统计- 总个数:不需要分析:需要分析:分析成功";
            stat.Str2 = string.Format("{0}:{1}--{2}:{3}",
                MsisdnParseStat.SortSignalThisDay, MsisdnParseStat.SignalNotNeedParseThisDay,
                MsisdnParseStat.SignalNeedParseThisDay, MsisdnParseStat.SignalParseOkThisDay);
            ListStatInfo.Items.Add(stat);

            stat = new ListItemValue();
            stat.Str1 = "当天非排序信令统计";
            stat.Str2 = string.Format("{0}",MsisdnParseStat.TotalSignalUnsortThisDay);
            ListStatInfo.Items.Add(stat);
        }

        private void ShowLog()
        {
            //显示日志
            while (true)
            {
                string item = AppLog.GetLog();
                if (item == string.Empty)
                    break;

                ListItemValue strs = new ListItemValue();
                strs.tag = item;
                strs.Str1 = (ListLogInfo.Items.Count + 1).ToString();
                strs.Str2 = AppHelper.DateTimeToStr(DateTime.Now);
                strs.Str3 = item;
                ListLogInfo.Items.Add(strs);

                while (ListLogInfo.Items.Count > 2000)
                    ListLogInfo.Items.RemoveAt(0);
            }
        }

        private void menuItemCopySel_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder txt = new StringBuilder();
            foreach (ListItemValue item in ListLogInfo.SelectedItems)
            {
                txt.AppendFormat("{0}\r\n", item.ToString());
            }
            Clipboard.SetDataObject(txt.ToString());
        }

        private void menuItemCopyAll_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder txt = new StringBuilder();
            foreach (ListItemValue item in ListLogInfo.Items)
            {
                txt.AppendFormat("{0}\r\n", item.ToString());
            }
            Clipboard.SetDataObject(txt.ToString());
        }

        private void menuItemClearAll_Click(object sender, RoutedEventArgs e)
        {
            ListLogInfo.Items.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("确定退出程序？", "询问", MessageBoxButton.YesNo,
               MessageBoxImage.Question, MessageBoxResult.No);
            //关闭窗口
            if (result == MessageBoxResult.Yes)
            {
                Environment.Exit(0);
            }
            //不关闭窗口
            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        private void btnSingalInput_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}|", txtSignalTime.Text);

                EN_Action action = (EN_Action)Enum.Parse(typeof(EN_Action), comboAction.SelectedValue.ToString());
                sb.AppendFormat("{0}|", (byte)action);
                sb.AppendFormat("{0}|{1}|", cmoboSignalLac.Text, cmoboSignalCi.Text);
                sb.AppendFormat("{0}|{1}|", txtSignalMsisdn1.Text, txtSignalMsisdn1.Text);

                SignalItem item = SignalItem.FromTxtLine(sb.ToString());
                AppMain.SignalFileManage.PutSignalItem(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        NetDataBuffer netBuffer = new NetDataBuffer();

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            NetMsisdnSch sch = new NetMsisdnSch(true);
            for (int i = 0; i < 1024; i++)
            {
                sch.AddPreMsisdn(i.ToString());
                sch.AddPostMsisdn(i.ToString());
            }
            byte[] data = sch.ToBytes();

            netBuffer.EventRcvPacket += NetBuffer_EventRcvPacket;
            for (int i = 0; i < data.Length; )
            {
                netBuffer.AddData(data, i, 3);
                i += 3;
            }
        }

        private void NetBuffer_EventRcvPacket(byte[] netData)
        {
            NetMsisdnSch sch;
            En_NetType netType = NetMsisdnSch.GetPacketType2(netData);
            switch (netType)
            {
                case En_NetType.EN_NetMsisdnSch:
                    {
                        sch = NetMsisdnSch.FromBytes(netData, 0);
                    }
                    break;
            }
        }
    }

    public class ListItemValue
    {
        public string Str1 { get; set; }
        public string Str2 { get; set; }
        public string Str3 { get; set; }

        public object tag;

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Str1, Str2, Str3);
        }
    }
}
