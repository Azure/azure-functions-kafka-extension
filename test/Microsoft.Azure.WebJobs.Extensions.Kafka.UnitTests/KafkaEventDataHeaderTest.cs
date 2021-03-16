// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaEventDataHeaderTest
    {
        [Fact]
        public void Throws_On_Null_Key()
        {
            Assert.Throws<ArgumentNullException>(() => new KafkaEventDataHeader(null, null));
        }
    }
}
