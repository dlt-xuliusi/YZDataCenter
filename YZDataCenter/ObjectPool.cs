using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YZDataCenter
{
    /// <summary>
    /// 对处理的数据进行缓冲.线性安全
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class ObjectPool<T>
    {
        private Queue<T> _listObj = new Queue<T>();

        public ObjectPool()
        {

        }

        public ObjectPool(int maxPoolCount)
        {
            MaxPoolCount = maxPoolCount;
        }

        public int CurPoolCount
        {
            get
            {
                lock (_listObj)
                {
                    return _listObj.Count;
                }
            }
        }

        public int MaxPoolCount { set; get; } = 500000000;

        public void Clear()
        {
            lock (_listObj)
            {
                _listObj.Clear();
            }
        }

        public bool PutObj(T obj)
        {
            lock (_listObj)
            {
                if (_listObj.Count >= MaxPoolCount)
                    return false;
                _listObj.Enqueue(obj);
                return true;
            }
        }

        public bool PutObjMust(T obj)
        {
            lock (_listObj)
            {
                _listObj.Enqueue(obj);
                return true;
            }
        }

        public T GetObj()
        {
            lock (_listObj)
            {
                if (_listObj.Count == 0)
                    return default(T);
                T data = _listObj.Dequeue();
                return data;
            }
        }

        public List<T> GetObjList(int maxCount)
        {
            List<T> result = new List<T>();
            while(true)
            {
                T n = GetObj();
                if(n == null)
                {
                    break;
                }
                result.Add(n);
                if (result.Count >= maxCount)
                    break;
            }
            return result;
        }
    }

    class ObjectPoolEx<T, P>
    {
        public class PairItem
        {
            public T t { set; get; }
            public P p { set; get; }
        };

        private ObjectPool<PairItem> _listObj = new ObjectPool<PairItem>();

        public ObjectPoolEx()
        {
            MaxPoolCount = 500000;
        }

        public ObjectPoolEx(int maxPoolCount)
        {
            MaxPoolCount = maxPoolCount;
        }

        public int CurPoolCount
        {
            get
            {
                return _listObj.CurPoolCount;
            }
        }

        public int MaxPoolCount
        {
            set
            { _listObj.MaxPoolCount = value; }

            get { return _listObj.MaxPoolCount; }
        }

        public void Clear()
        {
            _listObj.Clear();
        }

        public bool PutObj(T obj, P obj2)
        {
            return _listObj.PutObj(new PairItem() { t = obj, p = obj2 });
        }

        public bool PutObjMust(T obj, P obj2)
        {
            return _listObj.PutObjMust(new PairItem() { t = obj, p = obj2 });
        }

        public bool PutObj(T obj)
        {
            return PutObj(obj,default(P));
        }

        public bool PutObj(P obj)
        {
            return PutObj(default(T),obj);
        }

        public bool PutObjMust(T obj)
        {
            return PutObjMust(obj, default(P));
        }

        public bool PutObjMust(P obj)
        {
            return PutObjMust(default(T), obj);
        }

        public PairItem GetObj()
        {
            return _listObj.GetObj();
        }

        public bool GetObj(ref T t,ref P p)
        {
            PairItem item  = _listObj.GetObj();
            if (item == null)
                return false;
            t = item.t;
            p = item.p;
            return true;
        }
    }
}
