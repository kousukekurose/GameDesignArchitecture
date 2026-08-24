using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    private Player _player;
    private void Awake()
    {
        if(Instance == null)Instance = this;
    }

    private void Start()
    {
        _player = GetComponent<Player>();
    }
    public Vector2 _moveInput{get; private set;}
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        if(context.started)
        {
            _player.Move();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            _player.Jump();
        }
        if(context.canceled)
        {
            _player._hasStomped = false;
        }
    }
}
