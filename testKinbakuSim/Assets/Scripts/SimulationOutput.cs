// SimulationOutput.cs
// Live2D連携用の出力パラメータ（すべて 0.0～1.0に正規化）

[System.Serializable]
public struct SimulationOutput
{
    public float BodyTension;   // 体幹・肩の緊張        (Resistance, Arousal)
    public float BodyYield;     // 腰の脱力・反応        (1-Resistance, Arousal)
    public float BreathDepth;   // 呼吸の深さ・乱れ      (Arousal, Fatigue)
    public float FaceHeat;      // 顔の赤み・火照り      (Arousal, DriveBias-)
    public float EyeFocus;      // 目の焦点・理性残量    (1-Drive, Resistance)
    public float ControlMask;   // 感情抑制の度合い      (Resistance, 1-Drive)
    public float NeedMotion;    // 腰の反応・求める動き  (Drive, DriveBias-)
    public float PeakDrive;     // 崩壊演出の強度        (Drive)
    public float Aftershock;    // 余韻の震え            (Fatigue・射精直後)
    public float RejectMotion;      // イヤイヤ発火時の強度（0..1）
    public float RejectHabituation; // イヤイヤ慣れ（0..1）
    public float EdgeTension;       // 射精我慢テンション（0..1）→ 表情/体テンションに使用
    public float OrgasmScale;       // 直前の射精の激しさ（0=最小、1=最大）→ モーション強度・長さに使用
    public float CumulativeOrgasm;  // 累積オーガズム（繰り返すほど増加、時間で減衰）→ 絶頂後の表情演出に使用
}
