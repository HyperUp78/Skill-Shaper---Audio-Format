# Restoration Guide - TRS Mod Tool

## Prerequisites

### System Requirements
- Windows OS with 32-bit application support
- DirectX 9.0c runtime installed
- Visual C++ 2008/2010/2012 Redistributable (32-bit)
- At least 2GB free disk space

### Before Restoring
1. Check DirectX version: `dxdiag` → Display tab
2. Verify 32-bit support is enabled on 64-bit systems
3. Disable antivirus temporarily (some tools may be flagged)

## Restoration Steps

### Step 1: Extract Backup
```bash
# If using compressed backup
unzip trs_mod_tool_backup.zip -d C:\TRS_ModTool\
# Or extract to preferred location
```

### Step 2: Install Dependencies
1. Copy all files from `binaries/redistributables/` to Windows\System32
2. Register DLLs: Run `backup_scripts/register_dlls.bat` as Administrator
3. Install DirectX 9 runtime if missing

### Step 3: Configure Environment
1. Copy `configs/default_settings.ini` to your user profile
2. Update paths in configuration files
3. Run `tools/environment_setup.exe` to validate setup

### Step 4: Test Installation
1. Run `binaries/trs_mod_tool.exe`
2. Load sample project from `samples/basic_mod/`
3. Verify DirectX 9 rendering works correctly

## Troubleshooting

### Common Issues
- **"DirectX not found"**: Install DirectX 9.0c end-user runtime
- **"DLL missing"**: Run `backup_scripts/dll_checker.bat`
- **"Access denied"**: Run as Administrator or check file permissions
- **"Shader compilation failed"**: Update graphics drivers

### File Associations
Run `backup_scripts/setup_file_associations.reg` to restore .trs file associations.

## Mobile Backup Strategy

### What to Keep on Phone
- `documentation/` folder (guides and references)
- `configs/` folder (settings and presets)
- `samples/basic_mod/` (small example project)
- Key screenshots of working tool

### What to Store Elsewhere
- Full `binaries/` folder (can be large)
- Complete `assets/` folder (textures/models are big)
- `source_code/` if available elsewhere

## Verification Checklist
- [ ] Tool launches without errors
- [ ] Can load sample project
- [ ] DirectX 9 rendering works
- [ ] Can export basic mod
- [ ] File associations work
- [ ] All hotkeys respond correctly