// OutputDriver.cs
// SimParameters → SimulationOutput への変換
// Live2D連携ポイント：ここの出力値をAnimator Parameter / Cubism Parameterに接続する

using UnityEngine;

public class OutputDriver
{
    [System.Serializable]
    public class OutputWeights
    {
        [Header("=== BodyTension係数 ===")]
        public float BodyTensionResistance = 0.6f;
        public float BodyTensionArousal    = 0.4f;

        [Header("=== BreathDepth係数 ===")]
        public float BreathDepthArousal = 0.5f;
        public float BreathDepthFatigue = 0.5f;

        [Header("=== FaceHeat係数 ===")]
        public float FaceHeatArousal   = 0.7f;
        public float FaceHeatDriveBias = 0.3f; // DriveBias-（マイナス方向）

        [Header("=== EyeFocus係数 ===")]
        public float EyeFocusDrive      = 0.5f;
        public float EyeFocusResistance = 0.5f;

        [Header("=== NeedMotion係数 ===")]
        public float NeedMotionDrive     = 0.5f;
        public float NeedMotionDriveBias = 0.5f; // DriveBias-

        [Header("=== Aftershock ===")]
        public float AftershockDecaySpeed = 2.0f; // 余韻の収束速度
    }

    OutputWeights weights;
    float aftershockValue = 0f;

    public OutputDriver(OutputWeights w)
    {
        weights = w;
    }

    public SimulationOutput Compute(SimParameters param, bool justOrgasmed)
    {
        // 射精直後にAftershockをスパイク
        if (justOrgasmed)
            aftershockValue = 1.0f;
        else
            aftershockValue = Mathf.MoveTowards(aftershockValue, 0f, weights.AftershockDecaySpeed * Time.deltaTime);

        float driveBiasMinus = Mathf.Max(0f, -param.DriveBias); // マイナス方向の強さ

        return new SimulationOutput
        {
            BodyTension  = Clamp01(param.Resistance * weights.BodyTensionResistance
                                 + param.Arousal    * weights.BodyTensionArousal),

            BodyYield    = Clamp01((1f - param.Resistance) * 0.6f
                                 + param.Arousal           * 0.4f),

            BreathDepth  = Clamp01(param.Arousal  * weights.BreathDepthArousal
                                 + param.Fatigue  * weights.BreathDepthFatigue),

            FaceHeat     = Clamp01(param.Arousal  * weights.FaceHeatArousal
                                 + driveBiasMinus * weights.FaceHeatDriveBias),

            EyeFocus     = Clamp01((1f - param.Drive) * weights.EyeFocusDrive
                                 + param.Resistance    * weights.EyeFocusResistance),

            ControlMask  = Clamp01(param.Resistance  * 0.5f
                                 + (1f - param.Drive) * 0.5f),

            NeedMotion   = Clamp01(param.Drive        * weights.NeedMotionDrive
                                 + driveBiasMinus      * weights.NeedMotionDriveBias),

            PeakDrive    = param.Drive,

            Aftershock   = aftershockValue,
        };
    }

    float Clamp01(float v) => Mathf.Clamp01(v);
}
