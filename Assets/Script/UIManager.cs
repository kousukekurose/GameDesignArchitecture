using UnityEngine.SceneManagement;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject controllersUI;
    [SerializeField] private GameObject mainButton;

    void Start()
    {
        controllersUI.SetActive(false);
        mainButton.SetActive(true);
    }

    public void OnControllerON()
    {
        controllersUI.SetActive(true);
        mainButton.SetActive(false);
    }

    public void OnControllerOFF()
    {
        controllersUI.SetActive(false);
        mainButton.SetActive(true);
    }

    public void NextLoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

}
