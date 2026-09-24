using UnityEngine;
using R3;
using TMPro;
using System.Threading;
using Cysharp.Threading.Tasks;

public class CountDownManager : MonoBehaviour
{
    private static readonly Subject<Unit> _countDown = new();
    public static readonly Subject<Unit> CountDown = _countDown;
    private static readonly Subject<Unit> _stateSE = new();
    public static readonly Subject<Unit> StateSE = _stateSE;
    private CancellationTokenSource _cts;
    [SerializeField] private TextMeshProUGUI _countDownText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        transform.position = MainCamera.Instance.transform.position + new Vector3(0, 0, 1);
        _cts = new CancellationTokenSource();
        StartCountdownAsync(_cts.Token).Forget();
        Debug.Log("生成されて呼ばれているか");
    }

    private async UniTaskVoid StartCountdownAsync(CancellationToken ct)
    {
        try
        {
            for(int i = 3; 0 < i; i--)
            {
                _countDownText.text = i.ToString();
                await UniTask.Delay(1000,cancellationToken:ct);
            }
            _countDownText.text = "Start";
            _stateSE.OnNext(Unit.Default);
            await UniTask.Delay(1000,cancellationToken:ct);
            _countDown.OnNext(Unit.Default);
            Destroy(gameObject);
        }
        catch(System.OperationCanceledException)
        {
            Debug.Log("エラー");
        }
    }
}
