using UnityEngine;
using R3;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;

public class TitleUIManager : MonoBehaviour
{
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _controllersButton;
    [SerializeField]  private Button _exitControllersButton;
    [SerializeField]  private Button _exitButton;
    [SerializeField] private GameObject _controllersObj;

    private void Start()
    {
        _controllersObj.SetActive(false);

        _startButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_, ct) =>
        {
            PlayClickSE();
            await ChangeSceneAsync(ct);
        }).AddTo(this);

        _controllersButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await ToggleMenuAsync(ct);
        }).AddTo(this);

        _exitControllersButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await MenuExitAsync(ct);
        }).AddTo(this);

        _exitButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await ExitSceneAsync(ct);
        }).AddTo(this);
    }

    private void PlayClickSE() => Debug.Log("SE: カチッ！");

    private async UniTask ChangeSceneAsync(CancellationToken ct)
    {
        _startButton.interactable = false;
        await UniTask.Delay(500,cancellationToken : ct);
        await SceneManager.LoadSceneAsync("Stage01").WithCancellation(ct);
    }

    private async UniTask ToggleMenuAsync(CancellationToken ct)
    {
        _controllersButton.interactable = false;
        _exitControllersButton.interactable = true;
        _controllersObj.SetActive(true);
        await UniTask.Yield(ct);
    }

    private async UniTask MenuExitAsync(CancellationToken ct)
    {
        _exitControllersButton.interactable = false;
        _controllersButton.interactable = true;
        _controllersObj.SetActive(false);
        await UniTask.Yield(ct);
    }

    private async UniTask ExitSceneAsync(CancellationToken ct)
    {
        _exitButton.interactable = false;
        await UniTask.Delay(500,cancellationToken : ct);
        #if UNITY_EDITOR
        // Unityエディタの「再生モード」を停止する
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 本番のゲーム（PC/スマホアプリ）を終了する
        Application.Quit();
#endif
    }
}
