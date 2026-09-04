using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AudioVolumeManager : MonoBehaviour
{
    public static AudioVolumeManager Instance{ get; private set; }

    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _seSlider;
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _seSource;

    private Coroutine _duckingCoroutine;

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
    }

    private void Start()
    {
        _bgmSlider.onValueChanged.AddListener(OnSetBGMVolume);
        _seSlider.onValueChanged.AddListener(OnSetSEVolume);
    }

    public void OnSetBGMVolume(float volume)
    {
        if (_duckingCoroutine == null && _bgmSource != null)
        _bgmSource.volume = volume;
    }

    public void OnSetSEVolume(float volume)
    {
        if (_seSource != null)
        _seSource.volume = volume;
    }

    public void FadeOutAndInBGM(float lowVolume, float duration, float holdTime)
    {
        if (_bgmSource == null) return;
        if (_duckingCoroutine != null) StopCoroutine(_duckingCoroutine);
        _duckingCoroutine = StartCoroutine(DoDucking(lowVolume, duration, holdTime));
    }

    private IEnumerator DoDucking(float lowVolume, float duration, float holdTime)
    {
        float startVolume = _bgmSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(startVolume, lowVolume, elapsedTime / duration);
            yield return null;
        }
        _bgmSource.volume = lowVolume;

        yield return new WaitForSeconds(holdTime);

        elapsedTime = 0f;
        float targetVolume = _bgmSlider != null ? _bgmSlider.value : startVolume;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _bgmSource.volume = Mathf.Lerp(lowVolume, targetVolume, elapsedTime / duration);
            yield return null;
        }
        _bgmSource.volume = targetVolume;

        _duckingCoroutine = null;
    }
}