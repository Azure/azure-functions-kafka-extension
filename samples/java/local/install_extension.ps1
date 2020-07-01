# Install Kafka Extension for the target
# TODO remove installing Kafka Extension after the extension bundile for kafka is implemented.

$FunctionAppName = "kafka-function-20190419163130420"
$ExtensionVersion = "3.0.0"

# For windows uncomment this for using confluent cloud.
cp confluent_cloud_cacert.pem target\azure-functions\${FunctionAppName}

pushd . 
cd target\azure-functions\${FunctionAppName}
# If you want to install extension, put the nuget package on this directory and uncomment this line and comment out the second one.
# func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version ${ExtensionVersion} --source ..\..\.. --java
func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version ${ExtensionVersion}
popd 
