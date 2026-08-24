using System.Collections;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    public static PlayerVisual Instance{get; private set;}

    private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _playerRotation = 1f;

    public bool _isInvincible {get; private set;}

    private void Awake()
    {
        if(Instance == null) Instance = this;
    }

    private void Start()
    {
        _isInvincible = false;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ChangeDirection(float moveInputX)
    {
        if(moveInputX > 0)
        {
            transform.localScale = new Vector3(_playerRotation,_playerRotation,_playerRotation);
        }
        else if(moveInputX < 0)
        {
            transform.localScale = new Vector3(-_playerRotation,_playerRotation,_playerRotation);
        }
    }

    public void StartInvincibleFlash()
    {
        _isInvincible = true;
        StartCoroutine(IsInvincible());
    }

    private IEnumerator IsInvincible()
    {
        for(int i = 0; i < 15; i++)
        {
            _spriteRenderer.color = new Color(1f,1f,1f,0.3f);
            yield return new WaitForSeconds(0.1f);

            _spriteRenderer.color = new Color(1f,1f,1f,1f);
            yield return new WaitForSeconds(0.1f);
        }
        _spriteRenderer.color = new Color(1f,1f,1f,1f);
        _isInvincible = false;
    }

    public void PlayDieAnimation(Rigidbody2D _rd, float _jumpForce)
    {
        _rd.linearVelocity = new Vector2(0, _jumpForce);
        if(transform.localScale.x < 0)
        {
            transform.rotation = Quaternion.Euler(0,0,-10);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0,0,10);
        }
        StartCoroutine(ExitAnimation(_rd));
    }

    private IEnumerator ExitAnimation(Rigidbody2D _rd)
    {
        yield return new WaitForSeconds(2f);
        _rd.linearVelocity = new Vector2(_rd.linearVelocity.x, 0f);
    }
}
