using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IocpNetLib;
using System.Threading;
using System.Diagnostics;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// 负责与分处理节点网络事件
    /// </summary>
    class NetServer
    {
        bool _start = false;
        public ushort ListenPort { get; set; } = 8088;
        NetWrapper.MsgCallBack _netCallback;
        ObjectPool<NetCallbackData> _listNetData = new ObjectPool<NetCallbackData>();

        ObjectPool<SignalItem> _listSignalItem = new ObjectPool<SignalItem>();

        Dictionary<long, NetClientInfo> _clientGroup = new Dictionary<long, NetClientInfo>();

        public bool Init()
        {
            int result = 0;
            if (NetWrapper.Net_IsStart() <= 0)
            {
                _netCallback = new NetWrapper.MsgCallBack(NetData_CallBack);
                result = NetWrapper.Net_Start(_netCallback);
                NetWrapper.Net_SetSendBufSize(10240);
            }
            result = NetWrapper.Net_AddListenPort(ListenPort);

            AppLog.Log(string.Format("网络服务端启动。端口:{0}", ListenPort));

            _start = true;
            Thread thread1 = new Thread(new ThreadStart(ThreadProc));
            thread1.Start();
            return result >= 0;
        }

        private void ThreadProc()
        {
            while (_start)
            {
                DealNetMessage();
                DealSignalItem();

                Thread.Sleep(1);
            }
        }

        private int DealSignalItem()
        {
            int count = 0;
            while (true)
            {
                SignalItem item = _listSignalItem.GetObj();
                if (item == null)
                    break;
                count++;

                DealSignalItem(item);
            }
            return count;
        }

        private void DealSignalItem(SignalItem item)
        {
          foreach(NetClientInfo client in _clientGroup.Values)
            {
                client.DealSignalItem(item);
            }
        }

        public void PutSignal(SignalItem item)
        {
            _listSignalItem.PutObj(item);
        }

        private int DealNetMessage()
        {
            int count = 0;
            while (true)
            {
                NetCallbackData info = _listNetData.GetObj();
                if (info == null)
                    break;
                count++;

                if (info.NetEvent == EN_NetEvent.en_data)
                {
                    DealNetData(info);
                }
                else if (info.NetEvent == EN_NetEvent.en_disconnect
                    || info.NetEvent == EN_NetEvent.en_connet)
                {
                    DealNetConnect(info);
                }
            }
            return count;
        }

        private void DealNetData(NetCallbackData info)
        {
            if (!_clientGroup.ContainsKey(info.Socket))
            {
                Debug.Assert(false);
                return;
            }

            NetClientInfo client = _clientGroup[info.Socket];
            client.AddNetData(info);
        }

        private void DealNetConnect(NetCallbackData info)
        {
            if (info.NetEvent == EN_NetEvent.en_connet)
            {
                NetClientInfo client = new NetClientInfo(info,this);

                Debug.Assert(!_clientGroup.ContainsKey(info.Socket));
                _clientGroup.Remove(info.Socket);

                _clientGroup.Add(info.Socket, client);

                AppLog.Log(string.Format("分中心连接；{0}", client.PeerIP));
            }
            else if (info.NetEvent == EN_NetEvent.en_disconnect)
            {
                if (!_clientGroup.ContainsKey(info.Socket))
                    return;

                NetClientInfo client = _clientGroup[info.Socket];
                AppLog.Log(string.Format("分中心断开；{0}", client.PeerIP));

                _clientGroup.Remove(info.Socket);
            }
        }

        public void NetData_CallBack(ref MSGINFO msg)
        {
            NetCallbackData info = NetCallbackData.FromPtrData(ref msg);
            _listNetData.PutObj(info);
        }


        public long SendToPeerCount = 0;
        public long SendToPeerFailCount = 0;
        public EN_SEND_BUFFER_RESULT SendToPeerClient(long socket,byte[] data)
        {
            EN_SEND_BUFFER_RESULT result = NetWrapper.Net_PutSendBuffer(socket, data);
            if (result == EN_SEND_BUFFER_RESULT.en_send_buffer_ok)
            {
                SendToPeerCount++;
            }
            else
            {
                SendToPeerFailCount++;
            }
            return result;
        }
    }
}
