using UnityEngine;

public class WaveDistortionEffect : MonoBehaviour
{
    [SerializeField] private float waveSpeed = 5.0f;  // 波の速さ
    [SerializeField] private float waveAmount = 0.05f; // 波の細かさ（歪み具合）
    
    private Vector3 originalScale;

    void Start()
    {
        // 最初の大きさを記憶
        originalScale = transform.localScale;
    }

    void Update()
    {
        // サイン波を使って、縦横の大きさを超高速でユラユラ変化させる
        float distortX = Mathf.Sin(Time.time * waveSpeed) * waveAmount;
        float distortY = Mathf.Cos(Time.time * waveSpeed * 1.5f) * waveAmount;

        // 大きさを変形させることで、重なっている背景が歪んでいるように見せる
        transform.localScale = new Vector3(originalScale.x + distortX, originalScale.y + distortY, 1);
    }
}
