using System;
using System.Runtime.InteropServices; // JavaScriptと通信するための呪文
using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    // JavaScriptで作ったスマホ判定関数を呼び出すお守り
    [DllImport("__Internal")]
    private static extern bool IsMobileBrowser();

    [Header("モバイル用UIの親オブジェクト")]
    [SerializeField] private GameObject[] mobileUIObject; 

    void Start()
    {
        // 💡 unityroom（WebGLブラウザ上）で動いているときだけ、JavaScriptの判定を動かす
#if !UNITY_EDITOR && UNITY_WEBGL
        if (IsMobileBrowser())
        {
            Debug.Log("スマホブラウザ：十字キーを表示します");
            for(int i = 0; i < mobileUIObject.Length; i++)
            {
                mobileUIObject[i].SetActive(true);
            }
        }
        else
        {
            Debug.Log("PCブラウザ：十字キーを非表示にします");
            for(int i = 0; i < mobileUIObject.Length; i++)
            {
                mobileUIObject[i].SetActive(false);
            }
        }
#else
        for(int i = 0; i < mobileUIObject.Length; i++)
        {
             // Unityの編集画面（エディタ）でテスト中のときは、開発しやすいように表示しておく
            mobileUIObject[i].SetActive(true);
        }
#endif
    }
}
