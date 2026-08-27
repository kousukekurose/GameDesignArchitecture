using UnityEngine;
using R3;

public class Enemy : MonoBehaviour,IAttacker
{
    [Header("Settings")]
    [SerializeField] private string enemyId = "Slime"; // Resources/EnemyData/ 内のファイル名

    // ★ Controllerからアクセスできるように public で確実に定義
    public ReadOnlyReactiveProperty<int> Hp => _hp;
    public EnemyData Data => _enemyData;
    public float Speed => _enemyData != null ? _enemyData.MoveSpeed : 0f;

    public int _DamageAmount  => _enemyData != null ? _enemyData.AttackPower:0;

    private EnemyData _enemyData;
    private ReactiveProperty<int> _hp;
    public Collider2D _collider2D{get; private set;}
    public float _playerCheck{get; private set;} = 0.2f;
    public Rigidbody2D _rd{get;private set;}
    public LayerMask _playerLayer {get; private set;}

    private void Awake()
    {
        _enemyData = Resources.Load<EnemyData>($"EnemyData/{enemyId}");

        if (_enemyData == null)
        {
            Debug.LogError($"Resources/EnemyData/{enemyId} が見つかりません！");
            return;
        }

        _hp = new ReactiveProperty<int>(_enemyData.MaxHp);
    }

    private void Start()
    {
        _collider2D = GetComponent<Collider2D>(); 
        _rd = GetComponent<Rigidbody2D>();
        _playerLayer = LayerMask.GetMask("Player");
    }

    public void TakeDamage(int damage)
    {
        if (_hp == null) return;
        _hp.Value = Mathf.Max(0, _hp.Value - damage);
    }

    private void OnDestroy()
    {
        _hp?.Dispose();
    }
}
