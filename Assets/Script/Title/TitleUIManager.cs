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
    private static readonly Subject<Unit> _onSeActionSubject = new();
    public static Observable<Unit> OnSEAction => _onSeActionSubject;
    private CompositeDisposable _disposables = new();
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _controllersButton;
    [SerializeField]  private Button _exitControllersButton;
    [SerializeField]  private Button _exitButton;
    [SerializeField] private Button _audioButton;
    [SerializeField] private GameObject _controllersObj;

    [SerializeField] private GameObject _audioObj;

    private int _audioLeyer;

    private void Awake()
    {
        if(AudioManager.Instance == null && _audioObj != null)
        {
            GameObject _cloneAudioObj = Instantiate(_audioObj);
        }
        
        _startButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_, ct) =>
        {
            PlayClickSE();
            await ChangeSceneAsync(ct);
        }).AddTo(_disposables);

        _controllersButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await ToggleMenuAsync(ct);
        }).AddTo(_disposables);

        _exitControllersButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await MenuExitAsync(ct);
        }).AddTo(_disposables);

        _exitButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await ExitSceneAsync(ct);
        }).AddTo(_disposables);

        _audioButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await PlayAudioAsync(ct);
        }).AddTo(_disposables);

        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.OnAudioExit
            .ThrottleFirst(TimeSpan.FromSeconds(0.5),UnityTimeProvider.Update)
            .SubscribeAwait(async (_,ct) =>
            {
                PlayClickSE();
                await ExitAudioAsync(ct);
            }).AddTo(_disposables);
        }
    }


    private void Start()
    {
        _controllersObj.SetActive(false);
        _audioLeyer = LayerMask.NameToLayer("Audio");
    }

    private void PlayClickSE()
    {
        Debug.Log("SE: カチッ！");
        _onSeActionSubject.OnNext(Unit.Default);
        
    }

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

    private async UniTask PlayAudioAsync(CancellationToken ct)
    {
        _audioButton.interactable = false;
        if(AudioManager.Instance != null)
        {
            Transform[] allChildren = AudioManager.Instance.GetComponentsInChildren<Transform>(true);
            foreach(Transform child in allChildren)
            {
                if(child.gameObject.layer == _audioLeyer)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
        await UniTask.Yield(ct);
    }

    private async UniTask ExitAudioAsync(CancellationToken ct)
    {
        _audioButton.interactable = true;
        if(AudioManager.Instance != null)
        {
            Transform[] allChildren = AudioManager.Instance.GetComponentsInChildren<Transform>(true);
            foreach(Transform child in allChildren)
            {
                if(child.gameObject.layer == _audioLeyer)
                {
                    Debug.Log("閉じるか確認");
                    child.gameObject.SetActive(false);
                }
            }
        }
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
