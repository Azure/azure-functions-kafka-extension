#!/bin/bash
set -e

if [ "$EXTENSION_SOURCE" = "bundle" ]; then
  cat > host.json <<EOF
{
  "version": "2.0",
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
  "extensions": {
    "kafka": {
      "maxBatchSize": 3
    }
  }
}
EOF
fi

if [ -n "$TARGET_DIR" ] && [ -d "$TARGET_DIR" ]; then
  cp host.json "$TARGET_DIR/host.json"
fi

mvn azure-functions:run