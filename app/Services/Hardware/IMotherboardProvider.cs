using System.Threading.Tasks;

namespace RegProbe.App.Services.Hardware
{
    public interface IMotherboardProvider
    {
        Task<MotherboardInfo> GetAsync();
    }
}
