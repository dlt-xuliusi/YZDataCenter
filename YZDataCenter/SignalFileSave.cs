using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YZNetPacket;

namespace YZDataCenter
{
    class SignalFileSave
    {
        public DateTime lastCreatFileTime = DateTime.MinValue;
        string _fileName = string.Empty;
        string _filePath = string.Empty;
        FileStream _fileStream = null;

        public bool CreateSaveFile()
        {
            DateTime date = DateTime.Now;
            if ((date - lastCreatFileTime).TotalSeconds < 1)
                return false;
            lastCreatFileTime = date;

            string fileName = GetFileName(date);
            if (_fileName == fileName)
            {
                //每分钟生成一个新文件
                return false;
            }

            _fileName = fileName;
            try
            {
                if (_fileStream != null)
                {
                    bool delete = (_fileStream.Length == 0);
                    _fileStream.Close();
                    if (delete)
                    {
                        File.Delete(_filePath);
                    }
                }
                _filePath = Path.Combine(AppParam.SignalSortSaveDir, _fileName);
                _fileStream = new FileStream(_filePath, FileMode.Append);
                AppLog.Log("生成文件:" + _filePath);
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Log("生成文件失败:" + ex.Message);
                return false;
            }
        }
        public void SaveSignal(SignalItem item)
        {
            if (_fileStream == null)
                return;

            byte[] byteFile = Encoding.ASCII.GetBytes(item.ToStringDetail() + "\r\n");
            _fileStream.Write(byteFile, 0, byteFile.Length);
            _fileStream.Flush();
        }

        string GetFileName(DateTime date)
        {
            return string.Format("{0}{1}{2}{3}{4}.txt",
                date.Year,
                date.Month.ToString().PadLeft(2, '0'),
                date.Day.ToString().PadLeft(2, '0'),
                date.Hour.ToString().PadLeft(2, '0'),
                date.Minute.ToString().PadLeft(2, '0'));
        }
    }
}
