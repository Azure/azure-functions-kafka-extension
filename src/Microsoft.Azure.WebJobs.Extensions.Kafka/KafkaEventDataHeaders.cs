// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventDataHeaders : IKafkaEventDataHeaders, IEnumerable<IKafkaEventDataHeader>
    {
        List<IKafkaEventDataHeader> headers = new List<IKafkaEventDataHeader>();

        public KafkaEventDataHeaders()
        {
        }

        public KafkaEventDataHeaders(Confluent.Kafka.Headers headers)
        {
            if (headers != null)
            {
                this.headers.AddRange(headers.Select(x=>new KafkaEventDataHeader(x.Key, x.GetValueBytes())));
            }
        }

        public KafkaEventDataHeaders(IEnumerable<IKafkaEventDataHeader> headers)
        {
            this.headers.AddRange(headers);
        }
        public void Add(string key, byte[] value)
        {
            headers.Add(new KafkaEventDataHeader(key, value));
        }

        public IEnumerator<IKafkaEventDataHeader> GetEnumerator() => headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => headers.Count;
    }
}
