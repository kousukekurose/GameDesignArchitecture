using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

public class EnemyController : MonoBehaviour, IEnemyController
{
    private Enemy _enemy;

    [SerializeField] private EnemyVisual visual;
    [SerializeField] private Rigidbody2D rb2d;
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
        if (IsObstacleOrCliffAhead())
        {
            _isFacingRight = !_isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;

            float forceSpeed = _isFacingRight ? _enemy.Speed : -_enemy.Speed;
            rb2d.linearVelocity = new Vector2(forceSpeed, rb2d.linearVelocity.y);

            await UniTask.Delay(System.TimeSpan.FromSeconds(0.2f), cancellationToken: ct);
            return;
        }

        float currentSpeed = _isFacingRight ? _enemy.Speed : -_enemy.Speed;
        rb2d.linearVelocity = new Vector2(currentSpeed, rb2d.linearVelocity.y);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
