using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IocpNetLib
{
    public class NetWrapper
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate void MsgCallBack(ref MSGINFO msg);

        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_IsStart@@YAHXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Net_IsStart();

        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_Start@@YAHP6AXPEAUMSGINFO@@@Z@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Net_Start(MsgCallBack msgCallback);

        //关闭网络层
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_Close@@YAXXZ", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Net_Close();

        //增加网络监听端口；可多次调用
        [DllImport(@"IocpNetLib.dll", EntryPoint = "?Net_AddListenPort@@YAHG@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Net_AddListenPort(UInt16 listenPort);

        //关闭网络监听端口
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_CloseListenPort@@YAHG@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Net_CloseListenPort(UInt16 listenPort);

        //同步连接对方。
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_ConnectPeer@@YA_KPADGH@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern long Net_ConnectPeer_Dll(IntPtr pIp, UInt16 port, int tag);

        //异步连接对方。
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_ConnectPeerAsyn@@YA_KPADGH@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Net_ConnectPeerAsyn_Dll(IntPtr pIp, UInt16 port, int tag);

        //设置每个链接的发送缓冲大小（单位K字节）；默认每个发送缓冲为1M byte；
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_SetSendBufSize@@YAXH@Z", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Net_SetSendBufSize(int kByte);

        //将数据放到发送缓冲
        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_PutSendBuffer@@YAH_KPEADH@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int Net_PutSendBuffer_Dll(IntPtr socket, IntPtr buffer, int bufLen);

        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_GetPeerName@@YAH_KPEADPEAG@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int Net_GetPeerName_Dll(IntPtr socket, IntPtr ip, IntPtr port);

        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_GetLocalName@@YAHIPADPAG@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int Net_GetLocalName_Dll(IntPtr socket, IntPtr ip, IntPtr port);

        [DllImport("IocpNetLib.dll", EntryPoint = "?Net_CloseSocket@@YAHI@Z", CallingConvention = CallingConvention.Cdecl)]
        static extern int Net_CloseSocket_Dll(IntPtr socket);

        public static int Net_GetPeerName(long socket, ref string ip, ref ushort port)
        {
            byte[] ip2 = new byte[50];
            byte[] port2 = new byte[2];

            GCHandle hin = GCHandle.Alloc(ip2, GCHandleType.Pinned);
            GCHandle hin2 = GCHandle.Alloc(port2, GCHandleType.Pinned);

            IntPtr p = new IntPtr(socket);
            Net_GetPeerName_Dll(p, hin.AddrOfPinnedObject(), hin2.AddrOfPinnedObject());
            hin.Free();
            hin2.Free();

            ArrayAsciiTrim(ref ip2);
            ip = Encoding.ASCII.GetString(ip2).Trim('\0');
            port = BitConverter.ToUInt16(port2, 0);
            return 0;
        }

        static void ArrayAsciiTrim(ref byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    Array.Resize(ref data, i);
                    break;
                }
            }
        }

        public static int Net_GetLocalName(long socket, ref string ip, ref ushort port)
        {
            byte[] ip2 = new byte[50];
            byte[] port2 = new byte[2];

            GCHandle hin = GCHandle.Alloc(ip2, GCHandleType.Pinned);
            GCHandle hin2 = GCHandle.Alloc(port2, GCHandleType.Pinned);

            IntPtr p = new IntPtr(socket);
            Net_GetLocalName_Dll(p, hin.AddrOfPinnedObject(), hin2.AddrOfPinnedObject());
            hin.Free();
            hin2.Free();

            ArrayAsciiTrim(ref ip2);
            ip = Encoding.ASCII.GetString(ip2).Trim('\0');
            port = BitConverter.ToUInt16(port2, 0);
            return 0;
        }

        public static ushort Net_GetLocalPort(long socket)
        {
            string ip = string.Empty;
            ushort port = 0;
            Net_GetLocalName(socket, ref ip, ref port);
            return port;
        }

        public static int Net_CloseSocket(long socket)
        {
            IntPtr p = new IntPtr(socket);
            int n = Net_CloseSocket_Dll(p);
            return n;
        }

        public static bool Net_ConnectPeer(string ip, ushort port, ref long socket, int tag)
        {
            IntPtr p = new IntPtr(socket);
            byte[] data = Encoding.ASCII.GetBytes(ip);
            byte[] data2 = new byte[data.Length + 2];
            Array.Copy(data, data2, data.Length);

            GCHandle hin = GCHandle.Alloc(data2, GCHandleType.Pinned);
            socket = Net_ConnectPeer_Dll(hin.AddrOfPinnedObject(), port, tag);
            hin.Free();

            return socket != 0;
        }

        public static bool Net_ConnectPeerAsyn(string ip, ushort port, ref long socket, int tag)
        {
            IntPtr p = new IntPtr(socket);
            byte[] data = Encoding.ASCII.GetBytes(ip);
            byte[] data2 = new byte[data.Length + 2];
            Array.Copy(data, data2, data.Length);

            GCHandle hin = GCHandle.Alloc(data2, GCHandleType.Pinned);
            socket = Net_ConnectPeerAsyn_Dll(hin.AddrOfPinnedObject(), port, tag);
            hin.Free();

            return socket != 0;
        }

        //将数据放到发送缓冲
        public static EN_SEND_BUFFER_RESULT Net_PutSendBuffer(long socket, byte[] data)
        {
            GCHandle hin = GCHandle.Alloc(data, GCHandleType.Pinned);
            EN_SEND_BUFFER_RESULT n = (EN_SEND_BUFFER_RESULT)Net_PutSendBuffer_Dll(new IntPtr(socket), hin.AddrOfPinnedObject(), data.Length);
            hin.Free();
            return n;
        }

        public static EN_SEND_BUFFER_RESULT Net_PutSendBuffer(long socket, IntPtr buffer, int bufLen)
        {
            EN_SEND_BUFFER_RESULT n = (EN_SEND_BUFFER_RESULT)Net_PutSendBuffer_Dll(new IntPtr(socket), buffer, bufLen);
            return n;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSGINFO
    {
        public int status;
        public IntPtr socket;
        public int tag;

        public int bufLen;     //当为STATUS_CONNECT时，此值为监听端口
        public IntPtr buffer;
    };

    public enum EN_SEND_BUFFER_RESULT
    {
        en_send_buffer_ok = 0,
        en_not_validate_socket = 1,
        en_send_buffer_full = 2
    };

    public class NetCallbackData
    {
        public EN_NetEvent NetEvent;
        public long Socket;
        public ushort ListenPort;
        public int Tag;
        public byte[] NetData;
        public static NetCallbackData FromPtrData(ref MSGINFO msg)
        {
            NetCallbackData data = new NetCallbackData();
            data.NetEvent = (EN_NetEvent)msg.status;
            data.Socket = (long)msg.socket;
            data.Tag = msg.tag;
            if(data.NetEvent == EN_NetEvent.en_data 
                && msg.bufLen > 0)
            {
                data.NetData = new byte[msg.bufLen];
                Marshal.Copy(msg.buffer, data.NetData, 0, msg.bufLen);
            }
            return data;
        }
    }

    public enum EN_NetEvent
    {
        en_data = 0,        //正常 有数据包
        en_connet = 1,      //客户端连接
        en_disconnect = 2,      //客户端断开
        en_connect_ok_asyn = 3,     //异步链接成功
        en_connect_fail_asyn = 4		//异步链接失败
    }
}
