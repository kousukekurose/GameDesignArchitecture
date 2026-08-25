using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }
    
    private IPlayerState _currentState;
    public IPlayerState CurrentState => _currentState;

    [Header("移動・ジャンプ設定")]
    [SerializeField] public float _moveSpeed = 5.0f;
    [SerializeField] private float _currentSpeed = 10f;
    public float _jumpForce { get; private set; } = 5.0f;
    public float _enemyBoundForce { get; private set; } = 5.0f;
    public float _groundCheckOffset { get; private set; } = 0.1f;

    [Header("プレイヤーのステータス")]
    [SerializeField] private int _playerHp = 3;
    // 横から敵にぶつかったときのダメージ量
    [SerializeField] private int _sideDamageAmount = 1; 
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

    void Start()
    {
        _currentHp = _playerHp;
        _rd = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<Collider2D>();
        _groundLayer = LayerMask.GetMask("Ground");
        _enemyLayer = LayerMask.GetMask("Enemy");
        
        _isGround = true;
        _hasStomped = false;
        _jumpCount = 0; 

        // 初期ステート（アイドル）を設定してゲーム開始
        ChangeState(new PlayerStateIdel(this));
    }

    void FixedUpdate()
    {
        _currentState?.FixedUpdate();
    }

    void Update()
    {
        _currentState?.Update();
        CheckSideCollisions();
    }

    public void ChangeState(IPlayerState _playerState)
    {
        if (_currentState != null) _currentState?.Exit();
        _currentState = _playerState;
        _currentState?.Enter();
    }

    // 左右の当たり判定
    public void CheckSideCollisions()
    {
        // すでに死んでいる、または無敵状態ならダメージ判定を完全にスルーする
        if (_currentHp <= 0 || _currentState is PlayerStateDie) return;
        if (PlayerVisual.Instance._isInvincible) return;

        Vector2 _startPos = _collider2D.bounds.center;
        Vector2 _liftPos = new Vector2(_startPos.x - _groundCheckOffset, _startPos.y);
        Vector2 _rithPos = new Vector2(_startPos.x + _groundCheckOffset, _startPos.y);
        
        RaycastHit2D _tochingLiftEnemy = Physics2D.Linecast(_startPos, _liftPos, _enemyLayer);
        RaycastHit2D _tochingRithEnemy = Physics2D.Linecast(_startPos, _rithPos, _enemyLayer);
        
        Debug.DrawLine(_startPos, _liftPos, Color.blue);
        Debug.DrawLine(_startPos, _rithPos, Color.yellow);

        if (_tochingLiftEnemy.collider != null || _tochingRithEnemy.collider != null)
        {
            Damage(_sideDamageAmount);
        }
    }

    private void Damage(int _damage)
    {
        _currentHp -= _damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0; 
            ChangeState(new PlayerStateDie(this));
        }
        else
        {
            PlayerVisual.Instance.StartInvincibleFlash();
        }
    }
}
