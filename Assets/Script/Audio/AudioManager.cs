using UnityEngine;
using R3;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance{ get; private set; }

    private readonly Subject<Unit> _onAudioExitSubject = new();
    public Observable<Unit> OnAudioExit => _onAudioExitSubject;
    private CompositeDisposable _disposables = new();

    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private Button _audioExit; // AudioExitボタン

    [SerializeField] private AudioSource _bgmSource; // BGM用のAudioSource
    [SerializeField] private AudioSource _seSource;  // 効果音用のAudioSource

    [SerializeField] private AudioClip _currentBGM; // 現在再生中のBGMを保持する変数
    [SerializeField] private AudioClip _currentSE;  // 現在再生中の効果音を保持する変数
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        float bgmMiddleValue = (_bgmSlider.minValue + _bgmSlider.maxValue) / 2f;
        _bgmSlider.value = bgmMiddleValue;

        float seMiddleValue = (_bgmSlider.minValue + _bgmSlider.maxValue) / 2f;
        _seSlider.value = seMiddleValue;

        TitleManager.OnTitleStart
        .Subscribe(_ =>
        {
            Debug.Log("タイトル画面開始時にBGMを再生");
            PlayBGM(_currentBGM);
        }).AddTo(_disposables);

        TitleUIManager.OnSEAction
        .Subscribe(_ =>
        {
            Debug.Log("タイトル画面でボタンが押された時に効果音を再生");
            PlaySE(_currentSE);
        }).AddTo(_disposables);

        // スライダーの変化を監視
        _bgmSlider.OnValueChangedAsObservable()
        .Subscribe(volume => OnSetBGMVolume(volume))
        .AddTo(_disposables);
        
        _seSlider.OnValueChangedAsObservable()
        .Subscribe(volume => OnSetSEVolume(volume))
        .AddTo(_disposables);

        // AudioExitボタンのクリックイベントを購読して通知を送る
        _audioExit.OnClickAsObservable()
        .Subscribe(_ => _onAudioExitSubject.OnNext(Unit.Default))
        .AddTo(_disposables);

        GameUIManager.OnSEAction
        .Subscribe(_ =>
        {
            PlaySE(_currentSE);
        }).AddTo(_disposables);
    }

    public void PlayBGM(AudioClip clip)
    {
        if(_bgmSource == null || clip == null) return; // nullチェック
        if(_bgmSource.clip == clip) return; // 既に再生中のBGMと同じ場合は何もしない
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlaySE(AudioClip clip)
    {
        if(_seSource == null || clip == null) return; // nullチェック
        _seSource.PlayOneShot(clip);
    }

    public void StopSE()
    {
        _seSource.Stop();
    }

    public void OnSetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
    }

    public void OnSetSEVolume(float volume)
    {
        _seSource.volume = volume;
    }
}
