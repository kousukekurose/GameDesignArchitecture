using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerState : MonoBehaviour { }

#region 地上（ベース・アイドル・移動）
// ==================================================================================

public abstract class PlayerStateGroundBase : IPlayerState
{
    protected Player _player;

    public PlayerStateGroundBase(Player player)
    {
        _player = player;
    }

    public virtual void Enter()
    {
        _player._isGround = true;
    }

    public virtual void Update()
    {
        // 足元の接地判定
        Vector2 _startPos = _player._collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x, _player._collider2D.bounds.min.y - _player._groundCheckOffset);
        bool tochingGronud = Physics2D.Linecast(_startPos, _endPos, _player._groundLayer);

        // 地面から離れたら自動で「落下状態」へ移行
        if (!tochingGronud)
        {
            _player.ChangeState(new PlayerStateFall(_player));
            return;
        }

        // 入力の有無に応じて自動でIdelとMoveを切り替える（ガード付き）
        float moveX = PlayerController.Instance._moveInput.x;
        if (moveX != 0f)
        {
            if (_player.CurrentState is not PlayerStateMove)
            {
                _player.ChangeState(new PlayerStateMove(_player));
            }
        }
        else
        {
            if (_player.CurrentState is not PlayerStateIdel)
            {
                _player.ChangeState(new PlayerStateIdel(_player));
            }
        }
    }

    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
}

public class PlayerStateIdel : PlayerStateGroundBase
{
    public PlayerStateIdel(Player player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        // アイドル時は横移動速度を完全にゼロにする
        _player._rd.linearVelocity = new Vector2(0f, _player._rd.linearVelocity.y);
    }
}

public class PlayerStateMove : PlayerStateGroundBase
{
    public PlayerStateMove(Player player) : base(player) { }

    public override void Update()
    {
        base.Update();
        if ((_player.IsTouchingWallLeft && _player._currentSpeed < 0f) || (_player.IsTouchingWallRight && _player._currentSpeed > 0f))
        {
            _player._currentSpeed = 0f;
        }
        // 移動速度の上書きと向きの変更
        _player._rd.linearVelocity = new Vector2(PlayerController.Instance._moveInput.x * _player._currentSpeed, _player._rd.linearVelocity.y);
        PlayerVisual.Instance.ChangeDirection(PlayerController.Instance._moveInput.x);
    }
}

// ==================================================================================
#endregion

#region 空中（ベース・ジャンプ・落下）
// ==================================================================================

public abstract class PlayerStateAirBase : IPlayerState
{
    protected Player _player;

    public PlayerStateAirBase(Player player)
    {
        _player = player;
    }

    public virtual void Enter()
    {
        _player._isGround = false;
    }

    public virtual void Update()
    {
        // 足元と敵のセンサー判定
        Vector2 _startPos = _player._collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x, _player._collider2D.bounds.min.y - _player._groundCheckOffset);
        bool tochingGronud = Physics2D.Linecast(_startPos, _endPos, _player._groundLayer);
        RaycastHit2D tochingEnemy = Physics2D.Linecast(_startPos, _endPos, _player._enemyLayer);
        Debug.DrawLine(_startPos, _endPos, Color.red);

        // 頭上のブロックセンサー判定
        Vector2 _headEndPos = new Vector2(_startPos.x, _player._collider2D.bounds.max.y + _player._groundCheckOffset);
        RaycastHit2D _hittingBlock = Physics2D.Linecast(_startPos, _headEndPos, _player._groundLayer);
        Debug.DrawLine(_startPos, _headEndPos, Color.blue);

        // 1. 着地判定：敵（最優先）
        if (_player._rd.linearVelocity.y <= 0f && tochingEnemy && !_player._hasStomped)
        {
            _player.enemyCount++;
            _player._hasStomped = true;
            _player._jumpCount = 1; // 空中ジャンプ回数を1回復

            // 踏みつけ時の跳ね返り速度を与える
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, _player._enemyBoundForce);
            
            _player.ChangeState(new PlayerStateJump(_player));
            return;
        }

        // 2. 着地判定：地面
        if (_player._rd.linearVelocity.y <= 0f && !_player._isGround && tochingGronud)
        {
            _player._jumpCount = 0;
            _player._isGround = true;

            if (PlayerController.Instance._moveInput != Vector2.zero)
            {
                _player.ChangeState(new PlayerStateMove(_player));
            }
            else
            {
                _player.ChangeState(new PlayerStateIdel(_player));
            }
            return;
        }

        // 3. 頭上のブロック破壊判定
        if (_player._rd.linearVelocity.y > 0f && _hittingBlock.collider != null)
        {
            Tilemap _tilemap = _hittingBlock.collider.GetComponent<Tilemap>();
            if (_tilemap != null)
            {
                Vector3 _hitWorldPos = _headEndPos;
                Vector3Int _cellPosition = _tilemap.WorldToCell(_hitWorldPos);
                _tilemap.SetTile(_cellPosition, null);
            }
            // 上向きの勢いを止めて落下に転じさせる
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, 0f);
        }
    }

    public virtual void FixedUpdate()
    {
        if ((_player.IsTouchingWallLeft && _player._currentSpeed < 0f) || (_player.IsTouchingWallRight && _player._currentSpeed > 0f))
        {
            Debug.Log("空中壁激突");
            _player._currentSpeed = 0f;
        }
        // 空中での横移動の慣性制御
        float _targetXVelocity = _player._rd.linearVelocity.x;
        if (PlayerController.Instance._moveInput.x != 0f)
        {
            _targetXVelocity = PlayerController.Instance._moveInput.x * _player._currentSpeed;
        }
        
        Vector2 _airVelocity = new Vector2(_targetXVelocity, _player._rd.linearVelocity.y);
        _player._rd.linearVelocity = _airVelocity;
        
        PlayerVisual.Instance.ChangeDirection(PlayerController.Instance._moveInput.x);
    }

    public virtual void Exit() { }
}

public class PlayerStateJump : PlayerStateAirBase
{
    public PlayerStateJump(Player player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        
        // 2回（2段ジャンプ）までの制限付きで上方向へ初速を与える
        if (_player._jumpCount < 2)
        {
            _player._isGround = false;
            _player._jumpCount++;
            _player._rd.linearVelocity = new Vector2(_player._rd.linearVelocity.x, _player._jumpForce);
        }
    }
}

public class PlayerStateFall : PlayerStateAirBase
{
    public PlayerStateFall(Player player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        // 崖からそのまま落ちた場合、通常ジャンプを1回消費した扱いにすることで空中2段ジャンプを可能にする
        if (_player._jumpCount == 0)
        {
            _player._jumpCount = 1;
        }
    }
}

// ==================================================================================
#endregion

#region 死亡ステート
// ==================================================================================

public class PlayerStateDie : IPlayerState
{
    private Player _player;

    public PlayerStateDie(Player player)
    {
        _player = player;
    }

    public void Enter()
    {
        _player._collider2D.isTrigger = true; // 他のコライダーをすり抜けて落ちるようにする
        PlayerVisual.Instance.PlayDieAnimation(_player._rd, _player._jumpForce);
    }

    public void Update() { }
    public void FixedUpdate() { }
    public void Exit() { }
}

// ==================================================================================
#endregion
