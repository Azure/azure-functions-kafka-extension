// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Config;

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

        /// <summary>
        /// Client certificate in PEM format.
        /// ssl.certificate.pem in librdkafka
        /// </summary>
        public string SslCertificatePEM { get; set; }

        /// <summary>
        /// Client Private Key in PEM format.
        /// ssl.key.pem in librdkafka
        /// </summary>
        public string SslKeyPEM { get; set; }

        /// <summary>
        /// Client certificate for verifying the broker's certificate in PEM format.
        /// ssl.ca.pem in librdkafka.
        /// </summary>
        public string SslCaPEM { get; set; }

        /// <summary>
        /// Client certificate and key in PEM format.
        /// Additional Configuration for extension as KeyVault supports uploading certificate only with private key.
        /// </summary>
        public string SslCertificateandKeyPEM { get; set; }

        /// <summary>
        /// Lag threshold
        /// Default: 1000
        /// </summary>
        public long LagThreshold { get; set; }

        /// <summary>
        /// OAuth Bearer method.
        /// Either 'default' or 'oidc'
        /// sasl.oauthbearer in librdkafka
        /// </summary>
        public SaslOauthbearerMethod SaslOAuthBearerMethod { get; set; }

        /// <summary>
        /// OAuth Bearer Client Id
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.client.id in librdkafka
        /// </summary>
        public string SaslOAuthBearerClientId { get; set; }

        /// <summary>
        /// OAuth Bearer Client Secret
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.client.secret in librdkafka
        /// </summary>
        public string SaslOAuthBearerClientSecret { get; set; }

        /// <summary>
        /// OAuth Bearer scope.
        /// Client use this to specify the scope of the access request to the broker. 
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.extensions in librdkafka
        /// </summary>
        public string SaslOAuthBearerScope { get; set; }

        /// <summary>
        /// OAuth Bearer token endpoint url.
        /// Specify only when OAuthBearerMethod is 'oidc'
        /// sasl.oauthbearer.token.endpoint.url in librdkafka
        /// </summary>
        public string SaslOAuthBearerTokenEndpointUrl { get; set; }

        /// <summary>
        /// OAuth Bearer extensions.
        /// Allow additional information to be provided to the broker.
        /// Comma-separated list of key=value pairs. E.g., "supportFeatureX=true,organizationId=sales-emea"
        /// sasl.oauthbearer.extensions in librdkafka
        /// </summary>
        public string SaslOAuthBearerExtensions { get; set; }


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
