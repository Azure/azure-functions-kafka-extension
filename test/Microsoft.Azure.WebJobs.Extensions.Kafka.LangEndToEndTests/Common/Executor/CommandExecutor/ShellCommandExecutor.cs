// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
    /* Executor for Shell Commands
    */
    public class ShellCommandExecutor : IExecutor<IInfraCommand<Process>, Process>
    {
        public Task<Process> ExecuteAsync(IInfraCommand<Process> request)
        {
           return request.ExecuteCommandAsync();
        }
    }
}
