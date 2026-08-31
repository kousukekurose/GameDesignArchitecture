using UnityEngine;
using R3;
using TMPro;
using System.Threading;
using Cysharp.Threading.Tasks;

public class CountDownManager : MonoBehaviour
{
    private static readonly Subject<Unit> _countDown = new();
    public static readonly Subject<Unit> CountDown = _countDown;
    private CancellationTokenSource _cts;

    [SerializeField] private TextMeshProUGUI _countDownText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
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
