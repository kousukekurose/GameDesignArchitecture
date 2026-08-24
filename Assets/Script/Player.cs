using UnityEngine.Tilemaps;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance{get; private set;}
    private IPlayerState _currentState;

    //PlayerState _playerState = PlayerState.Idle;
    
    [SerializeField] public float _moveSpeed = 5.0f;
    [SerializeField] private float _currentSpeed = 10f;
    [SerializeField] private float _jumpForce = 5.0f;
    [SerializeField] private float _enemyBoundForce = 5.0f;
    [SerializeField] private float _groundCheckOffset = 0.1f;

    public int enemyCount {get; private set;}

    // 💡【追加①】今、通算で何回ジャンプしたかを数えるカウンター
    private int _jumpCount = 0;

    //private Vector2 _moveInput;
    private bool _isGround = false;
    public bool _hasStomped = false;

    private Collider2D _collider2D;
    public Rigidbody2D _rd {get; private set;}
    private LayerMask _groundLayer;
    private LayerMask _enemyLayer;

    [SerializeField] private int _playerHp = 3;
    public int _currentHp{get; private set;}

    private void Awake()
    {
        if(Instance == null)
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
        Debug.Log(_currentHp);
        _rd = GetComponent<Rigidbody2D>();
        _collider2D = GetComponent<Collider2D>();
        _groundLayer = LayerMask.GetMask("Ground");
        _enemyLayer = LayerMask.GetMask("Enemy");
        _isGround = true;
        _hasStomped = false;
        _jumpCount = 0; // 💡最初は0回
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
        //今の部屋を出る瞬間の挨拶（Exit）を実行
        if (_currentState != null) _currentState?.Exit();
        _currentState = _playerState;
        //新しい部屋に入る瞬間(Enter)を実行
        _currentState?.Enter();
    }

    public void Move()
    {
        if(!_isGround)return;
        _rd.linearVelocity = new Vector2(PlayerController.Instance._moveInput.x * _moveSpeed, _rd.linearVelocity.y);
        //ウォーク中の向きを変える仕組み
        PlayerVisual.Instance.ChangeDirection(PlayerController.Instance._moveInput.x);
    }

    public void Jump()
    {
        if (_jumpCount < 2)
        {
            _isGround = false;
            // ジャンプしたので、カウンターを1増やす（1回目 ➔ 2回目になる）
            _jumpCount++; 
             // 上へ飛び立つ
             // 1回目でも2回目（空中ジャンプ）でも、上への初速をガツンとリセットして与える
            _rd.linearVelocity = new Vector2(_rd.linearVelocity.x, _jumpForce);
            ChangeState(new PlayerStateJump(this));
        }
    }

    public void OnLand()
    {
        //足元の判定
        Vector2 _startPos = _collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x,_collider2D.bounds.min.y - _groundCheckOffset);
        bool tochingGronud = Physics2D.Linecast(_startPos,_endPos,_groundLayer);
        RaycastHit2D tochingEnemy = Physics2D.Linecast(_startPos,_endPos,_enemyLayer);
        Debug.DrawLine(_startPos, _endPos, Color.red);

        //頭の判定
        Vector2 _headEndPos = new Vector2(_startPos.x,_collider2D.bounds.max.y + _groundCheckOffset);
        RaycastHit2D _hittingBlock = Physics2D.Linecast(_startPos, _headEndPos, _groundLayer);
        Debug.DrawLine(_startPos, _headEndPos, Color.blue);

        if (_rd.linearVelocity.y > 0f && _hittingBlock.collider != null)
        {
            Debug.Log("ブロックを下から叩いた！");

            // 1. 当たった相手から「Tilemapコンポーネント」をガシッと取得する
            Tilemap _tilemap = _hittingBlock.collider.GetComponent<Tilemap>();

            if (_tilemap != null)
            {
                 // 2. レーザーが当たった世界の本物の座標（point）を取り出す
                // ※頭上センサーの線の先端（_headEndPos）の座標を使うと、より確実にマスの中心を捉えられます
                Vector3 _hitWorldPos = _headEndPos;

                 // 3. 翻訳機を使って、世界の本物の座標を「マス目の住所（Vector3Int）」に一瞬で変換！
                Vector3Int _cellPosition = _tilemap.WorldToCell(_hitWorldPos);

                // 4. そのマス目の住所にあるタイルを「null（空っぽ）」にして消去する！
                _tilemap.SetTile(_cellPosition, null);
            }
            // 頭をぶつけたので、上への勢いをピタッと止めて落下させる（マリオの挙動）
            _rd.linearVelocity = new Vector2(_rd.linearVelocity.x, 0f);
        }

        // 地面の場合
        if(tochingGronud && _rd.linearVelocity.y <= 0f && !_isGround)
        {
            Debug.Log("地面に着地");
            
            // 💡【追加③】無事に地面に着地したので、ジャンプ回数を「0」にリセットする！
            _jumpCount = 0; 
            if(PlayerController.Instance._moveInput != Vector2.zero)
            {
                ChangeState(new PlayerStateMove(this));
                _isGround = true;
            }
            else
            {
                ChangeState(new PlayerStateIdel(this));
                _isGround = true;
            }
        }
        // 敵だった場合
        else if(_rd.linearVelocity.y <= 0f && tochingEnemy.collider && !_hasStomped)
        {
            Debug.Log("敵にあたった");
            enemyCount++;
            Debug.Log(enemyCount);
            _hasStomped = true;
            
            // 💡敵を踏みつけたときも、空中ジャンプの権利を「1回分」回復させてあげると、マリオ風でさらに気持ちよくなります！
            if (_jumpCount > 1) _jumpCount = 1;

            _rd.linearVelocity = new Vector2(_rd.linearVelocity.x,_enemyBoundForce);
            Destroy(tochingEnemy.collider.gameObject);
        }
    }

    //左右の当たり判定
    public void CheckSideCollisions()
    {
        if(PlayerVisual.Instance._isInvincible)return;
        Vector2 _startPos = _collider2D.bounds.center;
        Vector2 _liftPos = new Vector2(_startPos.x - _groundCheckOffset,_startPos.y);
        Vector2 _rithPos = new Vector2(_startPos.x + _groundCheckOffset,_startPos.y);
        RaycastHit2D _tochingLiftEnemy = Physics2D.Linecast(_startPos,_liftPos,_enemyLayer);
        RaycastHit2D _tochingRithEnemy = Physics2D.Linecast(_startPos,_rithPos,_enemyLayer);
        Debug.DrawLine(_startPos, _liftPos, Color.blue);
        Debug.DrawLine(_startPos,_rithPos,Color.yellow);
        if(_currentHp == 0)return;
        if(_tochingLiftEnemy.collider != null || _tochingRithEnemy.collider != null)
        {
            Debug.Log("的に当たった");
            //仮のダメージをパラメータに追加
            Damage(1);
        }
    }

    private void Damage(int _damage)
    {
        _currentHp -= _damage;
        Debug.Log(_currentHp);
        if(_currentHp == 0)
        {
            ChangeState(new PlayerStateDie(this));
        }
        else
        {
            Debug.Log("無敵開始");
            PlayerVisual.Instance.StartInvincibleFlash();
        }
    }

    public void Die()
    {
        _collider2D.isTrigger = true;
        PlayerVisual.Instance.PlayDieAnimation(_rd,_jumpForce);
    }
}
