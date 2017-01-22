using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YZNetPacket;

namespace YZDataCenter
{
    //对手机信令分析
    public class MsisdnParseManage
    {
        Dictionary<string, MsidnItemParse> _msidnItemGroup = new Dictionary<string, MsidnItemParse>();

        public event Action<SignalItem> EventSignalItem;

        DateTime _maxSignalTime = DateTime.MinValue;

        public void DealSignal(SignalItem item)
        {
            //没有排序的信令
            if (item.sortStat != EN_SortStat.sort)
            {
                MsisdnParseStat.AddUnsortSignal(item);
                EventSignalItem?.Invoke(item);
                return;
            }

            //信令最大时间为当前时间
            if (item.timeStamp > _maxSignalTime)
                _maxSignalTime = item.timeStamp;

            if (item.msisdnIndex == EN_MsisdnIndex.first)
            {
                MsisdnParseStat.AddNotNeedParseSignalItem(item);

                AddMsisdn(item.msisdn, item);
                EventSignalItem?.Invoke(item);
            }
            else if (item.msisdnIndex == EN_MsisdnIndex.second)
            {
                MsisdnParseStat.AddNotNeedParseSignalItem(item);

                AddMsisdn(item.msisdn2, item);
                EventSignalItem?.Invoke(item);
            }
            else //两个手机号，不确定是哪个上报的消息
            {
                DealUncertainSignal(item);
                EventSignalItem?.Invoke(item);

                MsisdnParseStat.AddNeedParseSignalItem(item);
            }
        }

        public void AddMsisdn(string msisdn, SignalItem item)
        {
            MsidnItemParse misdnItem;
            if (_msidnItemGroup.ContainsKey(msisdn))
            {
                misdnItem = _msidnItemGroup[msisdn];
            }
            else
            {
                misdnItem = new MsidnItemParse(msisdn);
                _msidnItemGroup.Add(msisdn, misdnItem);
            }
            misdnItem.PutSignal(item);
        }

        //处理不确定的信令
        private void DealUncertainSignal(SignalItem item)
        {
            Debug.Assert(item.IsHaveTwoMsisdn());
            Debug.Assert(item.msisdnIndex == EN_MsisdnIndex.unknown);

            //查找历史记录
            MsidnItemParse item1 = null;
            if (_msidnItemGroup.ContainsKey(item.msisdn))
            {
                item1 = _msidnItemGroup[item.msisdn];
            }

            MsidnItemParse item2 = null;
            if (_msidnItemGroup.ContainsKey(item.msisdn2))
            {
                item2 = _msidnItemGroup[item.msisdn2];
            }

            ParseSignalItem(item, item1, item2);

            if (item.msisdnIndex == EN_MsisdnIndex.first)
            {
                AddMsisdn(item.msisdn, item);
            }
            else if (item.msisdnIndex == EN_MsisdnIndex.second)
            {
                AddMsisdn(item.msisdn2, item);
            }
            else
            {
                //没有根据关联判断出来
            }
        }

        private void ParseSignalItem(SignalItem curItem, MsidnItemParse item1, MsidnItemParse item2)
        {
            //最后一次信令
            SignalItem SignalItem1 = null;
            if (item1 != null)
            {
                SignalItem1 = item1.LastSignal;
            }
            SignalItem SignalItem2 = null;
            if (item2 != null)
            {
                SignalItem2 = item2.LastSignal;
            }

            //对切入类型特殊处理。切出、切入成对出现。
            if (curItem.action == EN_Action.切入)
            {
                if (SignalItem1 != null)
                {
                    bool signalCutout1 = SignalParam.IsCutoutValidate(SignalItem1, curItem.timeStamp);
                    if (SignalItem2 != null)
                    {
                        bool signalCutout2 = SignalParam.IsCutoutValidate(SignalItem2, curItem.timeStamp);
                        if (signalCutout1 && !signalCutout2)
                        {
                            curItem.msisdnIndex = EN_MsisdnIndex.first;
                        }
                        else if (!signalCutout1 && signalCutout2)
                        {
                            curItem.msisdnIndex = EN_MsisdnIndex.second;
                        }
                        else
                        {
                            //两个都无效或都有效，无法判断
                            return;
                        }
                    }
                    else
                    {
                        if (signalCutout1)
                        {
                            curItem.msisdnIndex = EN_MsisdnIndex.first;
                        }
                    }
                }
                else
                {
                    if (SignalItem2 != null)
                    {
                        bool signalCutout2 = SignalParam.IsCutoutValidate(SignalItem2, curItem.timeStamp);
                        if (signalCutout2)
                        {
                            curItem.msisdnIndex = EN_MsisdnIndex.second;
                        }
                    }
                    else
                    {

                    }
                }
                return;
            }

            //非切入信令
            //根据与上次的lac、ci匹配判断
            if (SignalItem1 != null)
            {
                bool macthOne = SignalParam.IsSignalValidate(SignalItem1, curItem.timeStamp) && SignalParam.IsSameCgi(SignalItem1, curItem);
                if (SignalItem2 != null)
                {
                    bool macthTwo = SignalParam.IsSignalValidate(SignalItem2, curItem.timeStamp) && SignalParam.IsSameCgi(SignalItem2, curItem);
                    if (macthOne && !macthTwo)
                    {
                        curItem.msisdnIndex = EN_MsisdnIndex.first;
                    }
                    else if (!macthOne && macthTwo)
                    {
                        curItem.msisdnIndex = EN_MsisdnIndex.second;
                    }
                    else
                    {
                        //两个都匹配 或都不匹配
                    }
                }
                else
                {
                    if (macthOne)
                    {
                        curItem.msisdnIndex = EN_MsisdnIndex.first;
                    }
                }
            }
            else
            {
                if (SignalItem2 != null)
                {
                    bool macthTwo = SignalParam.IsSignalValidate(SignalItem2, curItem.timeStamp) && SignalParam.IsSameCgi(SignalItem2, curItem);
                    if (macthTwo)
                    {
                        curItem.msisdnIndex = EN_MsisdnIndex.second;
                    }
                }
                else
                {

                }
            }
        }

        private void DisposeUncertainSignal(SignalItem signal)
        {

        }
    }

    //单个手机号处理
    public class MsidnItemParse
    {
        private string misdn;
        List<SignalItem> _listMsidn = new List<SignalItem>();

        public SignalItem LastSignal
        {
            get
            {
                if (_listMsidn.Count == 0)
                    return null;
                return _listMsidn[_listMsidn.Count - 1];
            }
        }

        public MsidnItemParse(string misdn)
        {
            this.misdn = misdn;
        }

        void ResizeSignal()
        {
            while (true)
            {
                if (_listMsidn.Count <= 3) //只保留最近的
                    break;
                _listMsidn.RemoveAt(0);
            }
        }

        public void PutSignal(SignalItem item)
        {
            //只保留 排序过的
            if (item.sortStat == EN_SortStat.sort)
            {
                _listMsidn.Add(item);
                ResizeSignal();
            }
        }
    }

    public class MsisdnParseStat
    {
        public static long SortSignalThisDay = 0;
        public static long SignalNotNeedParseThisDay = 0;
        public static long SignalNeedParseThisDay = 0;
        public static long SignalParseOkThisDay = 0;

        public static long SortSignalThisHour = 0;
        public static long SignalNotNeedParseThisHour = 0;
        public static long SignalNeedParseThisHour = 0;
        public static long SignalParseOkThisHour = 0;

        public static long TotalSignalUnsortThisDay = 0;
        public static long TotalSignalUnsortThisHour = 0;

        static DateTime LastRecalStat = DateTime.MinValue;
        public static void RecalStat()
        {
            if (LastRecalStat == DateTime.MinValue)
            {
                LastRecalStat = DateTime.Now;
                return;
            }

            // 跨天
            if (DateTime.Now.Day != LastRecalStat.Day)
            {
                ResetStatHour();
                ResetStatDay();
                LastRecalStat = DateTime.Now;
                return;
            }

            if (DateTime.Now.Hour != LastRecalStat.Hour)
            {
                ResetStatHour();
                LastRecalStat = DateTime.Now;
            }
        }

        static void ResetStatDay()
        {
            SortSignalThisDay = 0;
            SignalNotNeedParseThisDay = 0;
            SignalNeedParseThisDay = 0;
            SignalParseOkThisDay = 0;

            TotalSignalUnsortThisDay = 0;
        }

        static void ResetStatHour()
        {
            SortSignalThisHour = 0;
            SignalNotNeedParseThisHour = 0;
            SignalNeedParseThisHour = 0;
            SignalParseOkThisHour = 0;

            TotalSignalUnsortThisHour = 0;
        }

        public static void AddNotNeedParseSignalItem(SignalItem item)
        {
            SortSignalThisDay++;
            SignalNotNeedParseThisDay++;

            SortSignalThisHour++;
            SignalNotNeedParseThisHour++;
        }

        public static void AddNeedParseSignalItem(SignalItem item)
        {
            SortSignalThisDay++;
            SignalNeedParseThisDay++;

            if (item.msisdnIndex != EN_MsisdnIndex.unknown)
            {
                SignalParseOkThisDay++;
                SignalParseOkThisHour++;
            }

            SortSignalThisHour++;
            SignalNeedParseThisHour++;
        }

        public static void AddUnsortSignal(SignalItem item)
        {
            TotalSignalUnsortThisDay++;
            TotalSignalUnsortThisHour++;
        }
    }
}
