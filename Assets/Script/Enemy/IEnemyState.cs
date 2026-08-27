using System.Threading;
using Cysharp.Threading.Tasks;

public interface IEnemyState
{
    UniTask EnterAsync(IEnemyController controller, CancellationToken ct);
    UniTask UpdateAsync(IEnemyController controller, CancellationToken ct);
    UniTask ExitAsync(CancellationToken ct);
}
