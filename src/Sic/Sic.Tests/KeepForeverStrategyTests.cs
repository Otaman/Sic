using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sic.Tests
{
    [TestFixture]
    public class KeepForeverStrategyTests
    {
        private readonly ICachingStrategy _cachingStrategy = Cache.KeepForever(delayOnFailedLoads: TimeSpan.Zero);
        
        [Test]
        public async Task GetValue_ProvidesValueFromLoader()
        {
            var cached = _cachingStrategy.CreateCachedValue(async () => 1);
            var value = await cached.GetValue();
            
            Assert.AreEqual(1, value);
        }
        
        [Test]
        public void GetValue_ThrowsExceptionFromLoader()
        {
            var helper = new ConcurrencyHelper();
            var cached = _cachingStrategy.CreateCachedValue(helper.ThrowThenReturnValue());

            var task = cached.GetValue();
            helper.ResumeLoader();
            
            var ex = Assert.ThrowsAsync<Exception>(() => task);
            Assert.AreEqual("123", ex.Message);
        }
        
        [Test]
        public async Task GetValue_ProvidesSameValueOnSubsequentRequests()
        {
            var cached = _cachingStrategy.CreateCachedValue(async () => Guid.NewGuid());
            var first = await cached.GetValue();
            var second = await cached.GetValue();
            
            Assert.AreEqual(first, second);
        }
        
        [Test]
        public async Task GetValue_LoadsValueAgain_WhenPreviousLoadThrownException()
        {
            var helper = new ConcurrencyHelper();
            var cached = _cachingStrategy.CreateCachedValue(helper.ThrowThenReturnValue());
            
            var firstTask = cached.GetValue();
            helper.ResumeLoader();
            Assert.ThrowsAsync<Exception>(() => firstTask);
            
            await helper.WaitNextCall(); // wait when cache update the value in background
            helper.ResumeLoader();
            
            Assert.AreEqual(2, await cached.GetValue());
        }
        
        [Test]
        public async Task GetValue_ReturnsSameValueForConcurrentRequests()
        {
            var barrier = new SemaphoreSlim(0, 1);
            var cached = _cachingStrategy.CreateCachedValue(async () =>
            {
                await barrier.WaitAsync(4000);
                return Guid.NewGuid();
            });
            
            var firstTask = cached.GetValue();
            var secondTask = cached.GetValue();
            barrier.Release();
            
            Assert.AreEqual(await firstTask, await secondTask);
        }
        
        [Test]
        public async Task GetValue_ThrowsSameExceptionForConcurrentRequests()
        {
            var barrier = new SemaphoreSlim(0, 1);
            var cached = _cachingStrategy.CreateCachedValue<bool>(async () =>
            {
                await barrier.WaitAsync(4000);
                throw new Exception("123");
            });
            
            var firstTask = cached.GetValue();
            var secondTask = cached.GetValue();
            barrier.Release();

            var firstException = Assert.ThrowsAsync<Exception>(() => firstTask);
            var secondException = Assert.ThrowsAsync<Exception>(() => secondTask);
            Assert.AreEqual(firstException, secondException);
        }
        
        [Test]
        public async Task GetValue_EventuallyReturnsValueAfterExceptions()
        {
            Func<Task<int>> ThrowManyTimesThenReturnValue(int exceptionsCount)
            {
                var i = 0;
                return async () => ++i <= exceptionsCount ? throw new Exception("123") : i;
            }
            
            var cached = Cache.KeepForever(delayOnFailedLoads: TimeSpan.FromMilliseconds(10))
                .CreateCachedValue(ThrowManyTimesThenReturnValue(50));
            
            Assert.DoesNotThrowAsync(async () =>
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                var value = await GetValueWithRetries(cached, cts.Token);
                Assert.AreEqual(51, value);
            });
        }
        
        [Test]
        public async Task GetValue_ProducesAlwaysSameResponse_AfterFirstSuccessfulLoad()
        {
            Func<Task<int>> ThrowManyTimesThenReturnValue(int exceptionsCount)
            {
                var i = 0;
                return async () => ++i <= exceptionsCount ? throw new Exception("123") : i;
            }
            
            var cached = Cache.KeepForever(delayOnFailedLoads: TimeSpan.FromMilliseconds(10))
                .CreateCachedValue(ThrowManyTimesThenReturnValue(50));
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var first = await GetValueWithRetries(cached, cts.Token);
                
            //ensure at least one subsequent read will be made after first successful load
            var second = await cached.GetValue(); 
            Assert.AreEqual(51, first);
            Assert.AreEqual(51, second);

            while (!cts.IsCancellationRequested)
            {
                var value = await cached.GetValue();
                Assert.AreEqual(51, value);
            }
        }
        
        private async Task<T> GetValueWithRetries<T>(ICachedAsync<T> cached, CancellationToken token)
        {
            do
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(5, token);
                    return await cached.GetValue();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // ignored
                }
            } while (true);
        }

        private class ConcurrencyHelper
        {
            private readonly SemaphoreSlim _switch = new (0, 1);
            private readonly SemaphoreSlim _inner = new (0, 1);
            
            public void ResumeLoader()
            {
                if (_inner.CurrentCount > 0) 
                    _inner.Wait(4000);

                _switch.Release();
            }

            public Task WaitNextCall() => _inner.WaitAsync(4000);

            public Func<Task<int>> ThrowThenReturnValue()
            {
                var i = 0;
                return async () =>
                {
                    _inner.Release();
                    await _switch.WaitAsync(4000);
                    return ++i == 1 ? throw new Exception("123") : i;
                };
            }
        }
    }
}