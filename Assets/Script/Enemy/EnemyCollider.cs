using UnityEngine;

public class EnemyCollider : MonoBehaviour,IDamageable
{
    [SerializeField] private Enemy _enemy;
    [SerializeField] private EnemyVisual _visual;

    public void TakeDamage(int _damage)
    {
        if(_enemy == null)return;
        _enemy.TakeDamage(_damage);
        if(_visual != null)
        {
            //_visual.PlayHitEffect();
        }
    }
}
