using UnityEngine;
using R3;
using TMPro;

public class TimeCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timerText;

    private float _realTime;

    private bool _isCounting = false;

    public static float CurrentTime{get; private set;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.StateChanged
        .Subscribe(state =>
        {
            switch(state)
            {
                case GameManagerStateInitialize:
                    _realTime = 0f;
                    CurrentTime= 0f;
                    _isCounting = false;
                    Debug.Log("タイムリセット確認");
                break;
                case GameManagerStatePlaying:
                    _isCounting = true;
                    Debug.Log("タイム起動確認");
                break;
                case GameManagerStateGameClear:
                    _isCounting = false;
                    CurrentTime = _realTime;
                    Debug.Log("タイムカウント停止GameManagerStateGameClear");
                break;
                case GameManagerStateGameOver:
                    _isCounting = false;
                    CurrentTime = _realTime;
                    Debug.Log("タイムカウント停止GameManagerStateGameOver");
                break;
                default:
                    _isCounting = false;
                    Debug.Log("タイムカウント停止default");
                break;
            }
        } ).AddTo(this);
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(_isCounting + "タイム確認");
        if(!_isCounting) return;
        _realTime += Time.deltaTime;
        //Debug.Log(_realTime);

        int _minutes = Mathf.FloorToInt(_realTime / 60f);
        int _seconds = Mathf.FloorToInt(_realTime % 60f);

        _timerText.text = string.Format("{0:00}:{1:00}",_minutes,_seconds);
        Debug.Log(_timerText);
    }
}
