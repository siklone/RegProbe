using System.Threading.Tasks;

namespace WindowsOptimizer.App.Services.Hardware
{
    public interface IMotherboardProvider
    {
        Task<MotherboardInfo> GetAsync();
    }
}
