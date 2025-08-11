#!/bin/bash

# TRS Mod Tool Backup Script
# Creates a compressed backup optimized for mobile storage

echo "TRS Mod Tool Backup Creator"
echo "=========================="

# Configuration
BACKUP_DIR="TRS_Mod_Tool_Backup"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_NAME="trs_mod_backup_${TIMESTAMP}"

# Create backup types
echo "Select backup type:"
echo "1. Full backup (everything)"
echo "2. Essential backup (configs + docs + small samples)"
echo "3. Mobile backup (docs + configs only)"
read -p "Enter choice (1-3): " choice

case $choice in
    1)
        echo "Creating full backup..."
        zip -r "${BACKUP_NAME}_full.zip" $BACKUP_DIR/
        echo "Full backup created: ${BACKUP_NAME}_full.zip"
        ;;
    2)
        echo "Creating essential backup..."
        zip -r "${BACKUP_NAME}_essential.zip" \
            $BACKUP_DIR/documentation/ \
            $BACKUP_DIR/configs/ \
            $BACKUP_DIR/samples/ \
            $BACKUP_DIR/backup_scripts/ \
            TRS_MOD_TOOL_BACKUP.md
        echo "Essential backup created: ${BACKUP_NAME}_essential.zip"
        ;;
    3)
        echo "Creating mobile backup..."
        zip -r "${BACKUP_NAME}_mobile.zip" \
            $BACKUP_DIR/documentation/ \
            $BACKUP_DIR/configs/ \
            TRS_MOD_TOOL_BACKUP.md
        echo "Mobile backup created: ${BACKUP_NAME}_mobile.zip"
        ;;
    *)
        echo "Invalid choice. Exiting."
        exit 1
        ;;
esac

# Show file sizes
echo ""
echo "Backup file sizes:"
ls -lh *${TIMESTAMP}*.zip 2>/dev/null

echo ""
echo "Backup complete! Transfer files to your phone via:"
echo "- USB cable"
echo "- Cloud storage (Google Drive, Dropbox, etc.)"
echo "- Email (for smaller backups)"
echo "- Bluetooth/WiFi Direct"