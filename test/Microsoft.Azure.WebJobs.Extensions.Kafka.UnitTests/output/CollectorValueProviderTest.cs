// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.output
{
    public class CollectorValueProviderTest
    { 
        [Fact]
        public void SetValueAsyncTest_Null()
        {
            var kafkaProducerEntity = new Mock<KafkaProducerEntity>();
            IAsyncCollector<string> asyncCollector = new KafkaProducerAsyncCollector<string>(kafkaProducerEntity.Object, Guid.NewGuid());
            CollectorValueProvider cvp = new CollectorValueProvider(kafkaProducerEntity.Object, asyncCollector, typeof(IAsyncCollector<string>));
            Task task = cvp.SetValueAsync(null, CancellationToken.None);
            Assert.True(task.IsFaulted);
        }
    }
}
