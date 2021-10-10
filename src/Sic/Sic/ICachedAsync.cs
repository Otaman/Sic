using System.Threading.Tasks;

namespace Sic
{
    public interface ICachedAsync<T>
    {
        Task<T> GetValue();
    }
}