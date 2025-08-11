# Troubleshooting Guide - TRS Mod Tool

## Common DirectX 9 Issues

### Problem: "DirectX 9 not detected"
**Solution:**
1. Download DirectX End-User Runtime from Microsoft
2. Install even if you have DirectX 11/12 (DX9 is separate)
3. Restart computer after installation

### Problem: "Application failed to start (0xc000007b)"
**Solution:**
1. Install Visual C++ Redistributable 2008, 2010, 2012 (32-bit versions)
2. Check if all DLLs are present in System32
3. Run application as Administrator

### Problem: "Shader compilation errors"
**Solution:**
1. Update graphics drivers
2. Check if GPU supports Shader Model 3.0
3. Verify HLSL compiler is accessible
4. Use fallback shaders from samples/

## 32-bit Compatibility Issues

### Problem: "Program won't run on 64-bit Windows"
**Solution:**
1. Ensure 32-bit application support is enabled
2. Install 32-bit versions of all dependencies
3. Use WoW64 compatibility layer
4. Check DEP (Data Execution Prevention) settings

### Problem: "Out of memory errors"
**Solution:**
1. Close other applications
2. Use /3GB boot flag on 32-bit Windows
3. Optimize texture sizes in config
4. Enable virtual memory paging

## File and Permission Issues

### Problem: "Access denied when saving"
**Solution:**
1. Run as Administrator
2. Check folder permissions
3. Disable UAC temporarily
4. Move project to user folder instead of Program Files

### Problem: "Textures not loading"
**Solution:**
1. Check file paths in config
2. Verify texture format compatibility
3. Convert textures to DDS format
4. Check file size limits

## Performance Issues

### Problem: "Tool runs slowly"
**Solution:**
1. Reduce preview quality in settings
2. Disable real-time rendering
3. Use lower texture resolutions
4. Close unnecessary background apps

### Problem: "Frequent crashes"
**Solution:**
1. Check system RAM (32-bit apps limited to 4GB)
2. Update to latest stable version
3. Disable GPU acceleration if unstable
4. Run memory diagnostic tools

## Mobile Backup Issues

### Problem: "Backup too large for phone"
**Solution:**
1. Use mobile backup option (configs + docs only)
2. Upload to cloud storage instead
3. Split backup into smaller parts
4. Remove unnecessary assets before backup

### Problem: "Can't open files on phone"
**Solution:**
1. Install appropriate apps (text editors, zip extractors)
2. Use cloud storage with mobile apps
3. Convert documentation to phone-friendly formats
4. Take screenshots of important info

## Emergency Recovery

### If everything fails:
1. Use restore points if available
2. Check documentation/RESTORE_GUIDE.md
3. Contact support with error logs
4. Use backup from previous working state

### Important Logs:
- Windows Event Viewer → Application logs
- DirectX diagnostic: `dxdiag /t dxdiag.txt`
- Tool logs: Usually in AppData\Local\TRS_ModTool\logs\