using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    /* Collection of Supported Shell Commands.
    */
    public enum ShellCommandType
    {
        DOCKER_RUN, DOCKER_KILL, FUNC_START
    }
}
