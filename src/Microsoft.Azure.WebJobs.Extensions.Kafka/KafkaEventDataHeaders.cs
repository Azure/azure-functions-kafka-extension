// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventDataHeaders : IKafkaEventDataHeaders
    {
        List<IKafkaEventDataHeader> headers = new List<IKafkaEventDataHeader>();
        private readonly bool isReadOnly;

        internal static KafkaEventDataHeaders EmptyReadOnly { get; } = new KafkaEventDataHeaders(true);

        internal KafkaEventDataHeaders(bool isReadOnly = false)
        {
            this.isReadOnly = isReadOnly;
        }

        internal KafkaEventDataHeaders(Confluent.Kafka.Headers headers)
        {
            if (headers != null)
            {
                this.headers.AddRange(headers.Select(x => new KafkaEventDataHeader(x.Key, x.GetValueBytes())));
            }

            this.isReadOnly = true;
        }

        internal KafkaEventDataHeaders(IEnumerable<IKafkaEventDataHeader> headers, bool isReadOnly)
        {
            this.headers.AddRange(headers);
            this.isReadOnly = isReadOnly;
        }

        internal bool IsReadOnly => isReadOnly;

        public void Add(string key, byte[] value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ThrowIfReadOnly();
            headers.Add(new KafkaEventDataHeader(key, value));
        }

        public void Remove(string key)
        {
            ThrowIfReadOnly();
            headers.RemoveAll(x => x.Key == key);
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new NotSupportedException("The headers for an incoming KafkaEventData are readonly");
            }
        }

        public IEnumerator<IKafkaEventDataHeader> GetEnumerator() => headers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public byte[] GetFirst(string key)
        {
            if (TryGetFirst(key, out var output))
            {
                return output;
            }
            throw new KeyNotFoundException("No header with the specified key was found");
        }

        public bool TryGetFirst(string key, out byte[] output)
        {
            var header = headers.FirstOrDefault(x => x.Key == key);
            output = header?.Value;
            return output != null;
        }

        public byte[] GetLast(string key)
        {
            if (TryGetLast(key, out var output))
            {
                return output;
            }
            throw new KeyNotFoundException("No header with the specified key was found");
        }

        public bool TryGetLast(string key, out byte[] output)
        {
            var header = headers.LastOrDefault(x => x.Key == key);
            output = header?.Value;
            return output != null;
        }

        public int Count => headers.Count;
    }
}
