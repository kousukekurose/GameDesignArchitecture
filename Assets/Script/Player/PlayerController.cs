using UnityEngine;
using UnityEngine.InputSystem;
using R3;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    
    private readonly ReactiveProperty<Vector2> _moveInputProp = new (Vector2.zero);
    public ReadOnlyReactiveProperty<Vector2> MoveInput => _moveInputProp;
    public Vector2 _moveInput => _moveInputProp.Value;
    private Player _player;

    private void Awake()
    {
        if(Instance == null)Instance = this;
    }

    private void Start()
    {
        _player = GetComponent<Player>();
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInputProp.Value = context.ReadValue<Vector2>();
        if(context.started)
        {
            if(_player.CurrentState is not PlayerStateDie)
            if (_player._isGround)
            _player.ChangeState(new PlayerStateMove(_player));
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        Debug.Log("走るボタン");
        if(_player == null)return;
        if (context.started)
        {
            _player._isSprint = true;
        }
        if(context.canceled)
        {
            _player._isSprint = false;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            if(_player.CurrentState is not PlayerStateDie)
            _player.ChangeState(new PlayerStateJump(_player));
        }
        if(context.canceled)
        {
            _player._hasStomped = false;
        }
    }

    private void OnDestroy()
    {
        _moveInputProp.Dispose();
    }
}
