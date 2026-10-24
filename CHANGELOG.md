# Changelog

## 0.2.0

- Extracted AddressLibTool and AddressReferenceEditor into an Editor-only assembly without Odin.
- Added public group, asset registration, labels, folder scanning and code generation APIs.
- Added Inspector Build action and Editor usage documentation.
- Validate generated identifiers, duplicate keys, output ownership and asset references before mutation.
- Preserve unrelated labels and files; Resources entries use existing Resources assets.

## 0.1.0

- Extracted label loading, UI screen management and loading overlay APIs from QuickFramework.
- Removed project-specific dependencies and added standalone UPM metadata and usage samples.
- Added explicit Addressables instance ownership and scene load/unload API.
