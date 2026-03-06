// BandEvaluator.cs
// Below / Within / Above / Stop の判定

public class BandEvaluator
{
    public InputBand Evaluate(float mainIntensity, bool isActive, float tolLow, float tolHigh)
    {
        if (!isActive)
            return InputBand.Stop;

        if (mainIntensity < tolLow)
            return InputBand.Below;

        if (mainIntensity > tolHigh)
            return InputBand.Above;

        return InputBand.Within;
    }
}
