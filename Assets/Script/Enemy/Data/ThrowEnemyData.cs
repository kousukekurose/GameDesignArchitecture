using UnityEngine;

[CreateAssetMenu(fileName = "NewThrowEnemyData", menuName = "Enemy/ThrowEnemyData")]
public class ThrowEnemyData : EnemyData
{
    [SerializeField] private GameObject ThrowPrefab; // 投擲するオブジェクトのプレハブ
    [SerializeField] private float _throwForce;

    public GameObject GetThrowPrefab() => ThrowPrefab;

    public float ThrowForce => _throwForce;

    public override IEnemyState GetInitialState()
    {
        return new ThrowState();
    }
}