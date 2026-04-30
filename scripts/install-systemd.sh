#!/bin/bash
# Installs net2o as systemd service

SERVICE_FILE="systemd/net2o.service"
INSTALL_PATH="/etc/systemd/system/"

# check if run as root
if [ "$EUID" -ne 0 ]; then 
    echo "please execute as root (sudo)"
    exit 1
fi

# install service
cp "$SERVICE_FILE" "$INSTALL_PATH"
systemctl daemon-reload
systemctl enable --now net2o.service

echo "Installed and started net2o DHT Service"
