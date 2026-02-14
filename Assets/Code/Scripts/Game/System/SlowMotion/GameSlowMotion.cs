using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲーム全体の時間スケール管理を行うシングルトン
/// </summary>
public class GameSlowMotion : MonoBehaviour, IGameSlowMotion
{
    private static GameSlowMotion Instance;

    private CancellationTokenSource _cts;
    private float _originalTimeScale = 1f;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
            ServiceLocator.Instance.RegisterService<IGameSlowMotion>(this);
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
    public void SetTimeScale(float releaseTime, float scale)
    {
        if (scale <= 0f)
        {
            Debug.LogWarning("TimeScaleは0より大きくしてください。");
            return;
        }

        // 既に変更中なら解除
        Release();

        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

        _originalTimeScale = Time.timeScale;
        Time.timeScale = scale;

        WaitRelease(releaseTime, _cts.Token).Forget();
    }

    /// <summary>
    /// 時間スケールを元に戻す
    /// </summary>
    public void Release()
    {
        if (_cts == null) return;

        // TimeScale を元に戻す
        Time.timeScale = _originalTimeScale;

        // タスクをキャンセルして CTS を破棄
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
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
public interface IGameSlowMotion
{
    void SetTimeScale(float releaseTime, float scale);
    void Release();
}
