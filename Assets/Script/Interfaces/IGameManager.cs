using Cysharp.Threading.Tasks;
using System.Threading;

namespace GameDesignArchitecture.Interfaces
{
    public interface IGameManager 
    {
        void ChangeState(IGameManager newState);
        void PlayerGenerateNotification();
        void SendEnemyGenerateNotification();
        void SetEnemyPhysicsEnabled(bool enabled);
        void SetPlayerPhysicsEnabled(bool enabled);
        UniTask GameOverSequenceAsync(CancellationToken ct);
        UniTask GameClearSequenceAsync(CancellationToken ct);
        float LastScore{get;}
    }

}