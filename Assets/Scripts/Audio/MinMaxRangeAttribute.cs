using UnityEngine;

public sealed class MinMaxRangeAttribute : PropertyAttribute
{
    public MinMaxRangeAttribute(float minimum, float maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public float Minimum { get; }
    public float Maximum { get; }
}
