using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command
{
    //Why Disposable?
    //Command<HttpResponse>
    public interface Command<Type> : IDisposable
    {
        //Return HttpResponse
        public Task<Type> ExecuteCommandAsync();
    }
}
