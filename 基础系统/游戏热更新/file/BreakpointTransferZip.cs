using ICSharpCode.SharpZipLib.Zip;
using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
 
 
 
 
/// <summary>
/// 包体信息
/// </summary>
public class BundleInfo
{
    public string path;
    public string name;
    public int length;
    public string MD5;
}
/// <summary>
/// 下载包的信息
/// </summary>
public class DownLoadInfo
{
    public BundleInfo bundle;
    public long size;
    public string DownLoadPath;
    public string SavaPath;
    public string Fodler;
}
/// <summary>
/// 资源下载，检查，更新
/// </summary>
public class Driver : MonoBehaviour
{
    /// <summary>
    /// 连接方式
    /// </summary>
    public URLType urlType = URLType.Project;
 
    /// <summary>
    /// 运行平台
    /// </summary>
    public BuildPlat runPlatm = BuildPlat.Windows64;
 
    /// <summary>
    /// 资源加载方式
    /// </summary>
    public LoadMode loadMode = LoadMode.Resources;
 
    /// <summary>
    /// 加载进度条
    /// </summary>
    public Slider slider;
    /// <summary>
    /// 文本框
    /// </summary>
    public Text infoTex,tipTex,speedtext;
 
    /// <summary>
    /// 提示
    /// </summary>
    public GameObject Tip;
 
    /// <summary>
    /// 游戏管理入口
    /// </summary>
    public GameObject Gameobj;
 
    private List<string> needDeCompress = new List<string>();//本次需要解压的资源包
 
    private WaitForSeconds wait;
    private List<BundleInfo> hasDownLoad;//已经下载过的包体
    private long TotalLength;//需要下载的总大小
    private long hasDownloadLength;//已经下载的大小
 
    private void Awake()
    {    
        //运行设置
        Application.runInBackground = true;//后台运行
        Screen.sleepTimeout = SleepTimeout.NeverSleep;//禁用休眠
        Application.targetFrameRate = 40;//限制帧率
        Screen.fullScreen = true;//启用全屏
    }
    private IEnumerator Start()
    { 
   
        wait = new WaitForSeconds(0.01f);
 
        switch (loadMode)
        {
            case LoadMode.Resources:
                DestroySelf();
                break;
            case LoadMode.ABundle:
                yield return Init(urlType, runPlatm);
                break;
        }
    }
    /// <summary>
    /// 显示提示信息
    /// </summary>
    /// <param name="str"></param>
    private void ShowTip(string str)
    {
        Tip.gameObject.SetActive(true);
        tipTex.text = str;
    }
 
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="urlType"></param>
    /// <param name="runPlatm"></param>
    /// <param name="loading"></param>
    /// <returns></returns>
    public IEnumerator Init(URLType urlType, BuildPlat runPlatm)
    {
        ConfigSetting.urlType = urlType;
        ConfigSetting.RunPlat = runPlatm;
        needDeCompress.Clear();
        if (urlType != URLType.Project)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("无网络连接。");
                ShowTip("网络异常，请检查网络！");
                while (true)
                {
                    yield return null;
                }
            }
            yield return StartCoroutine(CheckMD5());
        }
        else
        {
            Debug.Log("加载本地文件");
        }
        yield return wait;
        Debug.Log("加载完毕!!");
    }
 
 
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckMD5()
    {
        TotalLength = 0;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!File .Exists(ConfigSetting.CopyVersionPath))
        {
            Debug.Log("拷贝版本文件");
            //现将streamingassets下的版本文件拷贝到永久目录
            string from = Application.streamingAssetsPath + "/version.txt"; ;
            WWW www = new WWW(from);
            yield return www;
            Debug.Log("error:" + www.error + "\n" + www.text);
            if (www.isDone)
            {
                Debug.Log("state：" + www.bytes.Length);
                //拷贝到指定路径
                File.WriteAllBytes(ConfigSetting.CopyVersionPath, www.bytes);
            }
            yield return new WaitForSeconds(0.05f);
        }
#endif
        hasDownLoad = new List<BundleInfo>();
        string path = ConfigSetting.SeverAdress + "version.txt";
        Debug.Log("版本下载地址：" + path);
        UnityWebRequest req = UnityWebRequest.Get(path);
        yield return req.SendWebRequest();
        Debug.Log(path + "  ----  " + req.error);
        if (!string.IsNullOrEmpty(req.error))
        {
            ShowTip("服务器异常！！，请退出重试！！");
            while (true)
            {
                yield return null;
            }
        }
        string streamingpath;
        List<BundleInfo> localinfos;
        string msg = req.downloadHandler.text;
        Debug.Log("MSG:" + msg);
        List<BundleInfo> severinfos = JsonMapper.ToObject<List<BundleInfo>>(msg);
        streamingpath = ConfigSetting.CopyVersionPath;
#if UNITY_ANDROID && !UNITY_EDITOR
        string text = File.ReadAllText(streamingpath);
        Debug.Log("读取：" + text);
        localinfos = JsonMapper.ToObject<List<BundleInfo>>(text);
        //yield return new WaitForSeconds(0.05f);
#else
        // streamingpath = Application.streamingAssetsPath + "/version.txt";//"jar:file://" + Application.dataPath + "!/assets" + "/" + ConfigSetting.RunPlat.ToString() + "/version.txt";
        Debug.Log("读取路径：" + streamingpath);
        UnityWebRequest loadVersion = UnityWebRequest.Get(streamingpath);
        yield return loadVersion.SendWebRequest();
        hasDownLoad = localinfos = JsonMapper.ToObject<List<BundleInfo>>(loadVersion.downloadHandler.text);
#endif
        List<DownLoadInfo> needdownload = new List<DownLoadInfo>();//需要下载 的列表
        List<DownLoadInfo> add = new List<DownLoadInfo>();//新增资源
        for (int i = 0; i < severinfos.Count; i++)
        {
            bool contain = false;
            for (int j = 0; j < localinfos.Count; j++)
            {
                if (localinfos[j].path == severinfos[i].path)
                {
                    if (localinfos[j].MD5 != severinfos[i].MD5)
                    {
                        DownLoadInfo info = new DownLoadInfo();
                        info.bundle = severinfos[i];
                        info .DownLoadPath = ConfigSetting.SeverAdress + severinfos[i].path;
                        info .SavaPath = ConfigSetting.Copy_OutPath + severinfos[i].path;
                        int bundleindex = info.SavaPath.LastIndexOf('/');
                        info .Fodler = info.SavaPath.Remove(bundleindex, info.SavaPath.Length - bundleindex);
                        var headRequest = UnityWebRequest.Head(info.DownLoadPath);
                        yield return headRequest.SendWebRequest();
                        info .size = long.Parse(headRequest.GetResponseHeader("Content-Length"));
                        Debug.Log("文件大小："+info.size);
                        TotalLength += info.size;
                        needdownload.Add(info);
                        hasDownLoad.Remove(localinfos[j]);
                    }
                    contain = true;
                }
 
                if (j > 0 )
                    slider.value = j / severinfos.Count;
            }
            if (!contain)
            {
                DownLoadInfo info = new DownLoadInfo();
                info.bundle = severinfos[i];
                info.DownLoadPath = ConfigSetting.SeverAdress + severinfos[i].path;
                info.SavaPath = ConfigSetting.Copy_OutPath + severinfos[i].path;
                int bundleindex = info.SavaPath.LastIndexOf('/');
                info.Fodler = info.SavaPath.Remove(bundleindex, info.SavaPath.Length - bundleindex);
                var headRequest = UnityWebRequest.Head(info.DownLoadPath);
                yield return headRequest.SendWebRequest();
                info.size = long.Parse(headRequest.GetResponseHeader("Content-Length"));
               
                TotalLength += info.size;
                add.Add(info);
            }
        }
        if (File.Exists(ConfigSetting.DeCompressInfo))
            needDeCompress = JsonMapper.ToObject<List<string>>(File.ReadAllText(ConfigSetting.DeCompressInfo));
 
        if (add.Count == 0 && needdownload.Count == 0)
        {
            Debug.Log("无需更新，准备检查资源!");
            if (File.Exists(ConfigSetting.DeCompressInfo))
            {
                //if (onLoading != null)
                //    onLoading.Invoke(1f, "无需更新，准备检查资源！");
 
            }
            else
            {
                infoTex.text = "检查完毕，进入游戏！"; 
                yield break;
            }
        }
        else
        {
            FileInfo fileInfo = null;
            FileStream fs = null;
            //更新资源
            for (int i = 0; i < needdownload.Count; i++)
            {
                //zip包下载
                yield return DownLoad(needdownload[i]);
                continue;
                #region        //ab包下载方式
                //Debug.Log("下载路径：" + loadpath);
                //UnityWebRequest uwr = UnityWebRequest.Get(loadpath);
                //yield return uwr.SendWebRequest();
                //while (uwr.downloadProgress < 1.0f)
                //{
                //    yield return null;
                //}
                //byte[] results = uwr.downloadHandler.data;
                fs.Write(字节数组, 开始位置, 数据长度);
                //fs.Write(results, 0, results.Length);
                //hasDownLoad.Add(needdownload[i]);
                //fs.Flush();     //文件写入存储到硬盘
                //yield return new WaitForEndOfFrame();
                //fs.Close();     //关闭文件流对象
                //fs.Dispose();   //销毁文件对象
                #endregion
               
            }
            //新增资源
            for (int i = 0; i < add.Count; i++)
            {
                Debug.Log("需要下载：" + add[i].bundle.path + "\n 路径：" + add[i].SavaPath);
                //if (add[i].bundle .path.Contains("//"))
                //{
                //    int index = add[i].bundle.path.LastIndexOf('/');
                //    string fodler = ConfigSetting.Copy_OutPath + add[i].bundle.path.Remove(index, add[i].bundle.path.Length - index);
                //    Debug.Log("文件夹：" + fodler);
                //    if (!Directory.Exists(fodler))
                //    {
                //        Debug.Log("创建文件夹：" + fodler);
                //        Directory.CreateDirectory(fodler);
                //    }
                //}
                if (!Directory.Exists(add [i].Fodler))
                {
                    Debug.Log("创建文件夹：" + add[i].Fodler);
                    Directory.CreateDirectory(add[i].Fodler);
                }
                //zip包下载
                //infoTex.text = "正在下载资源   " + i + "/" + add.Count;
                yield return DownLoad(add[i]);
                continue;
                #region  下载ab
                //fileInfo = new FileInfo(filepath);
                //fs = fileInfo.Create();
                //UnityWebRequest uwr = UnityWebRequest.Get(loadpath);
                //yield return uwr.SendWebRequest();
                //while (uwr.downloadProgress < 1.0f)
                //{
                //    yield return null;
                //}
                //byte[] results = uwr.downloadHandler.data;
                //Debug.Log("下载数据：" + results.Length);
                fs.Write(字节数组, 开始位置, 数据长度);
                //fs.Write(results, 0, results.Length);
                //hasDownLoad.Add(add[i]);
                //fs.Flush();     //文件写入存储到硬盘
                //fs.Close();     //关闭文件流对象
                //fs.Dispose();   //销毁文件对象
                //yield return new WaitForEndOfFrame();
                //fileInfo = null;
                //fs = null;
            
                #endregion
            }
 
            speedtext.text = "";
 
            if (hasDownLoad.Count > 0)
            {
                string str = JsonMapper.ToJson(hasDownLoad);
                //Debug.Log(str);
                //存储下载文件
                File.WriteAllText(ConfigSetting.CopyVersionPath, str);
                //存储需要解压的文件
                File.WriteAllText(ConfigSetting.DeCompressInfo, JsonMapper.ToJson(needDeCompress));
                hasDownLoad.Clear();
            }
        }
        int count = needDeCompress.Count;
        while (needDeCompress.Count > 0)
        {
            yield return SaveZip(needDeCompress[0], count);
            needDeCompress.RemoveAt(0);
        }
        Debug.Log("剩余需要解压个数：" + needDeCompress.Count);
        if (needDeCompress.Count == 0)
        {
            if (File.Exists(ConfigSetting.DeCompressInfo))
                File.Delete(ConfigSetting.DeCompressInfo);
        }
        else
        {
            //更新
            File.WriteAllText(ConfigSetting.DeCompressInfo, JsonMapper.ToJson(needDeCompress));
        }
        infoTex.text = "即将进入游戏！！";
    }
 
    /// <summary>
    /// 开始下载zip包
    /// </summary>
    /// <param name="adress"></param>
    /// <param name="savepath"></param>
    /// <returns></returns>
    private IEnumerator DownLoad(DownLoadInfo bundle)
    {
        Debug.Log("downloadpath: " + bundle.DownLoadPath );
        Debug.Log("savepath:" + bundle .SavaPath );
        
        Debug.Log("fodler：" + bundle .Fodler);
        if (!Directory.Exists(bundle.Fodler))
        {
            Directory.CreateDirectory(bundle.Fodler);
        }
        
        UnityWebRequest req = UnityWebRequest.Get(bundle.DownLoadPath);
        long fileLength = 0;
        //获取下载文件的总长度
        Debug.Log("totalLength:" + bundle.size );//ConfigSetting.Copy_OutPath + ConfigSetting.AssetName;
        FileStream fs = new FileStream(bundle .SavaPath , FileMode.OpenOrCreate, FileAccess.Write);
        //获取文件现在的长度
        fileLength = fs.Length;
        Debug.Log("fileLength:" + fileLength);
        if (fileLength > 0)
        {
            //设置开始下载文件从什么位置开始
            req.SetRequestHeader("Range", "bytes=" + fileLength + "-");//这句很重要
            fs.Seek(fileLength, SeekOrigin.Begin);//将该文件的指针移动到当前长度，即继续存储
        }
        if (fileLength < bundle .size)
        {
            //float progress = 0;
            long hasload = 0;
            req.SendWebRequest();
            float usedTime = 0;
            long lastSecLoad = 0;
            while (!req.isDone)
            {
                usedTime += Time.deltaTime;
                if(usedTime >= 1)
                {
                    usedTime = 0;
                    speedtext.text  = GetSize((long)req.downloadedBytes - lastSecLoad) + "/s";
                    lastSecLoad = (long)req.downloadedBytes;
                }
                hasload = hasDownloadLength + (long)req.downloadedBytes;
                if (hasload > 0)
                {
                    slider.value = (float)hasload  / TotalLength;
                    infoTex.text = "正在下载资源 " + GetSize(hasload) + "/" + GetSize(TotalLength);
                }
                yield return null;
            }
            hasDownloadLength += (long)req.downloadedBytes;
            Debug.Log(req.downloadHandler.data.Length);
           
            //将本次下载得到的数据存储到文件中
            fs.Write(req.downloadHandler.data, 0, req.downloadHandler.data.Length);
            yield return new WaitForSeconds(0.1f);
            fileLength += req.downloadHandler.data.Length;
            fs.Close();
            fs.Dispose();
            if (fileLength < bundle .size)
            {
                ShowTip("下载失败，请重启后重试！");
                while (true)
                {
                    yield return null;
                }
            }
        }
        hasDownLoad.Add(bundle .bundle);
        needDeCompress.Add(bundle .SavaPath);
    }
 
    Stream readSteam;
    /// <summary> 
    /// 解压功能(下载后直接解压压缩文件到指定目录) 
    /// </summary> 
    /// <param name="zipedFolder">指定解压目标目录(每一个Obj对应一个Folder)</param> 
    /// <param name="password">密码</param> 
    /// <returns>解压结果</returns> 
    private IEnumerator SaveZip(string zipPath, int index)
    {
        Debug.Log("开始解压：" + zipPath);
        if (!File.Exists(zipPath))
        {
            Debug.Log("已经解压过了，无需解压  \n " + zipPath);
            yield break;
        }
        //byte[] ZipByte = File.ReadAllBytes(savepath);
        ZipInputStream zipStream = null;
        ZipEntry entry = null;
        string fileName;
        int length = zipPath.LastIndexOf('/');
        string fodlerPath = zipPath.Remove(length, zipPath.Length - length);
        Debug.Log("文件夹: " + fodlerPath);
        if (!Directory.Exists(fodlerPath))
        {
            Directory.CreateDirectory(fodlerPath);
        }
 
        //直接使用 
        readSteam = new FileStream(zipPath, FileMode.Open, FileAccess.Read);//new MemoryStream(ZipByte);
        zipStream = new ZipInputStream(readSteam);
        byte[] data;
        int size = 2048;
        float saved = 0;
        while ((entry = zipStream.GetNextEntry()) != null)
        {
            //Debug.Log("总数据：" + zipStream.Length);
            data = new byte[zipStream.Length];
            saved += zipStream.Length;
            fileName = fodlerPath + "/" + entry.Name;
            if (entry.IsDirectory)
            {
                if (!Directory.Exists(fileName))
                {
                    Directory.CreateDirectory(fileName);
                }
            }
            else if (!string.IsNullOrEmpty(entry.Name))
            {
                Debug.Log("filename: " + entry.Name);
                if (entry.Name.Contains("\\") || entry.Name.Contains("/"))
                {
                    string[] array = entry.Name.Split("\\".ToCharArray());
                    string parentDir = fodlerPath + "/" + array[0];
                    for (int i = 1; i < array.Length - 1; i++)
                    {
                        parentDir += "/";
                        parentDir += array[i];
                    }
                    Debug.Log("所属文件夹：" + parentDir);
                    if (!Directory.Exists(parentDir))
                    {
                        Debug.Log("创建文件夹：" + parentDir);
                        Directory.CreateDirectory(parentDir);
                    }
                    fileName = parentDir + "/" + array[array.Length - 1];
                }
                Debug.Log("savepath:" + fileName);
                ;
                //WaitForSeconds sec = new WaitForSeconds(0.0001f);
                using (FileStream s = File.Create(fileName))
                {
                    //size = zipStream.Read(data, 0, data.Length);
                    //s.Write(data, 0, size);
                    while (true)
                    {
                        size = zipStream.Read(data, 0, data.Length);
                        if (size <= 0) break;
                        s.Write(data, 0, size);
                        //yield return null;
                        saved += 2048;
                        slider.value = saved / readSteam.Length;
                        Debug.Log(saved / zipStream.Length);
                        //if (onLoading != null)
                        //{
                        //    onLoading.Invoke(saved / stream.Length, "正在解压 " + (index - needDeCompress.Count + 1) + "/" + index);
                        //}
                    }
                    //saved = 0;
                    s.Close();
                }
                yield return null;
            }
            infoTex.text = "正在解压... " + (index - needDeCompress.Count + 1) + "/" + index;
            yield return null;
        }
        zipStream.Close();
        readSteam.Close();
        readSteam = null;
        yield return wait;
        Debug.Log("解压完成");
        File.Delete(zipPath);
    }
 
 
    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    public string GetSize(long b)
    {
        if (b.ToString().Length <= 10)
            return GetMB(b);
        if (b.ToString().Length >= 11 && b.ToString().Length <= 12)
            return GetGB(b);
        if (b.ToString().Length >= 13)
            return GetTB(b);
        return String.Empty;
    }
 
    /// <summary>
    /// 将B转换为TB
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    private string GetTB(long b)
    {
        for (int i = 0; i < 4; i++)
        {
            b /= 1024;
        }
        return b + "TB";
    }
 
    /// <summary>
    /// 将B转换为GB
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    private string GetGB(long b)
    {
        for (int i = 0; i < 3; i++)
        {
            b /= 1024;
        }
        return b + "GB";
    }
 
    /// <summary>
    /// 将B转换为MB
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    private string GetMB(long size)
    {
        float count = (float)size; 
        if (count < 1024)
            return count.ToString("f2") + "b";
        if (count < 1024 * 1024)
            return (count / 1024).ToString("f2") + "kb";
       
        for (int i = 0; i < 2; i++)
        {
            count /= 1024;
        }
        return count.ToString("f2") + "MB";
    }
    /// <summary>
    /// 将B转换为KB
    /// </summary>
    /// <param name="b"></param>
    /// <returns></returns>
    private string GetKB(long size)
    {
        size /= 1024;
        return size + "KB";
    }
 
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
 
    private void OnDestroy()
    {
        if (urlType != URLType.Project && hasDownLoad.Count > 0)
        {
            string str = JsonMapper.ToJson(hasDownLoad);
            //Debug.Log(str);
            //存储下载文件
            File.WriteAllText(ConfigSetting.CopyVersionPath, str);
            //存储需要解压的文件
            File.WriteAllText(ConfigSetting.DeCompressInfo, JsonMapper.ToJson(needDeCompress));
            hasDownLoad.Clear();
        }
        if(readSteam !=null)
        {
            readSteam.Close();
            readSteam = null;
        }
    }
 
}