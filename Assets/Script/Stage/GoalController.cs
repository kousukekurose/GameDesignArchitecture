using UnityEngine;
using R3;

public class GoalController : MonoBehaviour
{
    private static readonly Subject<Unit> _goaltrigger = new();
    public static Subject<Unit> GoalTrigger => _goaltrigger;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _goaltrigger.OnNext(Unit.Default);
        }
    }
}
