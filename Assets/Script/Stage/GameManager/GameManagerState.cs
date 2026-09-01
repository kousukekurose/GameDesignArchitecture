using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

// MonoBehaviorの継承は不要であれば外しても良いですが、元の形を維持
public class GameManagerState : MonoBehaviour{}

public class GameManagerStateInitialize : IGameManagerState
{
    protected GameManager _gameManager;

    public async UniTask EnterAsync(GameManager gameManager, CancellationToken ct)
    {
        _gameManager = gameManager;
        
        Object.Instantiate(_gameManager._stageObj);

        // マップ生成完了を待つ
        if (TextMapLoader.MapGenerate != null)
        {
            await TextMapLoader.MapGenerate.FirstAsync(ct);
        }
        
        _gameManager.PlayerGenerateNotification();
        await UniTask.WaitUntil(() => Player.Instance != null, cancellationToken: ct);
        
        // エネミーのイベント紐付け（GameManager側で適切に管理されている前提）
        _gameManager.BindEnemySpawnEvents();

        if(_gameManager._currentGameState == this)
        {
            _gameManager.ChangeState(new GameManagerStateReady());
        }
    }

    public async UniTask ExitAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }
}

public class GameManagerStateReady : IGameManagerState
{
    protected GameManager _gameManager;
    
    public async UniTask EnterAsync(GameManager gameManager, CancellationToken ct)
    {
        _gameManager = gameManager;
        
        Object.Instantiate(_gameManager._countDwonUIObj);
        _gameManager.SendEnemyGenerateNotification();
        
        // 物理演算の一時停止
        _gameManager.SetPlayerPhysicsEnabled(false);
        _gameManager.SetEnemyPhysicsEnabled(false);
        
        // カウントダウン終了を待つ
        await CountDownManager.CountDown.FirstAsync(ct);
        
        // 物理演算の再開
        _gameManager.SetEnemyPhysicsEnabled(true);
        _gameManager.SetPlayerPhysicsEnabled(true);
        
        if(_gameManager._currentGameState == this)
        {
            _gameManager.ChangeState(new GameManagerStatePlaying());
        }
    }

    public async UniTask ExitAsync(CancellationToken ct)
    {
        await UniTask.CompletedTask;
    }
}

public class GameManagerStatePlaying : IGameManagerState
{
    protected GameManager _gameManager;
    // プレイ中のみ有効なイベントを管理する使い捨てのゴミ箱
    private readonly CompositeDisposable _disposables = new();

    public async UniTask EnterAsync(GameManager gameManager, CancellationToken ct)
    {
        _gameManager = gameManager;

        // ★プレイ中ステートに入った瞬間にイベントを紐付ける
        Player.Instance.OnDeath
            .Subscribe(_ => _gameManager.ChangeState(new GameManagerStateGameOver()))
            .AddTo(_disposables);
        
        GoalController.GoalTrigger
            .Subscribe(_ => _gameManager.ChangeState(new GameManagerStateGameClear()))
            .AddTo(_disposables);

        DeathController.DeathTrigger
            .Subscribe(_ => _gameManager.ChangeState(new GameManagerStateGameOver()))
            .AddTo(_disposables);
        
        await UniTask.CompletedTask;
    }

    public async UniTask ExitAsync(CancellationToken ct)
    {
        _disposables.Dispose();
        await UniTask.CompletedTask;
    }
}

public class GameManagerStateGameOver : IGameManagerState
{
    protected GameManager _gameManager;
    public async UniTask EnterAsync(GameManager gameManager, CancellationToken ct)
    {
        _gameManager = gameManager;
        Debug.Log("GameManagerStateGameOver.EnterAsync");
        _gameManager.SetPlayerPhysicsEnabled(false);
        await _gameManager.GameOverSequenceAsync(ct);
    }

    public async UniTask ExitAsync(CancellationToken ct){}
}

public class GameManagerStateGameClear : IGameManagerState
{
    protected GameManager _gameManager;
    public async UniTask EnterAsync(GameManager gameManager, CancellationToken ct)
    {
        _gameManager = gameManager;
        Debug.Log("GameManagerStateGameClear.EnterAsync");
        _gameManager.SetPlayerPhysicsEnabled(false);
        await _gameManager.GameClearSequenceAsync(ct);
    }

    public async UniTask ExitAsync(CancellationToken ct){}
}
