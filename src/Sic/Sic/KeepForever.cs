using System;
using System.Threading.Tasks;

namespace Sic
{
    public class KeepForever : ICachingStrategy
    {
        public KeepForever(TimeSpan delayOnFailedLoads)
        {
            DelayOnFailedLoads = delayOnFailedLoads;
        }

        public TimeSpan DelayOnFailedLoads { get; }
        
        public ICachedAsync<T> CreateCachedValue<T>(Func<Task<T>> loader)
        {
            var cached = new CachedAsync<T>(loader());
            _= RetryOnException(cached, loader);
            
            return cached;
        }

        private async Task RetryOnException<T>(CachedAsync<T> cached, Func<Task<T>> loader)
        {
            while (true)
            {
                try
                {
                    await cached.GetValue();
                    break;
                }
                catch (Exception)
                {
                    if (DelayOnFailedLoads > TimeSpan.Zero)
                        await Task.Delay(DelayOnFailedLoads);
                    
                    cached.UpdateValue(loader());
                }
            }
        }
    }
}