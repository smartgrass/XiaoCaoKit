# Shader Variants Collector

A Unity editor plugin for managing and collecting shader variants into ShaderVariantCollection.

## Features

### Main Features
- **External Plugin**: Can be published as a standalone plugin to Unity Asset Store
- **Visual Interface**: Provides an intuitive UI for operations
- **TreeView Display**: Uses TreeView to display shaders and variants in ShaderVariantCollection
- **Variant Collection**: Automatically collects variants not included in ShaderVariantCollection from the project
- **Progress Display**: Shows progress bar during collection process
- **New Variant Management**: Newly collected variants can be individually confirmed or ignored
- **Batch Operations**: Supports "Confirm All" and "Ignore All" operations

### Manual Addition Features
- **Add Shader**: Manually add new shaders to the collection
- **Add Variant**: Add new keyword combination variants to existing shaders
- **Keyword Selection**: Provides keyword selection interface

### Reference Finding Features
- **Shader Indexing**: Click on shader to locate the shader file
- **Variant References**: Shows which materials and prefabs reference the variants

### Deletion Features
- **Delete Shader**: Delete entire shader and all its variants
- **Delete Variant**: Delete specific variants (keyword combinations)

## Installation

### Method 1: Install as Unity Package
1. Copy the plugin folder to the `Assets/` directory of your Unity project
2. Restart Unity Editor
3. Select `Tools > Shader Variants Collector` from the menu bar to open the plugin window

### Method 2: Install via Package Manager
1. Click the "+" button in Package Manager
2. Select "Add package from disk"
3. Select the plugin's package.json file
4. Click "Add" to install

## Usage

### Basic Operations
1. **Select ShaderVariantCollection**: Select the ShaderVariantCollection asset to manage in the plugin window
2. **View Existing Variants**: TreeView will display all shaders and variants in the current collection
3. **Collect New Variants**: Click the "Collect Variants" button to start collecting new variants from the project
4. **Manage New Variants**: New variants will be displayed with green background and can be individually confirmed or ignored

### Manual Addition
1. **Add Shader**: Click the "Add" button at the bottom of TreeView
2. **Add Variant**: Click the "Add" button under each shader
3. **Select Keywords**: Check the required keyword combinations in the dialog

### Deletion Operations
1. **Delete Shader**: Click the "Del" button on the right side of the shader row
2. **Delete Variant**: Click the "Del" button on the right side of the variant row
3. **Confirm Deletion**: Confirm the deletion in the popup dialog

### Reference Finding
1. **Locate Shader**: Click the "Locate" button on the right side of the shader row
2. **View References**: Click the "Refs" button on the right side of the variant row to view reference information

## Technical Implementation

### Core Components
- `ShaderVariantsCollectorWindow`: Main window class
- `ShaderVariantsTreeView`: TreeView display component
- `ShaderVariantData`: Variant data model
- `ShaderVariantCollector`: Collection utility class
- `AddShaderDialog`: Add shader dialog
- `AddVariantDialog`: Add variant dialog

### Key Technologies
- **Reflection**: Uses reflection to access internal data of ShaderVariantCollection
- **TreeView**: Custom TreeView implementation for hierarchical display
- **Progress Bar**: Real-time collection progress display
- **Reference Finding**: Asset reference finding through AssetDatabase

## Notes

1. **Performance Considerations**: Collecting large numbers of variants may take considerable time
2. **Memory Usage**: Pay attention to memory usage when processing large projects
3. **API Limitations**: Some features may be limited by Unity API version
4. **Backup Recommendation**: It's recommended to backup ShaderVariantCollection assets before operations

## Version Information

- **Version**: 1.0.0
- **Unity Version**: 2021.3+
- **License**: MIT

## Changelog

### v1.0.0
- Initial release
- Implemented basic variant collection and management features
- Support for manual addition of shaders and variants
- Provided reference finding functionality
- Support for deleting shaders and variants
- Complete UI interface and interaction features

## Technical Support

For questions or suggestions, please contact the developer or submit an Issue.

## License

This plugin is licensed under the MIT License. See the LICENSE file for details.