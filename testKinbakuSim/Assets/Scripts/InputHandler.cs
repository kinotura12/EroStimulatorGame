// InputHandler.cs
// MainIntensity / SubA / SubB の入力受け取り
// UIのOnValueChanged / OnClickから呼ぶ

using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [Header("=== 現在の入力値（確認用） ===")]
    [Range(0f, 1f)]
    [SerializeField] float mainIntensity = 0f;
    [SerializeField] bool isActive       = false;
    [SerializeField] bool subA           = false;
    [SerializeField] bool subB           = false;

    // 読み取り用プロパティ
    public float MainIntensity => mainIntensity;
    public bool  IsActive      => isActive;
    public bool  SubA          => subA;
    public bool  SubB          => subB;

    // --- UIから呼ぶメソッド ---

    // スライダーのOnValueChangedに接続
    public void SetMainIntensity(float value)
    {
        mainIntensity = Mathf.Clamp01(value);
        isActive      = true;
    }

    // 停止ボタン or スライダーを0に戻したとき
    public void SetStop()
    {
        isActive = false;
    }

    // SubAトグルボタン
    public void SetSubA(bool value)
    {
        Debug.Log($"[InputHandler] SetSubA called: {value}");
        subA = value;
    }

    // SubBトグルボタン
    public void SetSubB(bool value)
    {
        Debug.Log($"[InputHandler] SetSubB called: {value}");
        subB = value;
    }
}
