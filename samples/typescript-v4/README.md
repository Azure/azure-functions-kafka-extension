# Developers guide for Kafka Functions for TypeScript

TypeScript is almost the same as [JavaScript](../javascript-v4/README.md). Please refer the [JavaScript](../javascript-v4/README.md). In the TypeScript sample, we don't provide confluent cloud sample. The configuration is the same as JavaScript.

## Samples

We provide two samples.

| Name             | Description                   | Kafka Cluster | Enabled |
| ---------------- | ----------------------------- | ------------- | ------- |
| KafkaTrigger     | Simple Kafka trigger sample   | local         | yes     |
| KafkaTriggerMany | Kafka batch processing sample | local         | yes     |

## Difference between Javascript and TypeScript

You need to compile your app before start Azure Functions Core Tools. Refer to the [Functions Documentation: TypeScript](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node#typescript) documentation for more details.

```bash
$ npm install
$ npm run build
```

## Reference

- [Quickstart: Create a function in Azure using Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code?pivots=programming-language-typescript)
- [Quickstart: Create a function in Azure that responds to HTTP requests](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli?tabs=bash%2Cbrowser&pivots=programming-language-typescript)
- [Functions Documentation: TypeScript](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-node#typescript)
