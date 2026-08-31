using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

// 1. パトロール状態
public class PatrolState : IEnemyState
{
    public UniTask EnterAsync(IEnemyController controller, CancellationToken ct)
    {
        //パトロールアニメーション再生場所
        //controller.Visual.PlayAnimation("Walk");
        return UniTask.CompletedTask;
    }

    public async UniTask UpdateAsync(IEnemyController controller, CancellationToken ct)
    {
        // 追尾移行の判定をすべて削除し、純粋なパトロール移動のみを実行
        await controller.MovePatrolAsync(ct);
    }

    public UniTask ExitAsync(CancellationToken ct) => UniTask.CompletedTask;
}

// 2. 死亡状態
public class DeadState : IEnemyState
{
    public async UniTask EnterAsync(IEnemyController controller, CancellationToken ct)
    {
        // StopMovingの代わりに、Rigidbody2Dの速度を直接ゼロにして完全に停止させる
        if (controller.Enemy.TryGetComponent<Rigidbody2D>(out var rb2d))
        {
            rb2d.linearVelocity = Vector2.zero;
        }

        //controller.Visual.PlayAnimation("Die");
        Debug.Log($"{controller.Enemy.gameObject.name} が死亡しました。");
        
        await UniTask.Delay(System.TimeSpan.FromSeconds(1), cancellationToken: ct);
        if(controller.Enemy.gameObject != null)
        {
            Debug.Log("nullに変更");
            controller.Enemy.gameObject.layer = LayerMask.NameToLayer("null");
        }
        Object.Destroy(controller.Enemy.gameObject);
    }

    public UniTask UpdateAsync(IEnemyController controller, CancellationToken ct) => UniTask.CompletedTask;
    public UniTask ExitAsync(CancellationToken ct) => UniTask.CompletedTask;
}
