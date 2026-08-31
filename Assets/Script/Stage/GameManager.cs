using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private List<int> _enemyCount = new List<int>();
    private int _count = 0;
    private int _countValue;

    // 💡 外部から見れるように public static に変更（TextMapLoaderがこれを見ます）
    private static readonly SerializableReactiveProperty<GameState> _currentState = new(GameState.Initialize);
    public static ReadOnlyReactiveProperty<GameState> CurrentState => _currentState;

    // 💡 タイミングのすれ違いを防ぐため、最新の通知を1件記憶できる ReplaySubject に変更します！
    private static readonly ReplaySubject<Unit> _playerGenerate = new(1);
    public static Observable<Unit> PlayerGenerate => _playerGenerate;

    private static readonly ReplaySubject<Unit> _enemyGenerate = new(1);
    public static Observable<Unit> EnemyGenerate => _enemyGenerate;

    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _gameCts;

    private List<GameObject> _enemy = new List<GameObject>();

    [SerializeField] private GameObject _countDwonUIObj;
    [SerializeField] private GameObject _stageObj;

    public enum GameState { Initialize, Ready, Playing, GameOver, GameClear }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        _gameCts = new CancellationTokenSource();
        StartGameFlowAsync(_gameCts.Token).Forget();
    }

    private async UniTaskVoid StartGameFlowAsync(CancellationToken ct)
    {
        try
        {
            // --------------------------------------------------
            // 1. 初期化フェーズ
            // --------------------------------------------------
            _currentState.Value = GameState.Initialize;

            // 💡 ステージの生成と、TextMapLoaderの地形生成完了をここでしっかり「待つ」
            await InitializeGameAsync(ct);
            // プレイヤーが生成されるのを安全に待つ
            await UniTask.WaitUntil(() => Player.Instance != null, cancellationToken: ct);

            // プレイヤーのイベント紐付け
            BindPlayerEvents();
            EnemyEvents();
            BindEnemySpawnEvents();
            Debug.Log("プレイヤーの生成を検知し、死亡イベントの紐付けに成功しました。");

            // --------------------------------------------------
            // 2. 開始演出フェーズ（カウントダウン）
            // --------------------------------------------------
            _currentState.Value = GameState.Ready;
            
            // 💡 カウントダウンUIが完全に終了するまで、ここでしっかり「待つ」
            await ReadySequenceAsync(ct);

            // --------------------------------------------------
            // 3. プレイ中フェーズ
            // --------------------------------------------------
            _currentState.Value = GameState.Playing;
            
            // 💡 ステートを Playing に変えた「後」に、満を持して通知を発射する！
            // これにより、TextMapLoader側が確実に敵をスポーンさせられます
            Debug.Log("ゲーム本番開始！Playing通知を送信しました。");
        
            // プレイヤーの死亡、またはステージクリアのステート変更を「待つ」ループ
            await UniTask.WaitUntil(() => 
                _currentState.Value == GameState.GameOver || 
                _currentState.Value == GameState.GameClear, 
                cancellationToken: ct);

            // --------------------------------------------------
            // 4. 終了フェーズ（判定）
            // --------------------------------------------------
            if (_currentState.Value == GameState.GameOver) { await GameOverSequenceAsync(ct); }
            else if (_currentState.Value == GameState.GameClear) { await GameClearSequenceAsync(ct); }
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("ゲームフローがキャンセルされました。");
        }
    }

    private void BindPlayerEvents()
    {
        Player.Instance.OnDeath
            .Where(_ => _currentState.Value == GameState.Playing)
            .Subscribe(_ => _currentState.Value = GameState.GameOver)
            .AddTo(_disposables);
        
        GoalController.GoalTrigger
            .Where(_ => _currentState.Value == GameState.Playing)
            .Subscribe(_ => _currentState.Value = GameState.GameClear)
            .AddTo(_disposables);
    }

    private void EnemyEvents()
    {
        EnemyController.OnEnemyDead
            .Where(_ => _currentState.Value == GameState.Playing)
            .Subscribe(_ =>
            {
                _count++;
                _countValue = _count;
                Debug.Log($"敵を倒した数{_countValue}");
            }).AddTo(_disposables);
    }

    private void BindEnemySpawnEvents()
    {
        TextMapLoader.EnemyObj
        .Subscribe(_enemyObj =>
        {
            _enemy.Add(_enemyObj);
            Debug.Log(_enemy.Count);
        }).AddTo(this);
    }

    /// <summary>
    /// 💡 ステージオブジェクトを生成し、中身の生成完了を「待つ」メソッド
    /// </summary>
    private async UniTask InitializeGameAsync(CancellationToken ct)
    {
        Debug.Log("ステージやデータの生成中...");
        Instantiate(_stageObj);

        // 💡 適当に1秒待つのではなく、「マップ生成が完了した通知」が届くまでUniTaskでじっと待ち合わせる！
        // ※もしTextMapLoader側がまだSubjectを公開していなければ、ここを一時的に await UniTask.Delay(1000, cancellationToken: ct); に戻してください
        if (TextMapLoader.MapGenerate != null)
        {
            await TextMapLoader.MapGenerate.FirstAsync(ct);
        }
        _playerGenerate.OnNext(Unit.Default);
        Debug.Log("地形の生成を検知しました。初期化フェーズを抜けます。");
    }

    /// <summary>
    /// 💡 カウントダウンUIを生成し、演出の終了を「待つ」メソッド
    /// </summary>
    private async UniTask ReadySequenceAsync(CancellationToken ct)
    {
        Debug.Log("Ready... Go! の演出開始（UI生成）");
        Instantiate(_countDwonUIObj);
        _enemyGenerate.OnNext(Unit.Default);
        Rigidbody2D _playerrd = Player.Instance.GetComponent<Rigidbody2D>();
        _playerrd.simulated = false;
        for(int i = 0; i < _enemy.Count; i ++)
        {
            if(_enemy[i].TryGetComponent<Rigidbody2D>(out var _rd))
            {
                _rd.simulated = false;
            }

            if(_enemy[i].TryGetComponent<EnemyController>(out var controller))
            {
                controller.enabled = false;
            }
        }

        // 💡 1.5秒ただ待つのではなく、カウントダウンUIが「Start!」と数え終わった通知をここでガチッと待ち合わせる！
        await CountDownManager.CountDown.FirstAsync(ct);
        
        Debug.Log("カウントダウン終了の通知をキャッチしました。演出フェーズを抜けます。");
        for(int i = 0; i < _enemy.Count; i ++)
        {
            if(_enemy[i].TryGetComponent<Rigidbody2D>(out var _rd))
            {
                _rd.simulated = true;
            }

            if(_enemy[i].TryGetComponent<EnemyController>(out var controller))
            {
                controller.enabled = true;
            }
        }
        _playerrd.simulated = true;
    }

    private async UniTask GameOverSequenceAsync(CancellationToken ct)
    {
        Debug.Log("ゲームオーバー演出開始（画面フェードや暗転）");
        _enemyCount.Add(_countValue);
        await UniTask.Delay(2000, cancellationToken: ct);
    }

    private async UniTask GameClearSequenceAsync(CancellationToken ct)
    {
        Debug.Log("クリア演出開始");
        _enemyCount.Add(_countValue);
        await UniTask.Delay(2000, cancellationToken: ct);
    }

    private void OnDestroy()
    {
        _gameCts?.Cancel(); _gameCts?.Dispose(); _disposables?.Dispose();
    }
}
