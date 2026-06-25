#!/bin/bash
set -e

get_bundle_version_range() {
  if [ -n "$EXTENSION_BUNDLE_VERSION_RANGE" ]; then
    echo "$EXTENSION_BUNDLE_VERSION_RANGE"
    return
  fi

  local bundle_version="${EXTENSION_BUNDLE_VERSION:-4.3.2}"
  if [[ "$bundle_version" == \[* || "$bundle_version" == \(* ]]; then
    echo "$bundle_version"
    return
  fi

  IFS='.' read -r major minor patch <<< "$bundle_version"
  if ! [[ "$major" =~ ^[0-9]+$ && "$minor" =~ ^[0-9]+$ && "$patch" =~ ^[0-9]+$ ]]; then
    echo "Invalid EXTENSION_BUNDLE_VERSION '$bundle_version'. Use a semantic version like 4.37.0 or set EXTENSION_BUNDLE_VERSION_RANGE." >&2
    exit 1
  fi

  echo "[$major.$minor.$patch,$((major + 1)).0.0)"
}

if [ "$EXTENSION_SOURCE" = "bundle" ]; then
  bundle_version_range="$(get_bundle_version_range)"
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
    "version": "$bundle_version_range"
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