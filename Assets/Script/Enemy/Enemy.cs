using UnityEngine;
using R3;

public class Enemy : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string enemyId = "Slime"; // Resources/EnemyData/ 内のファイル名

    // ★ Controllerからアクセスできるように public で確実に定義
    public ReadOnlyReactiveProperty<int> Hp => _hp;
    public EnemyData Data => _enemyData;
    public float Speed => _enemyData != null ? _enemyData.MoveSpeed : 0f;

    private EnemyData _enemyData;
    private ReactiveProperty<int> _hp;

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
