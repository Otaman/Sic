using System;

namespace Sic
{
    //not a static to allow implement custom strategies as extension methods
    //ex.: new Cache().ClearAfter10Reads()
    public class Cache
    {
        public static ICachingStrategy KeepForever(TimeSpan delayOnFailedLoads)
        {
            return new KeepForever(delayOnFailedLoads);
        } 
    }
}