using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public class EnemyController : MonoBehaviour, IEnemyController
{
    private Enemy _enemy;

    [SerializeField] private EnemyVisual visual;
    //[SerializeField] private Rigidbody2D rb2d;
    [SerializeField] private Transform eyesLocation;      
    [SerializeField] private Transform groundCheckPoint;   

    public Enemy Enemy => _enemy;
    public EnemyVisual Visual => visual;

    private IEnemyState _currentState;
    private CancellationTokenSource _cts;
    private bool _isFacingRight = true;

    private void Start()
    {
        _cts = new CancellationTokenSource();

        _enemy = GetComponent<Enemy>();
        if (_enemy == null)
        {
            Debug.LogError($"{gameObject.name} に Enemy スクリプトが見つかりません！");
            return;
        }

        _enemy.Hp
            .Where(hp => hp <= 0)
            .Subscribe(_ => ChangeState(new DeadState())) 
            .AddTo(this);

        ChangeState(new PatrolState());
        StateLoopAsync(_cts.Token).Forget();
    }

    private async UniTaskVoid StateLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (_currentState != null)
                {
                    await _currentState.UpdateAsync(this, ct);
                }
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    public void ChangeState(IEnemyState newState)
    {
        _ChangeStateAsync(newState, _cts.Token).Forget();
    }

    private async UniTaskVoid _ChangeStateAsync(IEnemyState newState, CancellationToken ct)
    {
        if (_currentState != null) await _currentState.ExitAsync(ct);
        _currentState = newState;
        if (_currentState != null) await _currentState.EnterAsync(this, ct);
    }

    public bool IsObstacleOrCliffAhead()
    {
        Vector2 moveDirection = _isFacingRight ? Vector2.right : Vector2.left;
        int groundLayer = LayerMask.GetMask("Ground");

        RaycastHit2D wallHit = Physics2D.Raycast(eyesLocation.position, moveDirection, _enemy.Data.WallCheckDistance, groundLayer);
        if (wallHit.collider != null) return true;

        RaycastHit2D groundHit = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, _enemy.Data.CliffCheckDistance, groundLayer);
        if (groundHit.collider == null) return true;

        return false;
    }

    public async UniTask MovePatrolAsync(CancellationToken ct)
    {
        HitPlayer();
        if (IsObstacleOrCliffAhead())
        {
            Debug.Log("確認");
            _isFacingRight = !_isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;

            float forceSpeed = _isFacingRight ? _enemy.Speed : -_enemy.Speed;
            _enemy._rd.linearVelocity = new Vector2(forceSpeed, _enemy._rd.linearVelocity.y);

            await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
            return;
        }

        float currentSpeed = _isFacingRight ? _enemy.Speed : -_enemy.Speed;
        _enemy._rd.linearVelocity = new Vector2(currentSpeed, _enemy._rd.linearVelocity.y);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }

        public void HitPlayer()
    {
        // 1. 敵のボックスコライダーのサイズと位置を取得
        Vector2 boxSize = _enemy._collider2D.bounds.size;
        Vector2 boxCenter = _enemy._collider2D.bounds.center;
        float topY = _enemy._collider2D.bounds.max.y;

        // 2. センサーの「横幅」と「縦の厚み（高さ）」を決める
        // 横幅は敵のコライダーと同じにし、厚み（playerCheck）のぶんだけ頭上にセンサーを展開する
        Vector2 sensorSize = new Vector2(boxSize.x, _enemy._playerCheck);
        
        // センサーの配置中心点（頭のてっぺんから、厚みの半分だけ上に浮かせた位置）
        Vector2 sensorCenter = new Vector2(boxCenter.x, topY + (_enemy._playerCheck * 0.5f));

        float halfW = sensorSize.x * 0.5f;
        float halfH = sensorSize.y * 0.5f;
        Vector2 topLeft  = sensorCenter + new Vector2(-halfW,  halfH);
        Vector2 topRight = sensorCenter + new Vector2( halfW,  halfH);
        Vector2 botLeft  = sensorCenter + new Vector2(-halfW, -halfH);
        Vector2 botRight = sensorCenter + new Vector2( halfW, -halfH);

        // プレイヤーが当たっていれば赤、いなければ緑で「四角い枠」を画面に表示する
        RaycastHit2D _touchingPlayer = Physics2D.BoxCast(sensorCenter, sensorSize, 0f, Vector2.zero, 0f, _enemy._playerLayer);
        Color boxColor = _touchingPlayer.collider != null ? Color.red : Color.green;

        Debug.DrawLine(topLeft, topRight, boxColor);
        Debug.DrawLine(topRight, botRight, boxColor);
        Debug.DrawLine(botRight, botLeft, boxColor);
        Debug.DrawLine(botLeft, topLeft, boxColor);

        if (_touchingPlayer.collider != null)
        {
            IAttacker _attacker = _touchingPlayer.collider.GetComponent<IAttacker>();
            if (_attacker == null && _touchingPlayer.collider.transform.parent)
            {
                _attacker = _touchingPlayer.collider.transform.parent.GetComponent<IAttacker>();
            }

            if (_attacker != null)
            {
                float playerBottomY = _touchingPlayer.collider.bounds.min.y;
                float hitPointY = _touchingPlayer.point.y;
                
                if (hitPointY == 0 && _touchingPlayer.point.x == 0) hitPointY = sensorCenter.y - halfH;

                // 横から歩いてぶつかった時だけを排除するための「位置の縛り（足元か）」だけを残す
                bool isStepping = (hitPointY <= playerBottomY + 0.25f);

                if (isStepping)
                {
                    _enemy.TakeDamage(_attacker._DamageAmount);
                }
            }
        }
    }


    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
