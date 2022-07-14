## Software Installation

Function apps for the test run in docker containers. Please install docker on the system.

## Additional Resources

For the tests to run end-to-end we need to precreate the following resources - 

Azure Resources: Create the following the azure resources in the same subscription
1. Resource Group: Name can be configured as RESOURCE_GROUP in Constants file
2. Eventhub Namespace: Name can be configured as EVENTHUB_NAMESPACE in Constants file   
3. Azure Storage Account: Create an Azure Storage Account
4. Azure Service Principle: Create Service Principle with access to above created eventhub namespace and azure storage account

External Resources -
5. Confluent Cluster: Create a confluent cluster with the following topics per languange
	a) e2e-kafka-{lang}-single-confluent 	
	b) e2e-kafka-{lang}-multi-confluent
	c) e2e-kafka-{lang}-single-eventhub
	d) e2e-kafka-{lang}-multi-eventhub

## Env Variables

Add the following environment variables, which will be picked up automatically provided to functions as environment variables during container startup - 

1. AZURE_CLIENT_ID: Service Principle Client Id
2. AZURE_CLIENT_SECRET: Service Principle Client Secret
3. AZURE_SUBSCRIPTION_ID: Susbcription Id for all azure resources
4. AZURE_TENANT_ID: Tenant Id for all azure resources
5. AzureWebJobsStorage: Connection String for Azure Storage Account
6. ConfluentBrokerList: Bootstrap Server for Confluent cluster
7. ConfluentCloudPassword: API Key for Confluent Cluster
8. ConfluentCloudUsername: API Key name for Confluent Cluster
9. EventHubBrokerList: Eventhub server url
10. EventHubConnectionString: Connection String for Azure Eventhub with Manage/Read/Write Permissions