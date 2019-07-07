// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka listener configuration.
    /// </summary>
    public class KafkaListenerConfiguration
    {
        /// <summary>
        /// SASL mechanism to use for authentication. 
        /// Allowed values: Gssapi, Plain, ScramSha256, ScramSha512
        /// Default: Plain
        /// 
        /// sasl.mechanism in librdkafka
        /// </summary>
        public SaslMechanism? SaslMechanism { get; set; }

        /// <summary>
        /// SASL username for use with the PLAIN and SASL-SCRAM-.. mechanisms
        /// Default: ""
        /// 
        /// 'sasl.username' in librdkafka
        /// </summary>
        public string SaslUsername { get; set; }


        /// <summary>
        /// SASL password for use with the PLAIN and SASL-SCRAM-.. mechanism
        /// Default: ""
        /// 
        /// sasl.password in librdkafka
        /// </summary>
        public string SaslPassword { get; set; }


        /// <summary>
        /// Gets or sets the security protocol used to communicate with brokers
        /// Default is plain text
        /// 
        /// security.protocol in librdkafka
        /// </summary>
        public SecurityProtocol? SecurityProtocol { get; set; }

        /// <summary>
        /// Path to client's private key (PEM) used for authentication.
        /// Default: ""
        /// ssl.key.location in librdkafka
        /// </summary>
        public string SslKeyLocation { get; set; }

        /// <summary>
        /// Gets or sets the broker list.
        /// </summary>
        /// <value>The broker list.</value>
        public string BrokerList { get; set; }

        /// <summary>
        /// Gets or sets the event hub connection string.
        /// </summary>
        /// <value>The event hub connection string.</value>
        public string EventHubConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the topic.
        /// </summary>
        /// <value>The topic.</value>
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the consumer group.
        /// </summary>
        /// <value>The consumer group.</value>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Path to CA certificate file for verifying the broker's certificate.
        /// ssl.ca.location in librdkafka
        /// </summary>
        public string SslCaLocation { get; set; }

        /// <summary>
        /// Path to client's certificate.
        /// ssl.certificate.location in librdkafka
        /// </summary>
        public string SslCertificateLocation { get; set; }

        /// <summary>
        /// Password for client's certificate.
        /// ssl.key.password in librdkafka
        /// </summary>
        public string SslKeyPassword { get; set; }

        internal void ApplyToConfig(ClientConfig conf)
        {
            if (this.SaslMechanism.HasValue)
            {
                conf.SaslMechanism = this.SaslMechanism.Value;
            }

            if (!string.IsNullOrWhiteSpace(this.SaslUsername))
            {
                conf.SaslUsername = this.SaslUsername;
            }

            if (!string.IsNullOrWhiteSpace(this.SaslPassword))
            {
                conf.SaslPassword = this.SaslPassword;
            }

            if (this.SecurityProtocol.HasValue)
            {
                conf.SecurityProtocol = this.SecurityProtocol.Value;
            }
        }
    }
}
