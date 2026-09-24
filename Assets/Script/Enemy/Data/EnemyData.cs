using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("--- Base Status ---")]
    [SerializeField] private string _enemyName = "Goon";
    [SerializeField] private int _maxHp;
    [SerializeField] private int _attackPower;
    [SerializeField] private float _moveSpeed;

    [Header("--- Side Scroll AI Settings ---")]
    [SerializeField] private float _detectionRange = 5.0f;  // 索敵距離
    [SerializeField] private float _detectionAngle = 90f;   // 視界の角度
    [SerializeField] private float _attackRange = 1.2f;    // 攻撃射程
    [SerializeField] private float _wallCheckDistance = 0.3f;  // 前方の壁検知距離
    [SerializeField] private float _cliffCheckDistance = 0.5f; // 足元の崖検知距離

    // 外部読み出し用プロパティ
    public string EnemyName => _enemyName;
    public int MaxHp => _maxHp;
    public int AttackPower => _attackPower;
    public float MoveSpeed => _moveSpeed;
    public float DetectionRange => _detectionRange;
    public float DetectionAngle => _detectionAngle;
    public float AttackRange => _attackRange;
    public float WallCheckDistance => _wallCheckDistance;
    public float CliffCheckDistance => _cliffCheckDistance;

    public virtual IEnemyState GetInitialState()
    {
        return new PatrolState();
    }
}
