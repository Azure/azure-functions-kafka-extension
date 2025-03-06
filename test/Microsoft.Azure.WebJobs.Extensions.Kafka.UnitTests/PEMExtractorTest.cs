// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.Helpers;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Config;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class PEMExtractorTest
    {
        public class ExtractCertificateTestCase(string inputPem, string outputPem, string testDescription)
        {
            public string InputPem = inputPem;
            public string OutputPem = outputPem;
            public string TestDescription = testDescription;
        }

        public static IEnumerable<object[]> ExtractCertificateTestData()
        {
            var testCases = new List<ExtractCertificateTestCase> {
                new (
                    inputPem: """
-----BEGIN CERTIFICATE-----
dummyCertificateData
-----END CERTIFICATE-----
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyCertificateData
-----END CERTIFICATE-----
""",
                    testDescription: "Single certificate"

                ),
                new (
                    inputPem: """
-----BEGIN CERTIFICATE-----
dummyCertificateData1
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyCertificateData2
-----END CERTIFICATE-----
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyCertificateData1
-----END CERTIFICATE-----
""",
                    testDescription: "Two certificates"

                ),
                new (
                    inputPem: """
Bogus:
    foobar: baz
-----BEGIN CERTIFICATE-----
dummyCertificateData
-----END CERTIFICATE-----
moreBogus:
    foobar: baz
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyCertificateData
-----END CERTIFICATE-----
""",
                    testDescription: "Single certificate with bogus"

                ),
                new (
                    inputPem: """
-----BEGIN FOO-----
bar
-----END FOO-----
""",
                    outputPem: null,
                    testDescription: "Wrong section name"

                ),
            };

            return testCases.Select(tc => new object[] { tc }).ToList();
        }

        [Theory]
        [MemberData(nameof(ExtractCertificateTestData))]
        public void ExtractCertificate(ExtractCertificateTestCase testCase)
        {
            var result = PEMExtractor.ExtractCertificate(testCase.InputPem);
            Assert.Equal(testCase.OutputPem, result);
        }

        public class ExtractAllCertificatesTestCase(string inputPem, string outputPem, string testDescription)
        {
            public string InputPem = inputPem;
            public string OutputPem = outputPem;
            public string TestDescription = testDescription;
        }

        public static IEnumerable<object[]> ExtractAllCertificatesTestData()
        {
            var testCases = new List<ExtractAllCertificatesTestCase> {
                new (
                    inputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
""",
                    testDescription: "Single certificate"

                ),
                new (
                    inputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
""",
                    testDescription: "Two certificates in chain"

                ),
                new (
                    inputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCa1Cert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyIntermediateCa2Cert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCa1Cert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyIntermediateCa2Cert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
""",
                    testDescription: "Three certificates in chain"
                ),
                new (
                    inputPem: """
Bogus:
    foobar: baz
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
moreBogus:
    foobar: baz
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
""",
                    testDescription: "Single certificate with bogus"

                ),
                new (
                    inputPem: """
Bogus:
    foobar: baz
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
Bogus:
    foobar: baz
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
Bogus:
    foobar: baz
""",
                    outputPem: """
-----BEGIN CERTIFICATE-----
dummyIntermediateCaCert
-----END CERTIFICATE-----
-----BEGIN CERTIFICATE-----
dummyRootCaCert
-----END CERTIFICATE-----
""",
                    testDescription: "Two certificates in chain with bogus"
                ),
                new (
                    inputPem: """
-----BEGIN FOO-----
bar
-----END FOO-----
""",
                    outputPem: null,
                    testDescription: "Wrong section name"

                ),
            };

            return testCases.Select(tc => new object[] { tc }).ToList();
        }

        [Theory]
        [MemberData(nameof(ExtractAllCertificatesTestData))]
        public void ExtractAllCertificates(ExtractAllCertificatesTestCase testCase)
        {
            var result = PEMExtractor.ExtractAllCertificates(testCase.InputPem);
            Assert.Equal(testCase.OutputPem, result);
        }
    }
}