# Deployment Guide

## Mintsafe.SaleWorker 
The SaleWorker is a BackgroundService that should be published as a single executable with the appropriate config files.

### Publish Single Executable
```
dotnet publish -r linux-x64 Src/SaleWorker/Mintsafe.SaleWorker.csproj -c Release -o release -p:PublishSingleFile=true --self-contained true
```

### Update Config 
Go to the `release` folder, update the `hostsettings.json` and `appsettings.{environment}.json` config files accordingly depending on your requirements.

### Transfer files to instance
```
scp -i ssh.pem release/* {USER}@{INSTANCE}:/home/{USER}/{FILES}
```

### Create Service File
```
touch mintsafe.saleworker.service
echo "[Unit]" >> mintsafe.saleworker.service
echo "Description=Mintsafe.SaleWorker Service TACF" >> mintsafe.saleworker.service
echo "" >> mintsafe.saleworker.service
echo "[Service]" >> mintsafe.saleworker.service
echo "WorkingDirectory=/home/user/mintsafe/saleworker" >> mintsafe.saleworker.service
echo "ExecStart=/home/user/mintsafe/saleworker/mintsafe.saleworker" >> mintsafe.saleworker.service
echo "" >> mintsafe.saleworker.service
echo "Restart=on-failure" >> mintsafe.saleworker.service
echo "RestartSec=10" >> mintsafe.saleworker.service
echo "KillSignal=SIGINT" >> mintsafe.saleworker.service
echo "RestartKillSignal=SIGINT" >> mintsafe.saleworker.service
echo "" >> mintsafe.saleworker.service
echo "StandardOutput=journal" >> mintsafe.saleworker.service
echo "StandardError=journal" >> mintsafe.saleworker.service
echo "SyslogIdentifier=mintsafe.saleworker.tacf" >> mintsafe.saleworker.service
echo "" >> mintsafe.saleworker.service
echo "User=ss" >> mintsafe.saleworker.service
echo "Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false" >> mintsafe.saleworker.service
echo "" >> mintsafe.saleworker.service
echo "[Install]" >> mintsafe.saleworker.service
echo "WantedBy=multi-user.target" >> mintsafe.saleworker.service
chmod 400 mintsafe.saleworker.service
sudo cp mintsafe.saleworker.service /etc/systemd/system/
```

### Start Service 
```
sudo systemctl start mintsafe.saleworker.service
sudo systemctl enable mintsafe.saleworker.service
sudo systemctl status mintsafe.saleworker.service
```