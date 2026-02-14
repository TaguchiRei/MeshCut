using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorStartupHandler
{
    private const string INIT_KEY = "MyTool_Initialized";

    private const string SESSION_Start_Time_KEY = "SessionStartTime";
    private const string LAST_SAVE_TIME_KEY = "LastSaveTime";


    private static readonly string _folderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Documents",
        "WorkTrack"
    );

    private static DateTime _startDate;
    private static DateTime _lastSaveTime;
    private static WorkManagement _workManagement;

    static EditorStartupHandler()
    {
        EditorApplication.quitting += OnQuitEditor;
        EditorApplication.update += OnEditorUpdate;

        //スタートした時間がすでにセットされているかを調べ、セットされていなかったら現在時刻を指定
        var time = SessionState.GetString(SESSION_Start_Time_KEY, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        _startDate = DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

        //最後に自動セーブした時間を調べる
        var lastSave = SessionState.GetString(LAST_SAVE_TIME_KEY, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        _lastSaveTime = DateTime.ParseExact(lastSave, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture);

        //フォルダがなければ作成
        if (!ExistFolder())
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string targetPath = Path.Combine(documents, "WorkTrack");
            Directory.CreateDirectory(targetPath);
        }

        //ファイルがなければ無視し、あればデータを読み込む
        _workManagement = new WorkManagement();
        if (ExistFile($"{_startDate.Date:yyyy-MM-dd}.txt"))
        {
            string data = File.ReadAllText(Path.Combine(_folderPath, $"{_startDate.Date:yyyy-MM-dd}.txt"));
            _workManagement.FromString(data);
        }

        if (SessionState.GetBool(INIT_KEY, false))
            return; // このエディタセッション中に既に実行済みならスキップ

        string projectFolderName = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
        if (_workManagement.TryGetLastWork(projectFolderName, out var last) && last.IsWorking)
        {
            Debug.Log("前回の起動時はエディタが予期せず終了した可能性があります。");
            last.IsWorking = false;
        }
        SessionStart("作業開始を記録しました");
    }

    private static void SessionStart(string messageText)
    {
        SessionState.SetString(SESSION_Start_Time_KEY, _startDate.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        SessionState.SetBool(INIT_KEY, true);
        Debug.Log(messageText);
    }

    private static void OnEditorUpdate()
    {
        TimeSpan diff = DateTime.Now - _lastSaveTime;

        //五分ごとに自動セーブを行う
        if (diff.Duration() >= TimeSpan.FromMinutes(5))
        {
            SaveWork(true);
            _lastSaveTime = DateTime.Now;
            Debug.Log("定期記録を行いました");
        }

        if (DateTime.Now.Date == _startDate.Date) return;

        SaveWork();
        _startDate = DateTime.Now;
        SessionStart($"日付が変わったため、記録を終了して{DateTime.Now.Date}として再開しました");
    }

    private static void OnQuitEditor()
    {
        SaveWork();
    }

    private static void SaveWork(bool isWorking = false)
    {
        SessionState.SetString(LAST_SAVE_TIME_KEY, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

        string projectFolderName = Path.GetFileName(Path.GetDirectoryName(Application.dataPath));
    
        DateTime endTime = DateTime.Now;
    
        // 終了時刻が0:00:00の場合は前日の23:59:59に調整
        if (endTime.TimeOfDay == TimeSpan.Zero)
        {
            endTime = endTime.AddMilliseconds(-1);
        }

        if (_workManagement.TryGetLastWork(projectFolderName, out var last) && last.IsWorking)
        {
            last.End = endTime;
            last.IsWorking = isWorking;
        }
        else
        {
            _workManagement.AddWork(new Work(projectFolderName, _startDate, endTime, isWorking));
        }

        string path = Path.Combine(_folderPath, $"{_startDate.Date:yyyy-MM-dd}.txt");
        File.WriteAllText(path, _workManagement.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// ドキュメントフォルダ内に「WorkTrack」フォルダが存在するか確認する。
    /// </summary>
    private static bool ExistFolder()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string targetPath = Path.Combine(documents, "WorkTrack");

        return Directory.Exists(targetPath);
    }

    /// <summary>
    /// WorkTrackフォルダの中に指定したファイルがあるかどうかを確認する
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private static bool ExistFile(string fileName)
    {
        if (string.IsNullOrWhiteSpace(_folderPath) || string.IsNullOrWhiteSpace(fileName))
            return false;
        string filePath = Path.Combine(_folderPath, fileName);
        return File.Exists(filePath);
    }
}