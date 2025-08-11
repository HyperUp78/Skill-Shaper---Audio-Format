# Basic TRS Mod Sample Project

This is a minimal example project to test your TRS mod tool installation.

## Project Structure
```
basic_mod/
├── project.trs          # Main project file
├── assets/
│   ├── texture.dds      # Sample texture
│   └── model.x          # Sample DirectX model
├── scripts/
│   └── mod_script.lua   # Sample modification script
└── config.cfg           # Project-specific configuration
```

## Testing Instructions

1. **Load Project**: Open project.trs in TRS Mod Tool
2. **Verify Assets**: Check that texture.dds and model.x load properly
3. **Test Script**: Run mod_script.lua to verify scripting works
4. **Export Test**: Try exporting the project as a .mod file

## Expected Behavior

- Project should load without errors
- Texture should display in preview window
- Model should render with DirectX 9
- Script should execute and show "Hello World" message
- Export should create a working .mod file

## Troubleshooting

If this sample doesn't work:
1. Check DirectX 9 installation
2. Verify 32-bit compatibility
3. Review troubleshooting guide
4. Check system requirements

This sample uses minimal resources and should work on any system that supports the TRS mod tool.