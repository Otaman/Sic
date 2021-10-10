using System.Threading.Tasks;

namespace Sic
{
    internal class CachedAsync<T>: ICachedAsync<T>
    {
        private Task<T> _getter;
        
        internal CachedAsync(Task<T> getter) => _getter = getter;

        public Task<T> GetValue() => _getter;

        internal void UpdateValue(Task<T> getter) => _getter = getter;
    }
}