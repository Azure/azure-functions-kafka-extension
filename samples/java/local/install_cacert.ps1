# Add CA certificate to the project.

$FunctionAppName = "kafka-function-20190419163130420"

# For windows uncomment this for using confluent cloud.
cp confluent_cloud_cacert.pem target\azure-functions\${FunctionAppName}