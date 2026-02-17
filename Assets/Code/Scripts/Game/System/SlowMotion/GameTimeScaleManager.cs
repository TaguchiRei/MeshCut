using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲーム全体の時間スケール管理を行うシングルトン
/// </summary>
public class GameTimeScaleManager : MonoBehaviour, ITimeScaleManagement
{
    public event Action<float> TimeScaleChangeEvent;
    public event Action ReleaseEvent;

    private static GameTimeScaleManager Instance;

    private CancellationTokenSource _cts;
    private float _originalTimeScale = 1f;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
            ServiceLocator.Instance.RegisterService<ITimeScaleManagement>(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Release();
    }


    /// <summary>
    /// 終了時間を指定して時間スケールを変更する
    /// </summary>
    public void SetTimeScale(float scale, float releaseTime)
    {
        if (scale <= 0f)
        {
            Debug.LogWarning("TimeScaleは0より大きくしてください。");
            return;
        }

        TimeScaleChangeEvent?.Invoke(scale);

        // 既に変更中なら解除
        Release();

        _cts = new CancellationTokenSource();

        _originalTimeScale = Time.timeScale;
        Time.timeScale = scale;

        WaitRelease(releaseTime, _cts.Token).Forget();
    }

    public void Release()
    {
        if (_cts == null) return;

        // TimeScale を元に戻す
        Time.timeScale = _originalTimeScale;
        ReleaseEvent?.Invoke();

        // タスクをキャンセルして CTS を破棄
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    public float GetTimeScale()
    {
        return Time.timeScale;
    }

    /// <summary>
    /// 指定時間後に時間スケールを元に戻す非同期処理
    /// </summary>
    private async UniTaskVoid WaitRelease(float releaseTime, CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(releaseTime), cancellationToken: ct);
            Release();
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }
}

/// <summary>
/// ゲーム全体の時間操作用インターフェース
/// </summary>
public interface ITimeScaleManagement
{
    event Action<float> TimeScaleChangeEvent;

    event Action ReleaseEvent;

    /// <summary>
    /// timeScaleを設定する。1がデフォルト
    /// </summary>
    /// <param name="releaseTime">何秒で自動リリースするか</param>
    /// <param name="scale">timeScale</param>
    void SetTimeScale(float scale, float releaseTime);

    /// <summary>
    /// timeScaleを戻す
    /// </summary>
    void Release();

    /// <summary>
    /// timeScaleを取得する
    /// </summary>
    float GetTimeScale();
}