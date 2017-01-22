using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IocpNetLib;
using YZNetPacket;
using System.Diagnostics;

namespace YZDataCenter
{
    /// <summary>
    /// 处理每个socket连接
    /// </summary>
    class NetClientInfo
    {
        private NetCallbackData ConnectInfo;
        NetDataBuffer _netDataBuffer = new NetDataBuffer();

        public string PeerIP { get; set; }

        NetServer _netServer;

        public NetClientInfo(NetCallbackData message, NetServer netServer)
        {
            this.ConnectInfo = message;
            _netServer = netServer;

            string peerIp = string.Empty;
            ushort peerPort = 0;
            NetWrapper.Net_GetPeerName(ConnectInfo.Socket, ref peerIp, ref peerPort);
            PeerIP = peerIp;

            _netDataBuffer.EventRcvPacket += _netDataBuffer_EventRcvPacket;
            _netDataBuffer.EventRcvPacketLenError += _netDataBuffer_EventRcvPacketLenError;
        }

        private void _netDataBuffer_EventRcvPacketLenError(int packetLen)
        {

        }

        private void _netDataBuffer_EventRcvPacket(byte[] packetData)
        {
            NetHead netObj = NetHead.FromBytes(packetData, 0);
            if (netObj == null)
            {
                Debug.Assert(false);
                return;
            }

            DealNetPacket(netObj);

        }

        internal void AddNetData(NetCallbackData info)
        {
            _netDataBuffer.AddData(info.NetData, 0, info.NetData.Length);
        }

        private void DealNetPacket(NetHead netPacket)
        {
            if (netPacket.PacketType == En_NetType.EN_NetMsisdnSch)
            {
                DealMsisdnSch((NetMsisdnSch)netPacket);
            }
            else if (netPacket.PacketType == En_NetType.EN_NetCgiSch)
            {
                DealCgiSch((NetCgiSch)netPacket);
            }
        }

        //扇区订阅处理
        Dictionary<CGI, int> _cgiSch = new Dictionary<CGI, int>();
        private void DealCgiSch(NetCgiSch netPacket)
        {
            foreach(CGI cgi in netPacket.ListCgi)
            {
                CgiSch(cgi, netPacket.SchType == 1);
            }
        }

        public void CgiSch(CGI cgi,bool add)
        {
            if(add)
            {
                if (_cgiSch.ContainsKey(cgi))
                    return;
                _cgiSch.Add(cgi, 1);
            }
            else
            {
                _cgiSch.Remove(cgi);
            }
        }

        bool IsCgiNeed(CGI cgi)
        {
            if(_cgiSch.ContainsKey(cgi))
            {
                return true;
            }
            cgi.ci = 0;
            if (_cgiSch.ContainsKey(cgi))
            {
                return true;
            }
            return false;
        }

        private void DealMsisdnSch(NetMsisdnSch netPacket)
        {
            foreach (string msisdn in netPacket.ListMsisdn)
            {
                if (msisdn.StartsWith("*"))
                {
                    DealPostMsisdnSch(msisdn.Replace("*", string.Empty), netPacket.SchType == 1);
                }
                else if (msisdn.EndsWith("*"))
                {
                    DealPreMsisdnSch(msisdn.Replace("*", string.Empty), netPacket.SchType == 1);
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            RecalMsisdnSch();

            //发送到 分处理中心
            _sendPacketPool.PutObj(netPacket);
            SendToPeer();
        }


        Dictionary<string, int> _msisdnPostSch = new Dictionary<string, int>();
        Dictionary<string, int> _msisdnPreSch = new Dictionary<string, int>();

        List<int> _listPostSchLen = new List<int>();
        List<int> _listPreSchLen = new List<int>();
        private void RecalMsisdnSch()
        {
            //统计前缀和后缀 都包含哪些长度的号码
            _listPostSchLen = _msisdnPostSch.Keys.Select(o => o.Length).Distinct().ToList();
            _listPreSchLen = _msisdnPreSch.Keys.Select(o => o.Length).Distinct().ToList();
        }

        private void DealPreMsisdnSch(string msisdn, bool add)
        {
            if (add)
            {
                if (!_msisdnPreSch.ContainsKey(msisdn))
                {
                    _msisdnPreSch.Add(msisdn, 0);
                }
            }
            else
            {
                _msisdnPreSch.Remove(msisdn);
            }
        }

        private void DealPostMsisdnSch(string msisdn, bool add)
        {
            if (add)
            {
                if (!_msisdnPostSch.ContainsKey(msisdn))
                {
                    _msisdnPostSch.Add(msisdn, 0);
                }
            }
            else
            {
                _msisdnPostSch.Remove(msisdn);
            }
        }

        //手机号是否订阅了
        bool IsMsisdnSch(string msisdn)
        {
            if (string.IsNullOrEmpty(msisdn))
                return false;

            //后缀比较
            foreach (int len in _listPostSchLen)
            {
                if (len > msisdn.Length)
                    continue;
                string str = msisdn.Substring(msisdn.Length - len, len);

                if (_msisdnPostSch.ContainsKey(str))
                    return true;
            }

            //前缀比较
            foreach (int len in _listPreSchLen)
            {
                if (len > msisdn.Length)
                    continue;
                string str = msisdn.Substring(0, len);

                if (_msisdnPreSch.ContainsKey(str))
                    return true;
            }

            return false;
        }

        //是否订阅此信令
        bool IsSignalSch(SignalItem item)
        {
            if (IsMsisdnSch(item.msisdn)
                || IsMsisdnSch(item.msisdn2))
            {
                return true;
            }

            if(IsCgiNeed(item.CGI))
            {
                return true;
            }

            return false;
        }

        internal void DealSignalItem(SignalItem item)
        {
            if (!IsSignalSch(item))
                return;

            NetSignalData net = new NetSignalData();
            net.AddSignal(item);

            _sendPacketPool.PutObj(net);
            SendToPeer();
        }

        ObjectPool<NetHead> _sendPacketPool = new ObjectPool<NetHead>();
        NetHead _lastSendPacket = null;

        private void SendToPeer()
        {
            if (_lastSendPacket != null)
            {
                EN_SEND_BUFFER_RESULT result = _netServer.SendToPeerClient(ConnectInfo.Socket, _lastSendPacket.ToBytes());
                if (result == EN_SEND_BUFFER_RESULT.en_send_buffer_full)
                {
                    return;
                }
                _lastSendPacket = null;
            }

            //缓冲中数据发走
            while (true)
            {
                _lastSendPacket = _sendPacketPool.GetObj();
                if (_lastSendPacket == null)
                {
                    break;
                }

                EN_SEND_BUFFER_RESULT result = _netServer.SendToPeerClient(ConnectInfo.Socket, _lastSendPacket.ToBytes());
                if (result == EN_SEND_BUFFER_RESULT.en_send_buffer_full)
                {
                    break;
                }
                _lastSendPacket = null;
            }
            //Debug.Assert(result == EN_SEND_BUFFER_RESULT.en_send_buffer_ok);
        }
    }
}
