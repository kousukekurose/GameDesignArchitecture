using UnityEngine;

public class Parallax2D : MonoBehaviour
{
    public Transform cameraTransform; // カメラのトランスフォーム
    public float parallaxEffect;     // スクロール速度の倍率（0.1 〜 0.5 など）
    
    private float lastCameraX;       // 前フレームのカメラのX座標

    void Start()
    {
        lastCameraX = cameraTransform.position.x;
    }

    void LateUpdate()
    {
        // カメラが今フレームでどれだけ動いたかを計算
        float deltaX = cameraTransform.position.x - lastCameraX;
        
        // カメラの移動量に倍率をかけて、背景を同じ方向に動かす（結果的にゆっくり後ろに流れる）
        transform.position += new Vector3(deltaX * parallaxEffect, 0, 0);
        
        // 現在のカメラ位置を保存
        lastCameraX = cameraTransform.position.x;
    }
}
