using UnityEngine;
using R3;

public class DeathController : MonoBehaviour
{
    private static readonly Subject<Unit> _deathtrigger = new();
    public static Subject<Unit> DeathTrigger => _deathtrigger;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            _deathtrigger.OnNext(Unit.Default);
        }
    }
}
