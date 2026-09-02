using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance{ get; private set; }

    [SerializeField] private AudioSource _bgmSource; // BGM用のAudioSource
    [SerializeField] private AudioSource _seSource;  // 効果音用のAudioSource

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

    public void PlayBGM(AudioClip clip)
    {
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
        _seSource.PlayOneShot(clip);
    }

    public void StopSE()
    {
        _seSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        _bgmSource.volume = volume;
    }

    public void SetSEVolume(float volume)
    {
        _seSource.volume = volume;
    }
}
