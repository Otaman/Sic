using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sic
{
    public class UpdateInBackground : ICachingStrategy
    {
        //can't use bool, because Interlocked doesn't support it
        private int _hasReadsFromLastUpdate;
        
        public TimeSpan DelayWhenInUse { get; }
        public TimeSpan DelayWhenNoUse { get; }
        public TimeSpan DelayOnFailedLoads { get; }

        public event Action OnUpdate;
        public event Action<Exception> OnError;

        public UpdateInBackground(TimeSpan delayWhenInUse, 
            TimeSpan delayWhenNoUse, TimeSpan delayOnFailedLoads)
        {
            DelayWhenInUse = delayWhenInUse;
            DelayWhenNoUse = delayWhenNoUse;
            DelayOnFailedLoads = delayOnFailedLoads;
        }

        public ICachedAsync<T> CreateCachedValue<T>(Func<Task<T>> loader)
        {
            var cached = new CachedAsync<T>(loader());
            _= RunBackgroundUpdates(cached, loader);
            
            return cached;
        }

        private async Task RunBackgroundUpdates<T>(CachedAsync<T> cached, Func<Task<T>> loader)
        {
            //called concurrently, but unconditional update is fine
            cached.OnGet += () => _hasReadsFromLastUpdate = 1;
            //stabilize, then any error in future won't cause an exception on GetValue()
            _ = await UpdateValueUntilSucceed(cached, loader);
            
            var nextDelay = DelayWhenInUse;
            while (true)
            {
                try
                {
                    await Task.Delay(nextDelay);
                    
                    var task = loader();
                    await task; //no exception, good: update the value
                    cached.UpdateValue(task);
                    OnUpdate?.Invoke();
                    
                    var hadReads = Interlocked.Exchange(ref _hasReadsFromLastUpdate, 0);
                    nextDelay = hadReads == 0 ? DelayWhenNoUse : DelayWhenInUse;
                }
                catch (Exception e)
                {
                    OnError?.Invoke(e);
                    nextDelay = DelayOnFailedLoads;
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }
        
        private async Task<T> UpdateValueUntilSucceed<T>(CachedAsync<T> cached, Func<Task<T>> loader)
        {
            while (true)
            {
                try
                {
                    if (DelayOnFailedLoads > TimeSpan.Zero)
                        await Task.Delay(DelayOnFailedLoads);
                    
                    return await cached.GetValue();
                }
                catch (Exception e)
                {
                    OnError?.Invoke(e);
                    cached.UpdateValue(loader());
                }
            }
        }
    }
}