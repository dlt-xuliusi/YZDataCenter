using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using YZNetPacket;

namespace YZDataCenter
{
    public class SortSignalManage
    {
        //对数据缓冲多久才排序
        public TimeSpan PoolTimeSpan { get; set; } = TimeSpan.FromSeconds(0);

        Dictionary<DateTime, SignalItemList> _sortSignalGroup = new Dictionary<DateTime, SignalItemList>();

        DateTime _signalMaxTime = DateTime.MinValue;
        DateTime _signalMinTime = DateTime.MinValue;

        DateTime _signalSortStartTime = DateTime.MinValue;

        long _putSignalCount = 0;
        public long PutSignalCount
        {
            get
            {
                return _putSignalCount;
            }
        }

        long _poolSignalCount = 0;
        public int PoolCount
        {
            get
            {
                lock (this)
                {
                    return (int)_poolSignalCount;// _sortSignalGroup.Count;
                }
            }
        }

        public DateTime SignalStartTime
        {
            get
            {
                if (_signalSortStartTime != DateTime.MinValue)
                    return _signalSortStartTime;
                return _signalMinTime;
            }
        }

        public DateTime SignalEndTime
        {
            get
            {
                return _signalMaxTime;
            }
        }

        public bool PutSignalItem(SignalItem item)
        {
            //信令的时间 比已取走的信令时间还早。 该信令延迟太多！
            //是否还要处理？如果处理，会导致信令的排序不准确
            if (_signalSortStartTime != DateTime.MinValue
               && item.timeStamp < _signalSortStartTime)
            {
                // Debug.Assert(false);
                return false;
            }

            //仅仅精确到秒
            DateTime dtSecond = SignalDefine.RemoveMillisecond(item.timeStamp);

            //设置信令的最大时间
            if (dtSecond > _signalMaxTime)
            {
                _signalMaxTime = dtSecond;
            }
            //设置信令的最小时间
            if (_signalMinTime > dtSecond
                || _signalMinTime == DateTime.MinValue)
            {
                _signalMinTime = dtSecond;
            }
            item.sortStat = EN_SortStat.sort;

            lock (this)
            {
                _putSignalCount++;
                _poolSignalCount++;

                if (!_sortSignalGroup.ContainsKey(dtSecond))
                {
                    _sortSignalGroup.Add(dtSecond, new SignalItemList(item));
                }
                else
                {
                    _sortSignalGroup[dtSecond].AddItem(item);
                }
                return true;
            }
        }

        public List<SignalItem> GetSignalItem()
        {
            lock (this)
            {
                if (_sortSignalGroup.Count == 0)
                    return null;

                if (_signalSortStartTime == DateTime.MinValue)
                {
                    //信令时间差超过设定值，才开始处理
                    TimeSpan span = (_signalMaxTime - _signalMinTime);
                    if (span < PoolTimeSpan)
                        return null;
                    _signalSortStartTime = _signalMinTime;
                }


                SignalItemList signalList = new SignalItemList();
                //从最小时间开始查找
                while (true)
                {
                    TimeSpan span = (_signalMaxTime - _signalSortStartTime);
                    if (span < PoolTimeSpan)
                        break;

                    if (!_sortSignalGroup.ContainsKey(_signalSortStartTime))
                    {
                        _signalSortStartTime = _signalSortStartTime.AddSeconds(1);//增大一秒
                        continue;
                    }

                    signalList.AddItemRange(_sortSignalGroup[_signalSortStartTime].Items);

                    _sortSignalGroup.Remove(_signalSortStartTime);
                    _signalSortStartTime = _signalSortStartTime.AddSeconds(1);//增大一秒
                    break;
                }

                if (signalList.Items.Count == 0)
                    return null;

                List<SignalItem> result = signalList.Items.OrderBy(o => o.timeStamp).ToList();
                _poolSignalCount -= result.Count;
                return result;
            }
        }

        public class SignalItemList
        {
            List<SignalItem> _listItems = new List<SignalItem>();

            public List<SignalItem> Items
            {
                get
                {
                    return _listItems;
                }
            }


            DateTime _timeStamp = DateTime.MinValue;
            public DateTime TimeStamp
            {
                get
                {
                    return _timeStamp;
                }
            }

            public SignalItemList()
            {

            }

            public SignalItemList(SignalItem item)
            {
                _timeStamp = SignalDefine.RemoveMillisecond(item.timeStamp);
                _listItems.Add(item);
            }

            public void AddItem(SignalItem item)
            {
                _listItems.Add(item);
            }

            public void AddItemRange(List<SignalItem> items)
            {
                _listItems.AddRange(items);
            }
        }
    }

    //public class SortSignalManage
    //{
    //    //对数据缓冲多久才排序
    //    public TimeSpan PoolTimeSpan = TimeSpan.FromSeconds(0);

    //    SortedDictionary<DateTime, SignalItemList> _sortSignalGroup = new SortedDictionary<DateTime, SignalItemList>();

    //    DateTime _signalMaxTime = DateTime.MinValue;

    //    DateTime _signalMinSortTime = DateTime.MinValue;

    //    public long PutSignalCount = 0;

    //    public int PoolCount
    //    {
    //        get
    //        {
    //            lock (this)
    //            {
    //                return _sortSignalGroup.Count;
    //            }
    //        }
    //    }


    //    public void PutSignalItem(SignalItem item)
    //    {
    //        //信令的时间 比已取走的信令时间还早。 该信令延迟太多！
    //        //是否还要处理？如果处理，会导致信令的排序不准确
    //        if (item.timeStamp < _signalMinSortTime)
    //        {
    //            // Debug.Assert(false);
    //            return;
    //        }

    //        PutSignalCount++;
    //        //设置信令的最大时间
    //        if (item.timeStamp > _signalMaxTime)
    //        {
    //            _signalMaxTime = item.timeStamp;
    //        }

    //        lock (this)
    //        {
    //            if (!_sortSignalGroup.ContainsKey(item.timeStamp))
    //            {
    //                _sortSignalGroup.Add(item.timeStamp, new SignalItemList(item));
    //            }
    //            else
    //            {
    //                _sortSignalGroup[item.timeStamp].AddItem(item);
    //            }
    //        }
    //    }

    //    public SignalItem GetSignalItem()
    //    {
    //        lock (this)
    //        {
    //            if (_sortSignalGroup.Count == 0)
    //                return null;

    //            SignalItemList signalList = null;
    //            //缓冲时间需要超过设定时间
    //            foreach (SignalItemList signalList2 in _sortSignalGroup.Values)
    //            {
    //                signalList = signalList2;
    //                break;
    //            }
    //            if (signalList.Items.Count == 0)
    //            {
    //                _sortSignalGroup.Remove(signalList.TimeStamp);
    //                Debug.Assert(false);
    //                return null;
    //            }

    //            TimeSpan span = (_signalMaxTime - signalList.TimeStamp);
    //            if (span < PoolTimeSpan)
    //                return null;

    //            _signalMinSortTime = signalList.TimeStamp;

    //            SignalItem result = null;

    //            if (signalList.Items.Count > 0)
    //            {
    //                result = signalList.Items.First();
    //                signalList.Items.RemoveAt(0);
    //            }

    //            if (signalList.Items.Count == 0)
    //            {
    //                _sortSignalGroup.Remove(signalList.TimeStamp);
    //            }
    //            return result;
    //        }
    //    }


    //    public class SignalItemList
    //    {
    //        List<SignalItem> _listItems = new List<SignalItem>();

    //        public List<SignalItem> Items
    //        {
    //            get
    //            {
    //                return _listItems;
    //            }
    //        }


    //        DateTime _timeStamp = DateTime.MinValue;
    //        public DateTime TimeStamp
    //        {
    //            get
    //            {
    //                return _timeStamp;
    //            }
    //        }


    //        public SignalItemList(SignalItem item)
    //        {
    //            _timeStamp = item.timeStamp;
    //            _listItems.Add(item);
    //        }

    //        public void AddItem(SignalItem item)
    //        {
    //            _listItems.Add(item);
    //        }
    //    }
    //}
}
