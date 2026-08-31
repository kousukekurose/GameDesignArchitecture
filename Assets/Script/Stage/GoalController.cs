using UnityEngine;
using R3;

public class GoalController : MonoBehaviour
{
    private static readonly Subject<Unit> _goaltrigger = new();
    public static Subject<Unit> GoalTrigger => _goaltrigger;


    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("そもそも呼ばれているのか");
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            Debug.Log("プレイヤー判定");
            _goaltrigger.OnNext(Unit.Default);
        }
    }
}
