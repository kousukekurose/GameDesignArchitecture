using UnityEngine;

[CreateAssetMenu(fileName = "NewJumpEnemyData", menuName = "Enemy/JumpEnemyData")]
public class JumpEnemyData : EnemyData
{
    [SerializeField] private float _jumpForce;
    public float JumpForce => _jumpForce;
    public override IEnemyState GetInitialState()
    {
        return new JumpState();
    }
}