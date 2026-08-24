using UnityEngine;

public class PlayerState : MonoBehaviour{}


public class PlayerStateIdel :IPlayerState
{
    private Player _player;

    public PlayerStateIdel(Player player)
    {
        _player = player;
    }

    // その状態の部屋に「入った瞬間」に1回だけやる仕事
    public void Enter()
    {
        Debug.Log("アイドル開始");
        _player._rd.linearVelocity = new Vector2(0, _player._rd.linearVelocity.y);
    } 

    // その状態の部屋に「いる間」、毎フレーム連打でやる仕事
    public void Update()
    {
        if (PlayerController.Instance._moveInput.x != 0f)
        {
            _player.ChangeState(new PlayerStateMove(_player));
        }
    } 

    public void FixedUpdate() 
    {
        
    }

    // その状態の部屋から「出る瞬間」に1回だけやる仕事
    public void Exit()
    {
        
    }  
}

public class PlayerStateMove :IPlayerState
{
    private Player _player;

    public PlayerStateMove(Player player)
    {
        _player = player;
    }
    // その状態の部屋に「入った瞬間」に1回だけやる仕事
    public void Enter(){Debug.Log("Move開始");} 

    // その状態の部屋に「いる間」、毎フレーム連打でやる仕事
    public void Update()
    {
        _player.Move();
        if(PlayerController.Instance._moveInput.x == 0f)
        {
            _player.ChangeState(new PlayerStateIdel(_player));
        }
    } 

    public void FixedUpdate() 
    {
        
    }

    // その状態の部屋から「出る瞬間」に1回だけやる仕事
    public void Exit()
    {
        
    }  
}

public class PlayerStateJump :IPlayerState
{
    private Player _player;

    public PlayerStateJump(Player player)
    {
        _player = player;
    }
    // その状態の部屋に「入った瞬間」に1回だけやる仕事
    public void Enter(){} 

    // その状態の部屋に「いる間」、毎フレーム連打でやる仕事
    public void Update()
    {
        _player.OnLand();
    } 

    public void FixedUpdate() 
    {
        float _targetXVelocity = _player._rd.linearVelocity.x;
        if(PlayerController.Instance._moveInput.x != 0f)
        {
            _targetXVelocity = PlayerController.Instance._moveInput.x * _player._moveSpeed;
        }
        Vector2 _airVelocity = new Vector2(_targetXVelocity,_player._rd.linearVelocity.y);
        _player._rd.linearVelocity = _airVelocity;
        //ジャンプ中プレイヤーの向きを変える仕組み
        PlayerVisual.Instance.ChangeDirection(PlayerController.Instance._moveInput.x);
    }
    
    // その状態の部屋から「出る瞬間」に1回だけやる仕事
    public void Exit()
    {
        
    }  
}

public class PlayerStateDie :IPlayerState
{
    private Player _player;

    public PlayerStateDie(Player player)
    {
        _player = player;
    }
    // その状態の部屋に「入った瞬間」に1回だけやる仕事
    public void Enter()
    {
        _player.Die();
    } 

    // その状態の部屋に「いる間」、毎フレーム連打でやる仕事
    public void Update()
    {
        
    } 
    
    public void FixedUpdate() 
    {
        
    }
    
    // その状態の部屋から「出る瞬間」に1回だけやる仕事
    public void Exit()
    {
        
    }  
}