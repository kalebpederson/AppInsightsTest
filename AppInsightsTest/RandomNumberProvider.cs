using System;

namespace AppInsightsTest
{
    public class RandomNumberProvider : IRandomNumberProvider
    {
        private readonly object _syncLock = new();
        private readonly Random _random = new();       
        
        public int GetRandomNumberUpTo(int max)
        {
            return Convert.ToInt32(
                (GetRandomStepForPercentile(100, max)
                 + GetRandomStepForPercentile(50, max)
                 + GetRandomStepForPercentile(35, max)
                 + GetRandomStepForPercentile(20, max)
                 + GetRandomStepForPercentile(10, max)
                 + GetRandomStepForPercentile(5, max))
                / 220.0);
        }

        private int GetRandomStepForPercentile(int percent, int max)
        {
            switch (percent)
            {
                case < 0:
                case > 100:
                    throw new ArgumentOutOfRangeException(nameof(percent), "Percentage must be between 0 and 100");
                case 100:
                {
                    lock (_syncLock)
                    {
                        return _random.Next(max);
                    }
                }
                default:
                    return Convert.ToInt32((100 - percent) / 200.0) +
                           _random.Next(Convert.ToInt32((percent / 100.0) * max));
            }
        }
        
    }
}