using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Tilemaps;
using R3;
using Unity.VisualScripting;

#region 地上（ベース・アイドル・移動）
// ==================================================================================

public abstract class PlayerStateGroundBase : IPlayerState
{
    protected Player _player;
    protected CompositeDisposable _disposables = new();

    protected PlayerStateGroundBase(Player player)
    {
        _player = player;
    }

    // 部屋に「入った瞬間」に1回だけやる仕事
    public virtual async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        _player = player;
        _player._isGround = true;

        Observable.EveryUpdate(ct)
        .Select(_ => CheckTouchingGround())
        .Where(touching => !touching)
        .Subscribe(_ => _player.ChangeState(new PlayerStateFall(_player)))
        .AddTo(_disposables);

        if(PlayerController.Instance != null)
        {
            PlayerController.Instance.MoveInput
            .Subscribe(moveInput =>
            {
                if(Mathf.Abs(moveInput.x) > 0.01f)
                {
                    if(_player.CurrentState is PlayerStateIdle)_player.ChangeState(new PlayerStateMove(_player));
                }
                else
                {
                    if(_player.CurrentState is PlayerStateMove)_player.ChangeState(new PlayerStateIdle(_player));
                }
            }).AddTo(_disposables);
        }

        while(!ct.IsCancellationRequested)
        {
            TickUpdate();
            await UniTask.Yield(PlayerLoopTiming.Update,ct);
        }
    }

    protected virtual void TickUpdate(){}

    private bool CheckTouchingGround()
    {
        Vector2 _startPos = _player._collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x, _player._collider2D.bounds.min.y - _player._groundCheckOffset);
        return Physics2D.Linecast(_startPos, _endPos, _player._groundLayer);
    }


    // 部屋から「出る瞬間」に1回だけやる仕事
    public virtual void Exit()
    {
        //このステート内の監視を全て解除
        _disposables.Dispose();
    }
}

public class PlayerStateIdle : PlayerStateGroundBase
{
    public PlayerStateIdle(Player player) : base(player) { }

    public override async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        _player = player;
        _player._rd.linearVelocity = new Vector2(0f, _player._rd.linearVelocity.y);

        await base.EnterAsync(player,ct);
    }
}

public class PlayerStateMove : PlayerStateGroundBase
{
    public PlayerStateMove(Player player) : base(player) { }

    protected override void TickUpdate()
    {
        if(PlayerController.Instance == null)return;
        float moveX = PlayerController.Instance._moveInput.x;

        //壁激突時
        if((_player.IsTouchingWallLeft && moveX < 0f) || (_player.IsTouchingWallRight && moveX > 0f))
        {
            _player._rd.linearVelocity = new Vector2(0f,_player._rd.linearVelocity.y);
            return;
        }

        //移動速度の適用と反転
        _player._rd.linearVelocity = new Vector2(moveX * _player._currentSpeed,_player._rd.linearVelocity.y);
        if(PlayerVisual.Instance != null) PlayerVisual.Instance.ChangeDirection(moveX);

    }
}
#endregion

#region 空中（ベース・ジャンプ・落下）
// ==================================================================================

public abstract class PlayerStateAirBase : IPlayerState
{
    protected Player _player;
    protected CompositeDisposable _disposables = new();

    protected PlayerStateAirBase(Player player)
    {
        _player = player;
    }

    public virtual async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        _player = player;
        _player._isGround = false;

        while(!ct.IsCancellationRequested)
        {
            TickAirUpdate();
            await UniTask.Yield(PlayerLoopTiming.Update,ct);
        }
    }

    private void TickAirUpdate()
    {
        Vector2 _startPos = _player._collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x, _player._collider2D.bounds.min.y - _player._groundCheckOffset);
        bool touchingGround = Physics2D.Linecast(_startPos, _endPos, _player._groundLayer);
        RaycastHit2D touchingEnemy = Physics2D.Linecast(_startPos, _endPos, _player._enemyLayer);

        Vector2 _headEndPos = new Vector2(_startPos.x, _player._collider2D.bounds.max.y + _player._groundCheckOffset);
        RaycastHit2D _hittingBlock = Physics2D.Linecast(_startPos, _headEndPos, _player._groundLayer);

        // 1. 敵踏みつけ
        if (_player._rd.linearVelocity.y <= 0f && touchingEnemy && !_player._hasStomped)
        {
            _player.enemyCount++;
            _player._hasStomped = true;
            _player._jumpCount = 1;
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, _player._enemyBoundForce);
            _player.ChangeState(new PlayerStateJump(_player));
            return;
        }

        // 2. 着地
        if (_player._rd.linearVelocity.y <= 0f && !_player._isGround && touchingGround)
        {
            _player._jumpCount = 0;
            _player._isGround = true;

            float moveX = PlayerController.Instance != null ? PlayerController.Instance._moveInput.x : 0f;
            if (Mathf.Abs(moveX) > 0.01f) _player.ChangeState(new PlayerStateMove(_player));
            else _player.ChangeState(new PlayerStateIdle(_player));
            return;
        }

        // 3. ブロック破壊
        if (_player._rd.linearVelocity.y > 0f && _hittingBlock.collider != null)
        {
            Tilemap _tilemap = _hittingBlock.collider.GetComponent<Tilemap>();
            if (_tilemap != null)
            {
                Vector3Int _cellPosition = _tilemap.WorldToCell(_headEndPos);
                _tilemap.SetTile(_cellPosition, null);
            }
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, 0f);
        }

        ProcessAirMovement();
    }

    private void ProcessAirMovement()
    {
        if (PlayerController.Instance == null) return;
        float moveX = PlayerController.Instance._moveInput.x;

        if ((_player.IsTouchingWallLeft && moveX < 0f) || (_player.IsTouchingWallRight && moveX > 0f))
        {
            _player._rd.linearVelocity = new Vector2(0f, _player._rd.linearVelocity.y);
            return;
        }
        
        float _targetXVelocity = _player._rd.linearVelocity.x;
        if (moveX != 0f) _targetXVelocity = moveX * _player._currentSpeed;
        
        _player._rd.linearVelocity = new Vector2(_targetXVelocity, _player._rd.linearVelocity.y);
        
        if (PlayerVisual.Instance != null) PlayerVisual.Instance.ChangeDirection(moveX);
    }

    public virtual void Exit()
    {
        _disposables.Dispose();
    }
}

public class PlayerStateJump : PlayerStateAirBase
{
    public PlayerStateJump(Player player) : base(player) { }

    public override async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        _player = player;

        if (_player._jumpCount < 2)
        {
            _player._isGround = false;
            _player._jumpCount++;
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, _player._jumpForce);
        }
        await base.EnterAsync(player,ct);
    }
}

public class PlayerStateFall : PlayerStateAirBase
{
    public PlayerStateFall(Player player) : base(player) { }

    public override async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        _player = player;
        if (_player._jumpCount == 0) _player._jumpCount = 1;
        await base.EnterAsync(player,ct);
    }
}
#endregion

#region 死亡ステート
// ==================================================================================
public class PlayerStateDie : IPlayerState
{
    private Player _player;
    public PlayerStateDie(Player player) { _player = player; }

    public async UniTask EnterAsync(Player player,CancellationToken ct)
    {
        player = _player;
        player._collider2D.isTrigger = true;
        if (PlayerVisual.Instance != null)
        {
            PlayerVisual.Instance.PlayDieAnimation(_player._rd, _player._jumpForce);
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(2),cancellationToken: ct);
        //Object.Destroy(_player.gameObject);
    }
    public void Exit() { }
}
#endregion
