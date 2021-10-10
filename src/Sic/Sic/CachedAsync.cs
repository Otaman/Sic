using System;
using System.Threading.Tasks;

namespace Sic
{
    internal class CachedAsync<T>: ICachedAsync<T>
    {
        private Task<T> _getter;
        public event Action OnGet;
        
        internal CachedAsync(Task<T> getter) => _getter = getter;

        public Task<T> GetValue()
        {
            OnGet?.Invoke();
            return _getter;
        }

        public void UpdateValue(Task<T> getter) => _getter = getter;
    }
}