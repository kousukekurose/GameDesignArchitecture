using UnityEngine;
using R3;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] private GameObject _gameClearObject;
    [SerializeField] private GameObject _gameOverObject;

    private Button _titleButton;
    private Button _exitButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.CurrentState
        .Subscribe(state =>
        {
            switch(state)
            {
                case GameManager.GameState.Initialize:
                _gameClearObject.SetActive(false);
                _gameOverObject.SetActive(false);
                break;
                case GameManager.GameState.GameClear:
                _gameClearObject.SetActive(true);
                GameObjectButton(_gameClearObject);
                break;
                case GameManager.GameState.GameOver:
                _gameOverObject.SetActive(true);
                GameObjectButton(_gameOverObject);
                break;
            }
        }).AddTo(this);
    }

    private void GameObjectButton(GameObject obj)
    {
        Button[] buttons = obj.GetComponentsInChildren<Button>();
    
        foreach (var button in buttons)
        {
            if (button.name == "TitleButton")
                _titleButton = button;
            else if (button.name == "ExitButton")
                _exitButton = button;
        }
        
        _titleButton.interactable = true;
        _exitButton.interactable = true;

        _titleButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await TitleButton(ct);
        }).AddTo(this);

        _exitButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await ExitButoon(ct);
        }).AddTo(this);
    }

    private void PlayClickSE() => Debug.Log("SE: カチッ！");

    private async UniTask TitleButton(CancellationToken ct)
    {
        _titleButton.interactable = false;
        await UniTask.Delay(500,cancellationToken : ct);
        await SceneManager.LoadSceneAsync("Title").WithCancellation(ct);
    } 

    private async UniTask ExitButoon(CancellationToken ct)
    {
        _exitButton.interactable = false;
        await UniTask.Delay(500,cancellationToken : ct);
        #if UNITY_EDITOR
        // Unityエディタの「再生モード」を停止する
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 本番のゲーム（PC/スマホアプリ）を終了する
        Application.Quit();
#endif
    }
}
