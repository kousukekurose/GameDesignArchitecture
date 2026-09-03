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

    private static readonly Subject<Unit> _onSeActionSubject = new();
    public static Observable<Unit> OnSEAction => _onSeActionSubject;

    private Button _titleButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.StateChanged
        .Subscribe(state =>
        {
            switch(state)
            {
                case GameManagerStateInitialize:
                    _gameClearObject.SetActive(false);
                    _gameOverObject.SetActive(false);
                    break;
                case GameManagerStateGameClear:
                    Debug.Log("GameUIManager: Game Clear");
                    _gameClearObject.SetActive(true);
                    GameObjectButton(_gameClearObject);
                    break;
                case GameManagerStateGameOver:
                    Debug.Log("GameUIManager: Game Over");
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
        }
        
        _titleButton.interactable = true;

        _titleButton.OnClickAsObservable()
        .ThrottleFirst(TimeSpan.FromSeconds(0.5f),UnityTimeProvider.Update)
        .SubscribeAwait(async (_,ct) =>
        {
            PlayClickSE();
            await TitleButton(ct);
        }).AddTo(this);
    }

    private void PlayClickSE()
    {
        Debug.Log("SE: カチッ！");
        _onSeActionSubject.OnNext(Unit.Default);
    }

    private async UniTask TitleButton(CancellationToken ct)
    {
        _titleButton.interactable = false;
        await UniTask.Delay(500,cancellationToken : ct);
        
        // シングルトンのリセット
        GameManager.ResetInstance();
        Player.ResetInstance();
        PlayerVisual.ResetInstance();
        PlayerController.ResetInstance();
        
        await SceneManager.LoadSceneAsync("Title").WithCancellation(ct);
    } 
}
