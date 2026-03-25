using System.Threading.Tasks;

namespace OpenTraceProject.App.Services.Hardware
{
    public interface IMotherboardProvider
    {
        Task<MotherboardInfo> GetAsync();
    }
}
