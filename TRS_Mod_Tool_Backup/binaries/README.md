# Binaries Directory

Place your compiled TRS mod tool binaries and dependencies here.

## Suggested Organization:

```
binaries/
├── main/                 # Main executable and core DLLs
├── plugins/              # Plugin DLLs and modules
├── redistributables/     # VC++ redistributables, DirectX runtime
├── tools/                # Utility executables
└── dependencies/         # Third-party libraries
```

## File Types to Include:
- Executable files (.exe)
- Dynamic libraries (.dll)
- Static libraries (.lib)
- DirectX 9 runtime files
- Visual C++ Redistributable installers
- Configuration executables

## Important Notes:
- These files can be large - consider essential backup for mobile
- Include version information for redistributables
- Test all binaries after restoration
- Some antivirus software may flag these files