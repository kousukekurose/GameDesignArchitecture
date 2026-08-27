using Cysharp.Threading.Tasks;
using System.Threading;

public interface IEnemyController
{
    Enemy Enemy { get; }
    EnemyVisual Visual { get; }
    
    void ChangeState(IEnemyState newState);
    bool IsObstacleOrCliffAhead();
    UniTask MovePatrolAsync(CancellationToken ct);
}
