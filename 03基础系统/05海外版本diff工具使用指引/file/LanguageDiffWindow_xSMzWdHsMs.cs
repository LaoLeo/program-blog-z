using System;
using System.IO;
using System.Text.RegularExpressions;
using Framework;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Pjg;
using System.Collections.Generic;

public enum EReplaceFileType {
    TWLogicReplace = 0,
    XMLogicReplace = 1
}

public class LanguageDiffWindow : EditorWindow {
    const String SHELLEXE_KEY = "SHELLEXEPATH"; 
    String commitIDAfterMerge = "";
    String commitIDBeforeMerge = "";
    EGameLanguageType langType;
    String extensionsPath = "/Scripts/Lua/logic/extensions/";
    GUIStyle fontStyle = new GUIStyle();
    String tip;
    String shellExePath;
    string replaceFile {
        get {
            var prefix = Enum.GetName(typeof(EGameLanguageType),langType);
            return string.Format("Assets/Scripts/Lua/logic/international/{0}/{1}LogicReplace.lua", prefix, prefix.ToUpper());
        }
    }
    void OnEnable () {
        this.titleContent = new GUIContent ("海外版diff工具");
        this.position = new Rect (500, 200, 500, 300);
        shellExePath = PlayerPrefs.GetString(SHELLEXE_KEY);
        langType = CSGameUtils.GetGameLanguageType();
    }

    void OnGUI () {
        GUILayout.BeginVertical ();


        GUILayout.Space (10);
        langType = (EGameLanguageType) EditorGUILayout.EnumPopup ("语言类型：", langType);
        
        GUILayout.Space (10);
        commitIDAfterMerge = EditorGUILayout.TextField ("合并后版本号：", commitIDAfterMerge);
        GUILayout.Space (10);
        commitIDBeforeMerge = EditorGUILayout.TextField ("合并前版本号：", commitIDBeforeMerge);
        GUILayout.Space (10);
        shellExePath = EditorGUILayout.TextField ("自定义执行shell文件exe路径", shellExePath);
        EditorGUILayout.LabelField("非Mac系统需要填写");

        GUILayout.Space (10);
        EditorGUILayout.LabelField(tip, fontStyle, GUILayout.Height(30));

        GUILayout.Space (30);
        if (GUILayout.Button ("导出replace module更改", GUILayout.Height (30))) {
            if (validate()) ExportFile(GenReplaceModuleScriptParam());
        }
        GUILayout.Space (10);
        if (GUILayout.Button ("导出多语言zh/ui/views下预制更新情况", GUILayout.Height (30))) {
            if (validate(false)) ExportFile(GenLangPrefabScriptParam());
        }
        GUILayout.Space (10);
        if (GUILayout.Button ("导出多语言zh/scene目录下资源更新情况", GUILayout.Height (30))) {
            if (validate(false)) ExportFile(GenLangSceneScriptParam());
        }
        GUILayout.Space (10);
        if (GUILayout.Button ("导出fmod audioout event的更改", GUILayout.Height (30))) {
            if (validate(false)){
                string shellPath = Application.dataPath + "/Editor/Shell/fmodDiff.sh";
                ExportFile(GenFmodConfigParam(),shellPath);
            }
        }
        GUILayout.EndVertical ();

    }

    void OnDestroy () {
    }

    private Boolean validate(bool isVaildReplaceLogicFile = true) {
        String path = replaceFile;
        Boolean flag = true;

        tip = "";
        if (langType == EGameLanguageType.zh) {
            tip = "not support zh!";
            fontStyle.normal.textColor = Color.red;
            flag = false;
        }else if (isVaildReplaceLogicFile && !File.Exists(path)) {
            tip =  string.Format("{0} is not exits!", path);
            flag = false;
        } else if (String.IsNullOrEmpty(commitIDAfterMerge) || String.IsNullOrEmpty(commitIDBeforeMerge)) {
            tip = "CommitID Can Not be Empty!";
            flag = false;
        } else if (OSDef.RunOS != OSDef.Mac && String.IsNullOrEmpty(shellExePath)) {
            tip = "Shell Exe Path Is Not Configured!";
            flag = false;
        }

        if (!flag)
        {
            fontStyle.normal.textColor = Color.red;
        }

        return flag;
    } 

    private void ExportFile (string paramStr, string shellPath = "") {
        if (shellPath == "")
        {
            shellPath = Application.dataPath + "/Editor/Shell/diff.sh";
        }
        shellExePath = shellExePath.Replace("\\", "/");
        string result = ExecuteBatch (shellExePath, shellPath, paramStr);
        UnityEngine.Debug.Log (shellExePath);
        UnityEngine.Debug.Log (paramStr);
        if (result.Length > 0)
        {
            fontStyle.normal.textColor = Color.red;
            tip = "执行失败："+result;
            UnityEngine.Debug.LogError(tip);
            return;
        }
        
        UnityEngine.Debug.Log ("execute diff.sh success!");
        fontStyle.normal.textColor = Color.green;
        tip = "执行成功，文件已导出在桌面";

        PlayerPrefs.SetString(SHELLEXE_KEY, shellExePath);
        PlayerPrefs.Save();
    }

    private String GenReplaceModuleScriptParam()
    {
        var moduleArray = parseReplaceFile ();
        String moduleNameStr = String.Join (" ", moduleArray);
        String[] paramArr = { 
            "-C", commitIDBeforeMerge, commitIDAfterMerge, 
            "-M", moduleNameStr, 
            "-P", Application.dataPath + extensionsPath,
            "-N", string.Format("{0}-{1}-{2}", "replace-module", commitIDBeforeMerge, commitIDAfterMerge)
        };
        String paramStr = String.Join (" ", paramArr);
        return paramStr;
    }

    private String[] parseReplaceFile () {
        String path = replaceFile;
        String[] lines = File.ReadAllLines (path);
        String pattern = @"(--\s*)?(\w+)\.replace\(\)";
        List<string> modulesName = new List<string>();
        int needNum = 0;
        int matchNum = 0;
        for (int i = 0; i < lines.Length; i++) {
            String line = lines[i];
            Match match = Regex.Match (line, pattern);
            String moduleName;
            if (!match.Success) continue;

            matchNum++;
            if (match.Value.Contains ("--")) continue;
            moduleName = match.Value.Split ('.') [0];
            var prefix = Enum.GetName(typeof(EGameLanguageType),langType).ToUpper();
            moduleName = moduleName.Replace(prefix, "");
            needNum++;
            moduleName = moduleName+".lua";
            modulesName.Add(moduleName);
        }

        UnityEngine.Debug.LogFormat ("matchNum/moduleNum: " + matchNum + "/" + needNum);

        return modulesName.ToArray();
    }

    // 注意：改方法改写AoEditorUtil.ExecuteBatch，非mac系统下执行sh或bat文件需要传递参数的，需要指定shell exe文件路径
    private static string ExecuteBatch(string exePath, string batchPath,string param = null)
    {
        Process process = new Process();
        FileInfo batFile = new FileInfo(batchPath);
        if(!batFile.Exists)
        {
            UnityEngine.Debug.LogError("batch file not exists! path="+batchPath);
            return String.Empty;
        }
		if (OSDef.RunOS == OSDef.Mac) {
			process.StartInfo.FileName = "bash";
            //bash方式下执行pod相关命令会失败，改成使用终端的方式，需要注意终端的方式执行的话，WaitForExit需要等到手动退出终端进程。
            process.StartInfo.FileName = "/Applications/Utilities/Terminal.app/Contents/MacOS/Terminal";
		} else {
			process.StartInfo.FileName = exePath;
		}
        if (string.IsNullOrEmpty (param)) {
            process.StartInfo.Arguments = batchPath;
        } else {
            process.StartInfo.Arguments = batchPath + " " + param;
        }
        process.StartInfo.CreateNoWindow = false;
        //需要改变执行的工作目录
        process.StartInfo.WorkingDirectory = batFile.Directory.FullName;
		process.StartInfo.UseShellExecute = Application.platform == RuntimePlatform.OSXEditor;
        process.EnableRaisingEvents = true;
        //process.Exited += HandleExited;

        process.StartInfo.RedirectStandardError = true;
        process.Start();
        string result = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.Close();
        return result;
    }

    string GenLangPrefabScriptParam()
    {
        var zhDir = "Assets/GameAssets/language/zh/ui/views";
        var pattern = "*.prefab";
        var fileNames = GetZhResNames(zhDir, pattern);
        String[] paramArr = { 
            "-C", commitIDBeforeMerge, commitIDAfterMerge, 
            "-M", String.Join(" ", fileNames), 
            "-P", Path.Combine(Application.dataPath, "GameAssets/language/zh/ui/views"),
            "-N", string.Format("{0}-{1}-{2}", "zh-UIPrefab", commitIDBeforeMerge, commitIDAfterMerge)
        };
        String paramStr = String.Join (" ", paramArr);
        return paramStr;
    }

    string GenLangSceneScriptParam()
    {
        var zhDir = "Assets/GameAssets/language/zh/scene";
        var pattern = "*.*";
        var fileNames = GetZhResNames(zhDir, pattern);
        String[] paramArr = { 
            "-C", commitIDBeforeMerge, commitIDAfterMerge, 
            "-M", String.Join(" ", fileNames), 
            "-P", Path.Combine(Application.dataPath, "GameAssets/language/zh/scene"),
            "-N", string.Format("{0}-{1}-{2}", "zh-sceneRes", commitIDBeforeMerge, commitIDAfterMerge)
        };
        String paramStr = String.Join (" ", paramArr);
        return paramStr;
    }

    string[] GetZhResNames(string zhDir, string strPattern)
    {
        List<string> results = new List<string>();
        var lang = Enum.GetName(typeof(EGameLanguageType),langType);
        // var langPrefabDir = string.Format("Assets/GameAssets/language/{0}/scene", lang);
        var langResDir = zhDir.Replace("/zh/", "/"+lang+"/");
        var dirInfo = new DirectoryInfo(langResDir);
        // 从zh目录中找出tw目录和zh目录同时存在的资源（检查zh资源的更改情况）
        foreach (FileInfo file in dirInfo.GetFiles(strPattern, SearchOption.AllDirectories))
        {
            if (file.Name.EndsWith(".meta")) {
                continue;
            }
            var zhPrefabPath = FileUtils.Instance.UnifyPath(file.FullName).Replace("/"+lang+"/", "/zh/");
            if (File.Exists(zhPrefabPath))
            {
                results.Add(file.Name);
            }
        }
        // 查找zh目录对应的tw目录中不存在的资源（新增在zh目录的资源）
        // langPrefabDir = string.Format("Assets/GameAssets/language/{0}/scene", "zh");
        dirInfo = new DirectoryInfo(zhDir);
        foreach (FileInfo file in dirInfo.GetFiles(strPattern, SearchOption.AllDirectories))
        {
            if (file.Name.EndsWith(".meta")) {
                continue;
            }
            var twResPath = FileUtils.Instance.UnifyPath(file.FullName).Replace("/zh/", "/"+lang+"/");
            if (!File.Exists(twResPath))
            {
                results.Add(file.Name);
                UnityEngine.Debug.Log("zh目录下新增的res："+file.Name);
            }
        }

        return results.ToArray();
    }

    string GenFmodConfigParam()
    {
        FmodAudioOutHelper.createTempAudioOutEventFile();
        String[] paramArr = {
            commitIDBeforeMerge,
            commitIDAfterMerge,
            FmodAudioOutHelper.EventTempPath,
            FmodAudioOutHelper.GetFmodEventPath(),
            string.Format("{0}-{1}-{2}", "fmod", commitIDBeforeMerge, commitIDAfterMerge)
        };
        String paramStr = String.Join(" ", paramArr);
        return paramStr;
    }
}