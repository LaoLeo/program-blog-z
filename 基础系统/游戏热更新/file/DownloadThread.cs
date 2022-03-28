using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;
using System.Net;
using System.IO;
using Ionic.Zip;
//using System.Net.Security;
//using System.Security.Cryptography.X509Certificates;
//using Ionic.Crc;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Framework.Download
{
    internal class DownloadThread
    {
        private BreakpointTransferMgr _transferMgr;

        private readonly int kTimeOut = 10000;

        private bool _isStop;
        private Thread _thread;
        private Queue<DownloadTask> _pendingTasks;
        private Queue<DownloadTask> _finishedTasks;
        private volatile bool _isWaitting;

        // 当前正在下载的任务
        private DownloadTask _currentTask;
        // 当前任务文件名称
        private string _currentTaskFileName;
        public string CurrentTaskFileName { get { return _currentTaskFileName; } }

        // 当前任务已经接收的字节大小
        private long _currentTaskReceivedBytes;
        public long CurrentReceivedBytes { get { return _currentTaskReceivedBytes; } }

        // 当前任务总字节大小
        private long _currentTaskTotalBytes;
        public long CurrentTaskTotalBytes { get { return _currentTaskTotalBytes; } }
        // 下载完毕，字节数一致后，是否需要校验md5。zip有些情况下md5重复变化但是文件没变
        private bool isCheckMd5 = true;

        public DownloadThread(BreakpointTransferMgr transferMgr, Queue<DownloadTask> consumeQueue, Queue<DownloadTask> produceQueue, bool isCheckMd5 = true)
        {
            _transferMgr = transferMgr;
            _currentTaskFileName = "";
            _currentTaskReceivedBytes = 0;
            _currentTaskTotalBytes = 1;
            _pendingTasks = consumeQueue;
            _finishedTasks = produceQueue;
            _isWaitting = false;
            _isStop = true;
            this.isCheckMd5 = isCheckMd5;
			ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
		}

		protected bool CertificateValidationCallBack(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			bool isOk = true;
			// If there are errors in the certificate chain,
			// look at each error to determine the cause.
			if (sslPolicyErrors != SslPolicyErrors.None) {
				for (int i = 0; i < chain.ChainStatus.Length; i++) {
					if (chain.ChainStatus [i].Status == X509ChainStatusFlags.RevocationStatusUnknown) {
						continue;
					}
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build ((X509Certificate2)certificate);
					if (!chainIsValid) {
						isOk = false;
						break;
					}
				}
			}
			return isOk;
		}

        //private bool CertificateValidationCallBack(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        //{
        //    return true;
        //}

        public bool isWaitting
        {
            get
            {
                return _isWaitting;
            }
        }

        public void Start()
        {
            if (!_isStop || _thread != null)
            {
                CLogger.Log("DownloadThread::Start() - Download Thread Already Started:" + !_isStop + "#thread:" + ((_thread == null) ? "null" : _thread.ManagedThreadId.ToString()));
                return;
            }
            _isStop = false;
            _thread = new Thread(new ThreadStart(RunDownloading));
            CLogger.Log("DownloadThread::Start() - Create Download Thread:" + _thread.ManagedThreadId);
            _thread.IsBackground = true; // 设置为后台线程，确保当主线程退出时该线程也会结束
            _thread.Start();
        }

        public void Stop()
        {
            CLogger.Log("DownloadThread::Stop() - Stop Download Thread, Status:" + _isStop + "#thread:" + ((_thread == null) ? "null" : _thread.ManagedThreadId.ToString()));
            _isStop = true;
            if (_thread == null)
            {
                return;
            }

            lock (_pendingTasks)
            {
                if (this._isWaitting)
                {
                    this.Notify();
                }
            }

            _thread.Abort();
            _thread = null;
        }

        public void Wait()
        {
            Monitor.Wait(_pendingTasks);
        }

        public void Notify()
        {
            Monitor.Pulse(_pendingTasks);
        }

        private void RunDownloading()
        {
            for (; ; )
            {
                if (_isStop)
                {
                    CLogger.Log("DownloadThread::RunDownloading() - Download Thread Meet Stop Flag,ThreadId:" + Thread.CurrentThread.ManagedThreadId);
                    break;
                }
                if (_currentTask == null)
                {
                    lock (_pendingTasks)
                    {
                        int num = _pendingTasks.Count;
                        if (num > 0)
                        {
                            _currentTask = _pendingTasks.Dequeue();
                        }
                        else
                        {
                            _isWaitting = true;
                            this.Wait();
                            _isWaitting = false;
                        }
                    }
                }

                if (!_isStop && _currentTask != null)
                {
                    //Debug.Log ("StartDownload");
                    DownloadFromBreakPoint(_currentTask);
                    //Debug.Log ("EndDownload");
                }
            }
        }
        /// <summary>
        /// 断点续传下载
        /// </summary>
        /// <param name="task"></param>
        private void DownloadFromBreakPoint(DownloadTask task)
        {
            try
            {
                Uri uri = new Uri(task.url);
                _currentTaskFileName = task.file;
                //				Debug.Log("Download1");
                //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                ////				Debug.Log("Download2");
                //request.Timeout = TIME_OUT;
                //request.ReadWriteTimeout = TIME_OUT;
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                ////				Debug.Log("Download3");
                //long totalLength = response.ContentLength;
                //response.Close();
                //request.Abort();
                //Debug.LogWarning(totalLength);
                DownloadFileTransferInfo dfi = _transferMgr.GetDownloadFileInfo(_currentTaskFileName);
                long totalLength = dfi.size;
                long receivedLength = 0L;
                long toDownloadLength = totalLength;

                if (File.Exists(task.storagePath))
                {
                    //FileInfo fileinfo = new FileInfo(tempFileName);
                    //if (fileinfo.Exists)
                    //{
                    //    receivedLength = fileinfo.Length;
                    //    toDownloadLength = totalLength - receivedLength;
                    //}
                    using (FileStream fileStream = new FileStream(task.storagePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        receivedLength = fileStream.Length;
                        toDownloadLength = totalLength - receivedLength;
                        fileStream.Close();
                    }

                    if (receivedLength != dfi.receivedSize)
                    {
                        CLogger.Log(string.Format("DownloadThread::DownloadFromBreakPoint() - break point save receive size is wrong for file[{0}], saveSize={1}, fileSize={2}", _currentTaskFileName, dfi.receivedSize, receivedLength));
                    }
                }
                task.fileLength = totalLength;
                task.receivedLength = receivedLength;
                _currentTaskTotalBytes = totalLength;
                _currentTaskReceivedBytes = receivedLength;

                bool transferOkay = true;
                if (toDownloadLength > 0L)
                {
                    CLogger.Log("DownloadThread::DownloadFromBreakPoint() - start http download, The request url is [" + uri + "] with range [" + receivedLength + "," + totalLength + "]");

                    HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(uri);
                    request2.Timeout = kTimeOut;
                    request2.KeepAlive = true;
                    request2.ReadWriteTimeout = kTimeOut;
                    request2.AddRange((int)receivedLength, (int)totalLength);

                    HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
                    transferOkay = this.ReadBytesFromResponse(task, response2);
                    response2.Close();
                    request2.Abort();
                }
                if (transferOkay)
                {
                    this.OnDownloadFinished(task, null);
                }
            }
            catch (Exception ex)
            {
                CLogger.LogError("DownloadThread::DownloadFromBreakPoint() - ex: " + ex.Message + ",stackTrack:" + ex.StackTrace);
                this.OnDownloadFinished(task, ex);
            }
        }
        /// <summary>
        /// 不再断点续传
        /// </summary>
        /// <param name="task"></param>
        private void Download(DownloadTask task)
        {
            try
            {
                if (File.Exists(task.storagePath))
                {
                    File.Delete(task.storagePath);
                }
                Uri uri = new Uri(task.url);
                CLogger.Log("DownloadThread::Download() - start download:" + uri);

                HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create(uri);
                request2.ReadWriteTimeout = kTimeOut;
                HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse();
                task.fileLength = response2.ContentLength;
                bool transferOkay = this.ReadBytesFromResponse(task, response2);
                response2.Close();
                request2.Abort();
                if (transferOkay)
                {
                    this.OnDownloadFinished(task, null);
                }
            }
            catch (Exception ex)
            {
                string s = "DownloadThread::Download() - " + ex.Message + "@" + task.url + "#" + ex.StackTrace;
                CLogger.LogError(s);
                this.OnDownloadFinished(task, ex);
            }
        }

        private bool ReadBytesFromResponse(DownloadTask task, WebResponse response)
        {
            bool okay = false;
            DownloadFileTransferInfo fileInfo = _transferMgr.GetDownloadFileInfo(task.file);
            FileUtils.Instance.CheckDirExistsForFile(task.storagePath);

            using (FileStream fileStream = new FileStream(task.storagePath, task.receivedLength == 0 ? FileMode.Create : FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                try
                {
                    fileStream.Position = task.receivedLength;
                    byte[] array = new byte[1024];
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        int bytesRead = 0;
                        while (task.receivedLength < task.fileLength)
                        {
                            bytesRead = responseStream.Read(array, 0, array.Length);
                            fileStream.Write(array, 0, bytesRead);
                            task.receivedLength += bytesRead;
                            _currentTaskReceivedBytes = task.receivedLength;

                            _transferMgr.UpdateFileTransferProgress(fileInfo, task.receivedLength);
                        }

                        okay = true;
                    }

                    if (task.receivedLength != task.fileLength)
                    {
                        string s = string.Format("DownloadThread::ReadBytesFromResponse() - Download length not fit Error:{0}/{1}", task.receivedLength, task.fileLength);
                        CLogger.LogError(s);
                        okay = false;
                        this.OnDownloadFinished(task, new Exception(s));
                    }
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // 忽略
                }
                catch (Exception ex)
                {
                    okay = false;
                    string s = ex.Message + "\n" + ex.StackTrace;
                    CLogger.LogError(s);
                    this.OnDownloadFinished(task, ex);
                }
                finally
                {
                    _transferMgr.SaveTransferProgress("DownloadThread->>ReadBytesFromResponse");
                }
            }

            return okay;
        }

        private void OnDownloadFinished(DownloadTask task, Exception ex)
        {
            if (ex != null)
            {
                if (ex.Message.Contains("Sharing violation on path"))
                {
                    return;
                }
                DownloadError error;
                if (ex.Message.Contains("ConnectFailure") || ex.Message.Contains("NameResolutionFailure") || ex.Message.Contains("No route to host"))
                {
                    error = DownloadError.ServerMaintenance;
                }
                else if (ex.Message.Contains("404"))
                {
                    error = DownloadError.NotFound;
                }
                else if (ex.Message.Contains("403"))
                {
                    error = DownloadError.ServerMaintenance;
                }
                else if (ex.Message.Contains("Disk full"))
                {
                    error = DownloadError.DiskFull;
                }
                else if (ex.Message.Contains("time out") || ex.Message.Contains("Error getting response stream"))
                {
                    error = DownloadError.Timeout;
                }
                else if (ex.Message.Contains("length not fit"))
                {
                    error = DownloadError.NetworkDisconnect;
                }
                else
                {
                    error = DownloadError.Unknown;
                }
                CLogger.LogError("DownloadThread::OnDownloadFinished() - download failed: " + task.url + ", error message:" + ex.Message);
                NotifyResult(task, DownloadStatus.Fail, error);
                Thread.Sleep(1000);  // Downloading thread sleep for 3000ms when a exception occurred in the process of downloading 
            }
            else
            {
                CLogger.Log("DownloadThread::OnDownloadFinished() -" + task.url);
                CheckMD5AfterDownload(task);
            }
        }

        private void CheckMD5AfterDownload(DownloadTask task)
        {
            //bool okay = true;
            //支持设置是否需要校验MD5
            if (this.isCheckMd5)
            {
                string md5 = MD5Hash.GetFileMD5_lowercase(task.storagePath);
                if (md5 != task.md5)
                {
                    _transferMgr.UpdateFileTransferProgress(task.file, 0L);
                    CLogger.Log("DownloadThread::CheckMD5AfterDownload() - Downloaded file with wrong MD5 value! FilePath=" + task.storagePath);
                    File.Delete(task.storagePath);
                    //okay = false;  //delete it and download again
                    task.errorCode = DownloadError.MD5NotMatch;
                    NotifyResult(task, DownloadStatus.Fail, task.errorCode);
                    Thread.Sleep(3000);
                }
                else
                {
                    NotifyResult(task, DownloadStatus.Complete);
                }
            }
            else
            {
                CLogger.Log("DownloadThread::CheckMD5AfterDownload() - No Need Check MD5 value! FilePath=" + task.storagePath);
                NotifyResult(task, DownloadStatus.Complete);
            }

            //if(okay)
            //{
            //	//if the file downloaded is a packed file by zip,unzip it
            //	if(task.storagePath.EndsWith(".zip"))
            //	{
            //		if(unzipPackedFile(task))
            //		{
            //			NotifyResult(task,DownloadStatus.Complete);
            //			_currentTask = null;
            //		}
            //		else
            //		{
            //			NotifyResult(task,DownloadStatus.Fail,task.errorCode);
            //			Thread.Sleep(3000);
            //		}
            //	}
            //	else
            //	{
            //		//set name of downloaded file from  temp to localpath
            //		if(File.Exists(task.storagePath))
            //		{
            //			File.Delete(task.storagePath);
            //		}
            //	        File.Move(temp,task.storagePath);
            //		NotifyResult(task,DownloadStatus.Complete);
            //		_currentTask = null;
            //	}
            //}
        }

        //private bool unzipPackedFile(DownloadTask task)
        //{
        //    bool isSuccess = true;

        //    int index = task.storagePath.LastIndexOf("/");
        //    string unzipDir = Path.GetDirectoryName(task.storagePath);
        //    DirectoryInfo info = new DirectoryInfo(unzipDir);
        //    if (!info.Exists)
        //    {
        //        info.Create();
        //    }

        //    try
        //    {
        //        using (ZipInputStream s = new ZipInputStream(File.OpenRead(task.storagePath)))
        //        {
        //            ZipEntry theEntry;
        //            while ((theEntry = s.GetNextEntry()) != null)
        //            {
        //                string directoryName = Path.GetDirectoryName(theEntry.FileName);
        //                string fileName = Path.GetFileName(theEntry.FileName);
        //                string filePath = Path.Combine(unzipDir, theEntry.FileName);
        //                DirectoryInfo dirInfo = new DirectoryInfo(Path.Combine(unzipDir, directoryName));
        //                if (!dirInfo.Exists)
        //                {
        //                    dirInfo.Create();
        //                }

        //                if (File.Exists(filePath))
        //                {
        //                    File.Delete(filePath);
        //                }

        //                if (fileName != String.Empty)
        //                {
        //                    using (FileStream streamWriter = File.Create(filePath))
        //                    {
        //                        int size = 2048;
        //                        byte[] data = new byte[2048];
        //                        while (true)
        //                        {
        //                            size = s.Read(data, 0, data.Length);
        //                            if (size > 0)
        //                            {
        //                                streamWriter.Write(data, 0, size);
        //                            }
        //                            else
        //                            {
        //                                break;
        //                            }
        //                        }
        //                    }
        //                }
        //            }

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (ex.Message.Contains("Disk full"))
        //        {
        //            task.errorCode = DownloadError.DiskFull;
        //        }
        //        else
        //        {
        //            task.errorCode = DownloadError.Unknown;
        //        }
        //        task.status = DownloadStatus.Fail;
        //        isSuccess = false;
        //    }
        //    return isSuccess;
        //}

        private void NotifyResult(DownloadTask task, DownloadStatus status, DownloadError error = DownloadError.Unknown)
        {
            task.status = status;
            task.errorCode = error;
//			if (status == DownloadStatus.Complete) {
				lock (_finishedTasks) {
					_finishedTasks.Enqueue (task);
				}
//			}
			_currentTask = null;
        }
    }
}
