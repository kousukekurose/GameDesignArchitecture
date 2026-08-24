using UnityEngine.Tilemaps;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public static Player Instance{get; private set;}
    private enum PlayerState
    {
        Idle,
        Move,
        Jump,
        Attack,
        Die
    }

    PlayerState _playerState = PlayerState.Idle;
    
    [SerializeField] private float _moveSpeed = 5.0f;
    [SerializeField] private float _currentSpeed = 10f;
    [SerializeField] private float _jumpForce = 5.0f;
    [SerializeField] private float _enemyBoundForce = 5.0f;
    [SerializeField] private float _groundCheckOffset = 0.1f;

    public int enemyCount {get; private set;}

    // 💡【追加①】今、通算で何回ジャンプしたかを数えるカウンター
    private int _jumpCount = 0;

    private Vector2 _moveInput;
    private bool _isGround = false;
    private bool _hasStomped = false;

    private Collider2D _collider2D;
    private Rigidbody2D _rd;
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
    }

    void FixedUpdate()
    {
        if(_playerState == PlayerState.Jump)
        {
            float _targetXVelocity = _rd.linearVelocity.x;
            if(_moveInput.x != 0f)
            {
                _targetXVelocity = _moveInput.x * _moveSpeed;
            }
            Vector2 _airVelocity = new Vector2(_targetXVelocity,_rd.linearVelocity.y);
            _rd.linearVelocity = _airVelocity;
            //ジャンプ中プレイヤーの向きを変える仕組み
            PlayerVisual.Instance.ChangeDirection(_moveInput.x);
        }
    }

    void Update()
    {
        switch(_playerState)
        {
            case PlayerState.Idle:
                Debug.Log("アイドル中");
                _rd.linearVelocity = new Vector2(0, _rd.linearVelocity.y);
                break;
            case PlayerState.Move:
                Debug.Log("移動中");
                Move();
                break;
            case PlayerState.Jump:
                Debug.Log("ジャンプ");
                OnLand();
                break;
            case PlayerState.Die:
                break;
        }
        CheckSideCollisions();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if(_playerState == PlayerState.Die)return;
        _moveInput = context.ReadValue<Vector2>();
        if(context.started && _isGround)
        {
            _playerState = PlayerState.Move;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(_playerState == PlayerState.Die)return;
        if(context.started)
        {
            // 💡【追加②：最重要の条件式】
            // 今までのジャンプ回数（_jumpCount）が「2回未満」のときだけ、中身を実行する！
            if (_jumpCount < 2)
            {
                _isGround = false;
                _playerState = PlayerState.Jump;
                
                // ジャンプしたので、カウンターを1増やす（1回目 ➔ 2回目になる）
                _jumpCount++; 
                
                Jump(); // 上へ飛び立つ
            }
        }
        if(context.canceled)
        {
            _hasStomped = false;
        }
    }

    private void Move()
    {
        _rd.linearVelocity = new Vector2(_moveInput.x * _moveSpeed, _rd.linearVelocity.y);
        //ウォーク中の向きを変える仕組み
        PlayerVisual.Instance.ChangeDirection(_moveInput.x);

        if(_moveInput == Vector2.zero)
        {
            _playerState = PlayerState.Idle;
        }
    }

    private void Jump()
    {
        // 1回目でも2回目（空中ジャンプ）でも、上への初速をガツンとリセットして与える
        _rd.linearVelocity = new Vector2(_rd.linearVelocity.x, _jumpForce);
    }

    private void OnLand()
    {
        //足元の判定
        Vector2 _startPos = _collider2D.bounds.center;
        Vector2 _endPos = new Vector2(_startPos.x,_collider2D.bounds.min.y - _groundCheckOffset);
        bool tochingGronud = Physics2D.Linecast(_startPos,_endPos,_groundLayer);
        RaycastHit2D tochingEnemy = Physics2D.Linecast(_startPos,_endPos,_enemyLayer);
        Debug.DrawLine(_startPos, _endPos, Color.red);

        //頭の判定
        Vector2 _headEndPos = new Vector2(_startPos.x,_collider2D.bounds.max.y + _groundCheckOffset);
        RaycastHit2D hittingBlock = Physics2D.Linecast(_startPos, _headEndPos, _groundLayer);
        Debug.DrawLine(_startPos, _headEndPos, Color.blue);

        if (_rd.linearVelocity.y > 0f && hittingBlock.collider != null)
        {
            Debug.Log("ブロックを下から叩いた！");

            // 1. 当たった相手から「Tilemapコンポーネント」をガシッと取得する
            Tilemap tilemap = hittingBlock.collider.GetComponent<Tilemap>();

            if (tilemap != null)
            {
                 // 2. レーザーが当たった世界の本物の座標（point）を取り出す
                // ※頭上センサーの線の先端（_headEndPos）の座標を使うと、より確実にマスの中心を捉えられます
                Vector3 hitWorldPos = _headEndPos;

                 // 3. 翻訳機を使って、世界の本物の座標を「マス目の住所（Vector3Int）」に一瞬で変換！
                Vector3Int cellPosition = tilemap.WorldToCell(hitWorldPos);

                // 4. そのマス目の住所にあるタイルを「null（空っぽ）」にして消去する！
                tilemap.SetTile(cellPosition, null);
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

            if(_moveInput != Vector2.zero)
            {
                _playerState = PlayerState.Move;
                _isGround = true;
            }
            else
            {
                _playerState = PlayerState.Idle;
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
        RaycastHit2D tochingLiftEnemy = Physics2D.Linecast(_startPos,_liftPos,_enemyLayer);
        RaycastHit2D tochingRithEnemy = Physics2D.Linecast(_startPos,_rithPos,_enemyLayer);
        Debug.DrawLine(_startPos, _liftPos, Color.blue);
        Debug.DrawLine(_startPos,_rithPos,Color.yellow);
        if(_playerState == PlayerState.Die)return;
        if(tochingLiftEnemy.collider != null || tochingRithEnemy.collider != null)
        {
            Debug.Log("的に当たった");
            //仮のダメージをパラメータに追加
            Damage(1);
        }
    }

    private void Damage(int damage)
    {
        _currentHp -= damage;
        Debug.Log(_currentHp);
        if(_currentHp == 0)
        {
            _playerState = PlayerState.Die;
            Die();
        }
        else
        {
            Debug.Log("無敵開始");
            PlayerVisual.Instance.StartInvincibleFlash();
        }
    }

    private void Die()
    {
        _collider2D.isTrigger = true;
        PlayerVisual.Instance.PlayDieAnimation(_rd,_jumpForce);
    }
}
