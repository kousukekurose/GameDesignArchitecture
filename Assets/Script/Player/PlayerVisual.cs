using System.Collections;
using UnityEngine;
using R3;
public class PlayerVisual : MonoBehaviour
{
    public static PlayerVisual Instance { get; private set; }

    private readonly Subject<Unit> _onDeathSubject = new();
    public Observable<Unit> OnDeath => _onDeathSubject;

    private SpriteRenderer _spriteRenderer;

    [Header("方向・サイズ")]
    [SerializeField] private float _playerRotation = 1f;

    [Header("無敵の点滅設定")]
    // 点滅を繰り返す回数
    [SerializeField] private int _flashCount = 15; 
    // 消えている（薄い）時間           
    [SerializeField] private float _flashDuration = 0.1f;     
    // 見えている（濃い）時間
    [SerializeField] private float _visibleDuration = 0.1f;
    // ダメージを受けた時の透明度（0.0〜1.0）   
    [SerializeField] private float _flashAlpha = 0.3f;   

    [Header("死亡アニメーション設定")]
    // 死亡時にキャラクターが傾く角度
    [SerializeField] private float _dieRotateAngle = 10f;
    // 死亡演出が始まってから動きを止めるまでの秒数     
    [SerializeField] private float _dieAnimationDelay = 4f;   

    private CompositeDisposable _disposables;

    public bool _isInvincible { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public static void ResetInstance()
    {
        if (Instance != null)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        _isInvincible = false;
        _disposables = new CompositeDisposable();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        Player.Visual
        .Subscribe(_ =>
        {
            StartInvincibleFlash();
        }).AddTo(_disposables);
    }

    public void ChangeDirection(float moveInputX)
    {
        if (moveInputX > 0)
        {
            transform.localScale = new Vector3(_playerRotation, _playerRotation, _playerRotation);
        }
        else if (moveInputX < 0)
        {
            transform.localScale = new Vector3(-_playerRotation, _playerRotation, _playerRotation);
        }
    }

    public void StartInvincibleFlash()
    {
        _isInvincible = true;
        StartCoroutine(IsInvincible());
    }

    private IEnumerator IsInvincible()
    {
        Player.Instance._animator.SetTrigger("Hit");
        for (int i = 0; i < _flashCount; i++)
        {
            _spriteRenderer.color = new Color(1f, 1f, 1f, _flashAlpha);
            yield return new WaitForSeconds(_flashDuration);

            _spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            yield return new WaitForSeconds(_visibleDuration);
        }
        _spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        _isInvincible = false;
    }

    public void PlayDieAnimation(Rigidbody2D _rd, float _jumpForce)
    {
        _rd.linearVelocity = new Vector2(0, _jumpForce);
        
        if (transform.localScale.x < 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, -_dieRotateAngle);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, _dieRotateAngle);
        }
        StartCoroutine(ExitAnimation(_rd));
    }

    private IEnumerator ExitAnimation(Rigidbody2D _rd)
    {
        yield return new WaitForSeconds(_dieAnimationDelay);
        _rd.linearVelocity = new Vector2(_rd.linearVelocity.x, 0f);
        Destroy(_rd);
        yield return new WaitForSeconds(1f);
        _onDeathSubject.OnNext(Unit.Default);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }
}
