using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using YZNetPacket;

namespace YZDataCenter
{
    //负责处理 扇区与区号对照
    class AreaCodeGroup
    {
        Dictionary<CGI, AreaCodeInfo> _areaCodeGroup = new Dictionary<CGI, AreaCodeInfo>();
        public void ReloadFile(string fileName)
        {
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

                        UInt32 areaCode;
                        List<CGI> listCgi;
                        ParseLine(line, out areaCode, out listCgi);

                        if (areaCode != 0 && listCgi.Count > 0)
                        {
                            AddAreaCode(areaCode, listCgi);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLog.Log(string.Format("读取区号扇区对照文件出错！{0}",ex.Message));
            }
            finally
            {
            }
        }

        private void AddAreaCode(uint areaCode, List<CGI> listCgi)
        {
            foreach(CGI cgi in listCgi)
            {
                AddCgi(areaCode,cgi);
            }
        }

        private void AddCgi(uint areaCode, CGI cgi)
        {
            lock (this)
            {
                if (!_areaCodeGroup.ContainsKey(cgi))
                {
                    AreaCodeInfo info = new AreaCodeInfo() { AreaCode = areaCode };
                    _areaCodeGroup.Add(cgi, info);
                }
                else
                {
                    AreaCodeInfo info = _areaCodeGroup[cgi];
                    info.AreaCode = areaCode;
                }
            }
        }

        private bool ParseLine(string line, out uint areaCode, out List<CGI> listCgi)
        {
            areaCode = 0;
            listCgi = new List<CGI>();
            //531; 123,456; 854; 345;
            List<string> items = line.Split(';').ToList();
            if (items.Count <= 1)
                return false;

            int i = 0;
            foreach(string item in items)
            {
                i++;
                if (i == 1)
                {
                    areaCode = AppHelper.UintParse(item.Trim());
                    if (areaCode == 0)
                        return false;
                }
                else
                {
                    List<string> cgiItems = item.Split(',').ToList();
                    if (cgiItems.Count < 2)
                        continue;

                    CGI cgi;
                    cgi.lac = AppHelper.UshotParse(cgiItems[0].Trim());
                    //ci为0 代表整个lac
                    cgi.ci = AppHelper.UshotParse(cgiItems[1].Trim());
                    if(cgi.lac != 0)
                    {
                        listCgi.Add(cgi);
                    }
                }
            }

            return areaCode != 0 && listCgi.Count>0;
        }

        public UInt32 GetAreaCode(CGI cgi)
        {
            lock (this)
            {
                if (!_areaCodeGroup.ContainsKey(cgi))
                    return 0;
                return _areaCodeGroup[cgi].AreaCode;
            }
        }

        public class AreaCodeInfo
        {
            public UInt32 AreaCode { get; set; }
        }
    }
}
