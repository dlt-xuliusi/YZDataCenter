using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using YZNetPacket;

namespace YZDataCenter
{
    /// <summary>
    /// 对压缩文件解压
    /// </summary>
    class SignalZipFileManage
    {
        bool _threadStart;

        //正在处理的文件
        public static string  FileDoingFlag = ".copying";

        public void Init()
        {
            StartThread();
            AppLog.Log("信令文件解压模块启动");
        }

        void StartThread()
        {
            if (!_threadStart)
            {
                _threadStart = true;
                Thread thread1 = new Thread(new ThreadStart(ThreadProc));
                thread1.Start();
            }
        }

        private void ThreadProc()
        {
            while (_threadStart)
            {
                DealAllZipFile();
                Thread.Sleep(1);
            }
        }

        private void DealAllZipFile()
        {
            DirectoryInfo directory = new DirectoryInfo(AppParam.SignalZipFileDir);
            FileInfo[] fileInfoArray = directory.GetFiles("*.gz").OrderBy(o => AppHelper.GetTimeFromSignalFile(o.Name)).ToArray();

            foreach (FileInfo fileIno in fileInfoArray)
            {
                DealZipFile(fileIno.FullName);
            }
        }

        private void DealZipFile(string filePath)
        {
            try
            {
                //先解压到当前目录下
                string decomPath = filePath.Substring(0, filePath.Length - 3);
                decomPath += FileDoingFlag;
                ungzip(filePath, decomPath);

                string toPath = Path.Combine(AppParam.SignalReadDir, Path.GetFileName(decomPath));
                File.Move(decomPath, toPath);

                string realUnzipFile = toPath.Remove(toPath.Length - FileDoingFlag.Length, FileDoingFlag.Length);
                File.Delete(realUnzipFile);
                File.Move(toPath, realUnzipFile);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                AppLog.Log(string.Format("压缩文件{0} 解压完成!", Path.GetFileName(filePath)));
                string dealedFilePath = Path.Combine(AppParam.SignalZipFileDealedDir, Path.GetFileName(filePath));
                File.Delete(dealedFilePath);
                File.Move(filePath, dealedFilePath);
            }
        }

        public void ungzip(string path, string decomPath)
        {
            File.Delete(decomPath);
            using (GZipStream stream = new GZipStream(
                new FileStream(path, FileMode.Open, FileAccess.ReadWrite), CompressionMode.Decompress))
            {
                using (FileStream decompressedFile = new FileStream(decomPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    //data represents a byte from the compressed file
                    //it's set through each iteration of the while loop
                    int data;
                    byte[] buffer = new byte[1024];
                    while (true) //iterates over the data of the compressed file and writes the decompressed data
                    {
                        data = stream.Read(buffer, 0, buffer.Length);
                        decompressedFile.Write(buffer, 0, data);
                        if (data < buffer.Length)
                            break;
                    }
                    //close our file streams 
                    decompressedFile.Close();
                    stream.Close();
                }
            }
        }
    }
}
