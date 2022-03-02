using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue
{
    public class QueueCommand : Command<String>, IDisposable
    {
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public String ExecuteCommand()
        {
            throw new NotImplementedException();
        }
    }
}
