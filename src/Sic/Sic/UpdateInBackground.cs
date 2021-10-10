using System;
using System.Threading.Tasks;

namespace Sic
{
    //todo: implement background updates
    public class UpdateInBackground : ICachingStrategy
    {
        public UpdateInBackground(TimeSpan delayWhenInUse, 
            TimeSpan delayWhenNoUse, TimeSpan delayOnFailedLoads)
        {
            DelayWhenInUse = delayWhenInUse;
            DelayWhenNoUse = delayWhenNoUse;
            DelayOnFailedLoads = delayOnFailedLoads;
        }

        public TimeSpan DelayWhenInUse { get; }
        public TimeSpan DelayWhenNoUse { get; }
        public TimeSpan DelayOnFailedLoads { get; }
        
        public ICachedAsync<T> CreateCachedValue<T>(Func<Task<T>> loader)
        {
            throw new NotImplementedException();
        }
    }
}