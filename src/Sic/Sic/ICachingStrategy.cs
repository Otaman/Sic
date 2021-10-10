using System;
using System.Threading.Tasks;

namespace Sic
{
    public interface ICachingStrategy
    {
        ICachedAsync<T> CreateCachedValue<T>(Func<Task<T>> loader);
    }
}