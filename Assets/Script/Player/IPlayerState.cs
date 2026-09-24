using System.Threading;
using Cysharp.Threading.Tasks;

public interface IPlayerState 
{
    UniTask EnterAsync(Player player,CancellationToken ct);
    void Exit();
}
