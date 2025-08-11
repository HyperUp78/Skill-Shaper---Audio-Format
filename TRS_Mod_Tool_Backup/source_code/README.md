# Source Code Directory

Place your TRS mod tool source code files here.

## Suggested Organization:

```
source_code/
├── main/                 # Main application source
├── plugins/              # Plugin source code
├── shaders/              # HLSL shader source files
├── scripts/              # Build and utility scripts
└── third_party/          # External libraries and dependencies
```

## File Types to Include:
- C/C++ source files (.cpp, .c, .h, .hpp)
- DirectX 9 shader files (.hlsl, .fx)
- Build scripts (.bat, .sh, Makefile)
- Project files (.vcproj, .sln)
- Resource files (.rc, .resx)

## Notes:
- Keep original directory structure when possible
- Include build instructions in documentation/
- Consider using version control for source code
- Large binary dependencies can go in binaries/ instead