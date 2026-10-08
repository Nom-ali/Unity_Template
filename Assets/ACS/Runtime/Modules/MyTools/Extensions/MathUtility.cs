using UnityEngine;

public static class MathUtility
{
    /// <summary>
    /// Calculates the percentage of a total value.
    /// </summary>
    /// <param name="total">The total value.</param>
    /// <param name="percentage">The percentage to calculate.</param>
    /// <returns>The calculated percentage value.</returns>
    public static float PercentageOfTotal(float total, float percentage)
    {
        return (percentage / 100) * total;
    } 
    
    public static int PercentageOfTotal(int total, float percentage)
    {
        return (int)((percentage / 100) * total);
    }
}




