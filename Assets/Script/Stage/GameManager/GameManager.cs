using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using unityroom.Api;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<int> _enemyCount = new List<int>();
    private int _count = 0;
    private int _countValue;

    public IGameManagerState _currentGameState;

    private static readonly ReplaySubject<IGameManagerState> _stateChanged = new();
    public static Observable<IGameManagerState> StateChanged => _stateChanged;

    // 💡 タイミングのすれ違いを防ぐため、最新の通知を1件記憶できる ReplaySubject に変更します！
    private static readonly ReplaySubject<Unit> _playerGenerate = new();
    public static Observable<Unit> PlayerGenerate => _playerGenerate;

    private static readonly ReplaySubject<Unit> _enemyGenerate = new();
    public static Observable<Unit> EnemyGenerate => _enemyGenerate;

    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _gameCts;

    private List<GameObject> _enemy = new List<GameObject>();
    private List<float> _time = new List<float>();

    public float LastScore => CalculateScore(_enemyCount.Count > 0 ? _enemyCount[_enemyCount.Count - 1] : 0, 
                                            _time.Count > 0 ? _time[_time.Count - 1] : 0f);

    [SerializeField] public GameObject _countDwonUIObj;
    [SerializeField] public GameObject _stageObj;

    private const int RANKING_ID = 1;

    //public enum GameState { Initialize, Ready, Playing, GameOver, GameClear }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        _gameCts = new CancellationTokenSource();
        ChangeState(new GameManagerStateInitialize());
        //StartGameFlowAsync(_gameCts.Token).Forget();
    }

    public void ChangeState(IGameManagerState newState)
    {
        ChangeStateAsync(newState,_gameCts.Token).Forget();
    }

    private async UniTask ChangeStateAsync(IGameManagerState newState, CancellationToken ct)
    {
        if(_currentGameState != null) await _currentGameState.ExitAsync(ct);
        _currentGameState = newState;
        _stateChanged.OnNext(newState);
        if(_currentGameState != null) 
        {
            await _currentGameState.EnterAsync(this, ct);
        }
    }

    public void PlayerGenerateNotification()
    {
        _playerGenerate.OnNext(Unit.Default);
    }

    public void SendEnemyGenerateNotification()
    {
        _enemyGenerate.OnNext(Unit.Default);
    }

    public void EnemyEvents()
    {
        EnemyController.OnEnemyDead
            .Where(_ => _currentGameState is GameManagerStatePlaying)
            .Subscribe(_ =>
            {
                _count++;
                _countValue = _count;
            }).AddTo(_disposables);
    }

    public void BindEnemySpawnEvents()
    {
        TextMapLoader.EnemyObj
            .Subscribe(_enemyObj =>
            {
                _enemy.Add(_enemyObj);
                Debug.Log($"【GameManager】エネミーをリストに登録しました。 現在の登録数: {_enemy.Count}");
            }).AddTo(_disposables); // ★シーン終了時にきれいに片付く
    }

    public void SetEnemyPhysicsEnabled(bool enabled)
    {
        for(int i = 0; i < _enemy.Count; i ++)
        {
            if(_enemy[i].TryGetComponent<Rigidbody2D>(out var _rd))
            {
                _rd.simulated = enabled;
            }

            if(_enemy[i].TryGetComponent<EnemyController>(out var controller))
            {
                controller.enabled = enabled;
            }
        }
    }

    public void SetPlayerPhysicsEnabled(bool enabled)
    {
        if(Player.Instance.TryGetComponent<Rigidbody2D>(out var _rd))
        {
            _rd.simulated = enabled;
        }

        if(Player.Instance.TryGetComponent<PlayerController>(out var controller))
        {
            controller.enabled = enabled;
        }
    }

    public async UniTask GameOverSequenceAsync(CancellationToken ct)
    {
        Debug.Log("ゲームオーバー演出開始（画面フェードや暗転）");
        _enemyCount.Add(_countValue);
        _time.Add(TimeCounter.CurrentTime);
        float score = LastScore;
        Debug.Log($"スコア: {score} (敵: {_countValue}, タイム: {TimeCounter.CurrentTime})");
        UnityroomApiClient.Instance.SendScore(RANKING_ID, score, ScoreboardWriteMode.Always);
        await UniTask.Delay(2000, cancellationToken: ct);
    }

    public async UniTask GameClearSequenceAsync(CancellationToken ct)
    {
        Debug.Log("クリア演出開始");
        _enemyCount.Add(_countValue);
        _time.Add(TimeCounter.CurrentTime);
        //float score = LastScore;
        int score = Mathf.RoundToInt(LastScore); 
        Debug.Log($"スコア: {score} (敵: {_countValue}, タイム: {TimeCounter.CurrentTime})");
        UnityroomApiClient.Instance.SendScore(RANKING_ID, score, ScoreboardWriteMode.Always);
        await UniTask.Delay(2000, cancellationToken: ct);
    }

    private float CalculateScore(int enemyCount, float time)
    {
         float enemyScore = enemyCount * 100f;
         float timePenalty = time * 5f;
        return Mathf.Max(0f, enemyScore - timePenalty);
    }

    public static void ResetInstance()
    {
        if (Instance != null)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
    }

    private void OnDestroy()
    {
        _gameCts?.Cancel(); _gameCts?.Dispose(); _disposables?.Dispose();
    }
}
