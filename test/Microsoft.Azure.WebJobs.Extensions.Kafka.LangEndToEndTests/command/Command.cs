using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command
{
    public interface Command<Type> : IDisposable
    {
        public Type ExecuteCommand();
    }
}
