#!/bin/bash
set -e

if [ "$EXTENSION_SOURCE" = "bundle" ]; then
	cat > host.json <<EOF
{
	"version": "2.0",
	"logging": {
		"applicationInsights": {
			"samplingSettings": {
				"isEnabled": true,
				"excludedTypes": "Request"
			}
		}
	},
	"extensionBundle": {
		"id": "Microsoft.Azure.Functions.ExtensionBundle",
		"version": "[$EXTENSION_BUNDLE_VERSION]"
	},
	"extensions": {
		"kafka": {
			"maxBatchSize": 3
		}
	}
}
EOF
else
	cat > host.json <<EOF
{
	"version": "2.0",
	"logging": {
		"applicationInsights": {
			"samplingSettings": {
				"isEnabled": true,
				"excludedTypes": "Request"
			}
		}
	},
	"extensions": {
		"kafka": {
			"maxBatchSize": 3
		}
	}
}
EOF
fi

func start