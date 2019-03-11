using System.Threading;
using System.Threading.Tasks;

namespace ConsoleProducer
{
    public interface ITopicProducer
    {
        Task StartAsync(CancellationToken cancellationToken = default);
    }
}
