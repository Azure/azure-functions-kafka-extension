// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command
{
    /* Interface for all executable commands.
    */
    public interface Command<Type> : IDisposable
    {
        public Task<Type> ExecuteCommandAsync();
    }
}
