using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;
using System.Threading;

public class Player : MonoBehaviour, IDamageable,IAttacker
{
    public static Player Instance { get; private set; }
    
    private IPlayerState _currentState;
    public IPlayerState CurrentState => _currentState;

    private static readonly Subject<Unit> _visual = new();
    public static Observable<Unit> Visual => _visual;

    [Header("移動・ジャンプ設定")]
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private float _sprintSpeed = 10f;
    [SerializeField] private float _jumpForce = 5.0f;
    public float _currentSpeed;
    public bool _isSprint { get; set; }
    public float JumpForce { get=> _jumpForce; private set => _jumpForce = value; }
    public float _enemyBoundForce { get; private set; } = 5.0f;
    public float _groundCheckOffset { get; private set; } = 0.1f;
    public int _attackPower {get; private set;} = 1;
    public int _DamageAmount => _attackPower;
    public bool IsTouchingWallLeft { get; private set; }
    public bool IsTouchingWallRight { get; private set; }

    [Header("プレイヤーのステータス")]
    [SerializeField] private int _playerHp = 3;
    public int _currentHp { get; private set; }
    public int enemyCount { get; set; }
    public int _jumpCount { get; set; } = 0;

    [Header("状態フラグ")]
    public bool _isGround { get; set; } = false;
    public bool _hasStomped { get; set; } = false;

    // コンポーネント・レイヤー設定
    public Collider2D _collider2D { get; private set; }
    public Rigidbody2D _rd { get; private set; }
    public LayerMask _groundLayer { get; private set; }
    public LayerMask _enemyLayer { get; private set; }
    public LayerMask _nullEnemyLayer { get; private set; }
    public Animator _animator { get; private set; }

    private CancellationTokenSource _stateCts;
    private CompositeDisposable _disposables;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        _isSprint = false;
        _currentHp = _playerHp;
        _rd = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _collider2D = GetComponent<Collider2D>();
        _groundLayer = LayerMask.GetMask("Ground");
        _enemyLayer = LayerMask.GetMask("Enemy");
        _nullEnemyLayer = LayerMask.GetMask("null");
        _disposables = new CompositeDisposable();
        _isGround = true;
        _hasStomped = false;
        _jumpCount = 0; 

        Observable.EveryUpdate(this.destroyCancellationToken)
        .Subscribe(_ => CheckSideCollisions())
        .AddTo(_disposables);

        ChangeState(new PlayerStateIdle(this));
    }

    private void Update()
    {
        _currentSpeed = _isSprint ? _sprintSpeed : _moveSpeed;
    }

    //ステートの切り替え
    public void ChangeState(IPlayerState _playerState)
    {
        _stateCts?.Cancel();
        _stateCts?.Dispose();
        _currentState?.Exit();

        _stateCts = new CancellationTokenSource();
        _currentState = _playerState;

        _RunStateAsync(_playerState,_stateCts.Token).Forget();
    }

    private async UniTaskVoid _RunStateAsync(IPlayerState newState, CancellationToken ct)
    {
        try
        {
            if(newState != null)
            {
                await newState.EnterAsync(this,ct);
            }
        }
        catch(System.OperationCanceledException){}
    }

    // 左右の当たり判定
    public void CheckSideCollisions()
    {
        if (_currentHp <= 0 || _currentState is PlayerStateDie) return;
        if (PlayerVisual.Instance != null && PlayerVisual.Instance._isInvincible) return;

        Vector2 _startPos = _collider2D.bounds.center;
        
        Vector2 _enemyLeftPos = new Vector2(_collider2D.bounds.min.x - _groundCheckOffset, _startPos.y);
        Vector2 _enemyRightPos = new Vector2(_collider2D.bounds.max.x + _groundCheckOffset, _startPos.y);
        
        RaycastHit2D _touchingLeftEnemy = Physics2D.Linecast(_startPos, _enemyLeftPos, _enemyLayer);
        RaycastHit2D _touchingRightEnemy = Physics2D.Linecast(_startPos, _enemyRightPos, _enemyLayer);
        
        Vector2 _groundLeftPos = new Vector2(_collider2D.bounds.min.x - _groundCheckOffset, _startPos.y);
        Vector2 _groundRightPos = new Vector2(_collider2D.bounds.max.x + _groundCheckOffset, _startPos.y);
        
        IsTouchingWallLeft = Physics2D.Linecast(new Vector2(_collider2D.bounds.min.x, _startPos.y), _groundLeftPos, _groundLayer);
        IsTouchingWallRight = Physics2D.Linecast(new Vector2(_collider2D.bounds.max.x, _startPos.y), _groundRightPos, _groundLayer);
        
        Debug.DrawLine(_startPos, _enemyLeftPos, Color.blue);
        Debug.DrawLine(_startPos, _enemyRightPos, Color.yellow);

        Collider2D _hitCollider = _touchingRightEnemy.collider ?? _touchingLeftEnemy.collider;

        if (_hitCollider != null &&  _hitCollider.TryGetComponent<IAttacker>(out var attacker))
        {
            TakeDamage(attacker._DamageAmount);
        }
    }

    public void TakeDamage(int _damage)
    {
        // 死亡時や無敵時はダメージを重ねて受けない防衛コード
        if (_currentHp <= 0 || (_currentState is PlayerStateDie)) return;
        if (PlayerVisual.Instance != null && PlayerVisual.Instance._isInvincible) return;

        _currentHp -= _damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0; 
            ChangeState(new PlayerStateDie(this));
        }
        else
        {
            if (PlayerVisual.Instance != null)
            {
                //PlayerVisual.Instance.StartInvincibleFlash();
                _visual.OnNext(Unit.Default);
            }
        }
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
        _stateCts?.Cancel();
        _stateCts?.Dispose();
        _disposables?.Dispose();
    }
}
