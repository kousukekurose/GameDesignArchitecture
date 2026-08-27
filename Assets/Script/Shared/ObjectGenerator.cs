using UnityEngine;

public class ObjectGenerator : MonoBehaviour
{
    [SerializeField] GameObject _Object;
    private Vector3 spawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PSpawn();
    }

    public void PSpawn()
    {
        spawn = transform.position;
        Instantiate (_Object, spawn, Quaternion.identity);
    }
}
