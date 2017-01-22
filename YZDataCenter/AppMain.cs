using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YZDataCenter
{
    class AppMain
    {
        static SignalFileManage _signalFileManage = new SignalFileManage();
        static SignalDataClean _signalDataClean = new SignalDataClean();
        static NetServer _netServer = new NetServer();

        static SignalZipFileManage _signalZipFileManage = new SignalZipFileManage();

        public static AreaCodeGroup AreaCodeMap = new AreaCodeGroup();

        public static NetServer NetServer
        {
            get
            {
                return _netServer;
            }
        }

        public static SignalFileManage SignalFileManage
        {
            get
            {
                return _signalFileManage;
            }
        }

        public static void Init()
        {
            AppParam.Init();

            _signalZipFileManage.Init();

            _netServer.ListenPort = AppParam.NetListenPort;
            _netServer.Init();

            _signalFileManage.EventSignalItem += _signalDataClean.PutSignal;
            _signalDataClean.EventSignalItem += _netServer.PutSignal;

            _signalFileManage.Init();
            _signalDataClean.Init();

            _netServer.ListenPort = AppParam.NetListenPort;
        }
    }
}
