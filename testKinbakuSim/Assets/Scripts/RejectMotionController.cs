// RejectMotionController.cs
// イヤイヤ（拒否）モーション制御

using UnityEngine;

[System.Serializable]
public class RejectMotionSettings
{
    [Header("=== 慣れ（Habituation）===")]
    [Range(0f, 1f)]
    public float InitialHab = 0.5f;           // ゲーム開始時の慣れ初期値

    [Range(0f, 0.5f)]
    public float HabGainPerSecActive = 0.03f; // 入力中の慣れ上昇速度（/秒、intensity=0のとき。Aboveでは15%に低下）

    [Range(0f, 0.5f)]
    public float HabGainPerSecStopped = 0.36f; // 停止中の慣れ上昇速度（/秒）

    [Range(0f, 2f)]
    public float HabDropOnRise = 0.8f;        // 強度が上がったときの慣れ低下量（/単位変化率）
    // 強度が下がったときは HabDropOnRise * 0.3 を適用

    [Range(0f, 1f)]
    public float HabDropOnSubStart = 0.12f;   // SubA/B 入力開始時の慣れ低下量

    [Header("=== ピーク（急激な入力変化）===")]
    public float PeakThreshold = 2.0f;        // ピーク判定の変化率しきい値（単位/秒）

    [Range(0f, 5f)]
    public float PeakBoostRate = 1.5f;        // ピーク時の発火率追加（回/秒）

    [Range(1f, 30f)]
    public float PeakFatigueCooldown = 8.0f;  // ピーク慣れリセット時間（秒）

    [Header("=== 発火率（回/秒, Resistanceで変化）===")]
    public float FireRateMin = 0.05f;
    public float FireRateMax = 1.2f;

    [Header("=== モーション波形（Resistanceで変化）===")]
    [Range(0f, 1f)]
    public float AmpMin = 0.2f;              // 最小振れ幅
    [Range(0f, 1f)]
    public float AmpMax = 0.85f;             // 最大振れ幅

    public float MotionDuration = 0.6f;      // 1回の揺れの長さ（秒）

    public float FreqMin = 2.2f;             // 最小揺れ速さ（Hz）
    public float FreqMax = 5.5f;             // 最大揺れ速さ（Hz）
}

public class RejectMotionController
{
    float habituation;
    float peakCooldownTimer; // > 0 のあいだはピークに慣れている
    bool  initialized;

    float prevMain;
    float prevSubA;
    float prevSubB;
    bool  prevMainActive;

    // モーション波形
    float motionTimer;
    float motionDuration;
    float motionAmp;
    float motionFreq;
    float motionDir;
    float motionPhase;

    // --- 公開プロパティ ---
    public float Habituation     => habituation;
    public float OffsetX         { get; private set; }
    public float MotionIntensity { get; private set; }

    // --- デバッグ用 ---
    public float TriggerRate     { get; private set; }
    public bool  IsPeakFatigued  => peakCooldownTimer > 0f;

    public void Reset(float initialHab01 = 0f)
    {
        habituation       = Mathf.Clamp01(initialHab01);
        peakCooldownTimer = 0f;
        initialized       = false;

        prevMain       = 0f;
        prevSubA       = 0f;
        prevSubB       = 0f;
        prevMainActive = false;

        motionTimer    = 0f;
        OffsetX        = 0f;
        MotionIntensity = 0f;
        TriggerRate    = 0f;
    }

    public void Update(
        float dt,
        float resistance,
        bool  mainActive,
        float mainIntensity,
        bool  subA,
        bool  subB,
        RejectMotionSettings s)
    {
        if (dt <= 0f) return;

        float main = Mathf.Clamp01(mainIntensity);
        float subAV = subA ? 1f : 0f;
        float subBV = subB ? 1f : 0f;

        if (!initialized)
        {
            prevMain       = main;
            prevSubA       = subAV;
            prevSubB       = subBV;
            prevMainActive = mainActive;
            initialized    = true;
        }

        // ==================== 慣れ更新 ====================

        float habDelta = 0f;

        // 上昇（常時）: 強度が高いほど慣れにくい（Stop > Below > Within > Above）
        float habGain = mainActive
            ? s.HabGainPerSecActive * Mathf.Lerp(1f, 0.15f, main)
            : s.HabGainPerSecStopped;
        habDelta += habGain * dt;

        // 強度変化による低下（変化率 = 単位/秒）
        float dMainRate = (main - prevMain) / dt;
        if (dMainRate > 0f)
            habDelta -= s.HabDropOnRise * dMainRate * dt;         // 上昇は大きく低下
        else if (dMainRate < 0f)
            habDelta -= s.HabDropOnRise * 0.7f * (-dMainRate) * dt; // 下降も大きめ

        // SubA/B 開始（OFF -> ON）で低下
        if (subA && prevSubA < 0.5f) habDelta -= s.HabDropOnSubStart;
        if (subB && prevSubB < 0.5f) habDelta -= s.HabDropOnSubStart;

        habituation = Mathf.Clamp01(habituation + habDelta);

        // ==================== ピーク判定 ====================

        peakCooldownTimer = Mathf.Max(0f, peakCooldownTimer - dt);

        float absRate = Mathf.Abs(dMainRate);
        bool  isPeak  = absRate >= s.PeakThreshold;

        float peakBoost = 0f;
        if (isPeak && peakCooldownTimer <= 0f)
        {
            peakBoost         = s.PeakBoostRate;
            habituation       = 0f;             // ピーク時にHabをリセット
            peakCooldownTimer = s.PeakFatigueCooldown;
        }

        // ==================== 発火判定 ====================

        float baseRate  = Mathf.Lerp(s.FireRateMin, s.FireRateMax, Mathf.Clamp01(resistance));
        // Habはサブ要素：最大30%減まで（慣れても発火は止まらない）
        float habMod    = Mathf.Lerp(0.7f, 1f, 1f - habituation);
        // 慣れているほどピーク時に余計に驚く（Hab高→ピーク追加発火が増幅）
        float peakAmp   = Mathf.Lerp(1f, 2f, habituation);
        float fireRate  = baseRate * habMod + peakBoost * peakAmp;
        TriggerRate = Mathf.Max(0f, fireRate);

        if (motionTimer <= 0f && Random.value < TriggerRate * dt)
            StartMotion(resistance, s);

        // ==================== モーション波形 ====================

        OffsetX        = 0f;
        MotionIntensity = 0f;

        if (motionTimer > 0f)
        {
            motionTimer  -= dt;
            motionPhase  += dt * motionFreq * Mathf.PI * 2f;

            float t        = 1f - Mathf.Clamp01(motionTimer / Mathf.Max(0.0001f, motionDuration));
            float envelope = Mathf.Sin(t * Mathf.PI); // 0 → 1 → 0
            float raw      = Mathf.Sin(motionPhase) * motionAmp * envelope * motionDir;

            OffsetX        = Mathf.Clamp(raw, -1f, 1f);
            MotionIntensity = Mathf.Clamp01(Mathf.Abs(OffsetX));
        }

        // ==================== 前フレーム記憶 ====================

        prevMain       = main;
        prevSubA       = subAV;
        prevSubB       = subBV;
        prevMainActive = mainActive;
    }

    void StartMotion(float resistance01, RejectMotionSettings s)
    {
        float r = Mathf.Clamp01(resistance01);

        motionDuration = s.MotionDuration;
        motionFreq     = Mathf.Lerp(s.FreqMin, s.FreqMax, r);

        // 慣れが高いほど振れ幅が小さくなる
        float amp  = Mathf.Lerp(s.AmpMin, s.AmpMax, r);
        amp       *= 1f - habituation * 0.6f;
        motionAmp  = Mathf.Clamp01(amp);

        motionDir   = Random.value < 0.5f ? -1f : 1f;
        motionPhase = 0f;
        motionTimer = motionDuration;
    }
}
