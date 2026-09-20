using System.Threading.Tasks;
namespace Resources.WolfAPI;
public partial class WolfApi
{
    public static Task<GpusResponse?> GetGpus() => GetAsync<GpusResponse>("/gpus");
}
