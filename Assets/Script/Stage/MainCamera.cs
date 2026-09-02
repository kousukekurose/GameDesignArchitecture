using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public static MainCamera Instance { get; private set; }
    private Vector3 playerPos;
    [SerializeField] private float playerPosZ = 1;
    [SerializeField] private float playerPosY = 10;

    private void Awake()
    {
        // シングルトンの実体を登録する（これがないと他所で .Instance を呼んだ時にエラーになります）
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Player.Instance == null)return;
        playerPos = Player.Instance.transform.position;

        Camera.main.gameObject.transform.position = new Vector3(playerPos.x,playerPosY,-playerPosZ);
    }
}
