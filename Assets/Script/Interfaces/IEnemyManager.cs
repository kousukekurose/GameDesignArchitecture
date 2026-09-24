using UnityEngine;

namespace GameDesignArchitecture.Interfaces
{
    public interface IEnemyManager 
    {
        void RegisterEnemy(GameObject enemy);
        void SetAllEnemyiesPhysicsEnabled(bool enabled);
        int EnemyCount{get;}
    }

}
