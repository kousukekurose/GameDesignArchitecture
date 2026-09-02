using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

//パトロール状態
public class PatrolState : IEnemyState
{
    public UniTask EnterAsync(IEnemyController controller, CancellationToken ct)
    {
        //パトロールアニメーション再生場所
        controller.Enemy.Animator.SetTrigger("Walk");
        return UniTask.CompletedTask;
    }

    public async UniTask UpdateAsync(IEnemyController controller, CancellationToken ct)
    {
        // 追尾移行の判定をすべて削除し、純粋なパトロール移動のみを実行
        await controller.MovePatrolAsync(ct);
    }

    public UniTask ExitAsync(CancellationToken ct) => UniTask.CompletedTask;
}

//ジャンプ状態
public class JumpState : IEnemyState
{
    public UniTask EnterAsync(IEnemyController controller, CancellationToken ct)
    {
        // ジャンプ開始時の処理
        return UniTask.CompletedTask;
    }

    public async UniTask UpdateAsync(IEnemyController controller, CancellationToken ct)
    {
        // ジャンプ挙動の実装
        // controller.Enemy.Data.JumpForce を使用
        await controller.JumpAsync(ct);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
    }

    public UniTask ExitAsync(CancellationToken ct) => UniTask.CompletedTask;
}

//投擲状態
public class ThrowState : IEnemyState
{
    public UniTask EnterAsync(IEnemyController controller, CancellationToken ct)
    {
        // 投擲開始時の処理
        return UniTask.CompletedTask;
    }

    public async UniTask UpdateAsync(IEnemyController controller, CancellationToken ct)
    {
        // 投擲挙動の実装
        await controller.ThrowAsync(ct);
        await UniTask.Yield(PlayerLoopTiming.Update, ct);
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

