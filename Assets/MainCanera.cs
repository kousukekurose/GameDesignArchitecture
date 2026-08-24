using UnityEngine;

public class MainCanera : MonoBehaviour
{
    private Vector3 playerPos;
    [SerializeField] private float playerPosZ = 1;
    [SerializeField] private float playerPosY = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Player.Instance == null)return;
        playerPos = Player.Instance.transform.position;

        Camera.main.gameObject.transform.position = new Vector3(playerPos.x,playerPosY,-playerPosZ);
    }
}
