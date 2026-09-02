using System.Threading;
using Cysharp.Threading.Tasks;

public interface IGameManagerState
{
    UniTask EnterAsync(GameManager gameManager,CancellationToken ct);
    UniTask ExitAsync(CancellationToken ct);

}
