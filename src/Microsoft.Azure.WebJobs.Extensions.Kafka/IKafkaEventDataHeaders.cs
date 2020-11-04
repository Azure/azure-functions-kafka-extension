// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IKafkaEventDataHeaders : IEnumerable<IKafkaEventDataHeader>
    {
        /// <summary>
        /// Adds a header with a specified key and value
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        void Add(string key, byte[] value);

        /// <summary>
        /// Removes all headers with the specified key
        /// </summary>
        /// <param name="key">The key of the header(s)</param>
        void Remove(string key);
        
        /// <summary>
        /// Returns the number of headers
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the byte array from the first header with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] GetFirst(string key);

        /// <summary>
        /// Tries to get the byte array from the first header with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        bool TryGetFirst(string key, out byte[] output);

        /// <summary>
        /// Gets the byte array from the last header with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        byte[] GetLast(string key);

        /// <summary>
        /// Tries to get the byte array from the last header with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        bool TryGetLast(string key, out byte[] output);
    }
}