using System;

namespace Sic
{
    //not a static to allow implement custom strategies as extension methods
    //ex.: new Cache().ClearAfter10Reads()
    public class Cache
    {
        public static ICachingStrategy KeepForever(TimeSpan delayOnFailedLoads) => 
            new KeepForever(delayOnFailedLoads);
        
        public static ICachingStrategy UpdateInBackground(TimeSpan delayWhenInUse, 
            TimeSpan delayWhenNoUse, TimeSpan delayOnFailedLoads,
            Action onUpdate = null, Action<Exception> onError = null)
        {
            var strategy = new UpdateInBackground(delayWhenInUse, delayWhenNoUse, delayOnFailedLoads);
            
            if (onUpdate != null) strategy.OnUpdate += onUpdate;
            if (onError != null) strategy.OnError += onError;

            return strategy;
        }
    }
}