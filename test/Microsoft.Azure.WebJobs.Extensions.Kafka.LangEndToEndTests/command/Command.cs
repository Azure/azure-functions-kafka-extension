using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command
{

    public interface Command<Type> : IDisposable
    {
        public Task<Type> ExecuteCommandAsync();
    }
}
