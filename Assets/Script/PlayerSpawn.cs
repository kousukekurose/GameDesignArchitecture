using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] GameObject player;
    private Vector3 spawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PSpawn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PSpawn()
    {
        spawn = transform.position;
        Instantiate (player, spawn, Quaternion.identity);
    }
}
