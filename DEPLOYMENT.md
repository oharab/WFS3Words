# WFS3Words - IIS Deployment Guide

This guide provides detailed instructions for deploying WFS3Words to IIS on Windows Server.

## Prerequisites

### 1. Install .NET 8.0 Runtime on Windows Server

**Option A: Using .NET Hosting Bundle (Recommended)**
1. Download the ASP.NET Core 8.0 Hosting Bundle from:
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   ```
2. Look for "Hosting Bundle" under the "Run apps - Runtime" section
3. Run the installer (`dotnet-hosting-8.0.x-win.exe`)
4. Restart IIS after installation:
   ```powershell
   net stop was /y
   net start w3svc
   ```

**Option B: Using Individual Components**
1. Download and install:
   - .NET 8.0 Runtime (x64)
   - ASP.NET Core 8.0 Runtime (x64)
2. Restart IIS

**Verify Installation:**
```powershell
dotnet --list-runtimes
```
You should see both `Microsoft.NETCore.App 8.0.x` and `Microsoft.AspNetCore.App 8.0.x`

### 2. Enable IIS Features

Open PowerShell as Administrator and run:

```powershell
# Install IIS and required features
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
Install-WindowsFeature -Name Web-Asp-Net45
Install-WindowsFeature -Name Web-ISAPI-Ext
Install-WindowsFeature -Name Web-ISAPI-Filter
```

## Building and Publishing

### From Linux Development Machine

1. **Build the project:**
   ```bash
   ./build.sh
   ```

2. **Run tests (optional but recommended):**
   ```bash
   ./test.sh
   ```

3. **Publish for deployment:**
   ```bash
   ./publish.sh
   ```
   This creates a `publish/` directory with all required files.

4. **Transfer to Windows Server:**
   - Zip the `publish/` directory
   - Copy to your Windows Server (via FTP, RDP, network share, etc.)

### From Windows Development Machine

Using PowerShell or Command Prompt:

```powershell
# Build
dotnet build WFS3Words.sln --configuration Release

# Test (optional)
dotnet test

# Publish
dotnet publish src/WFS3Words.Api/WFS3Words.Api.csproj `
  --configuration Release `
  --output ./publish `
  --self-contained false
```

## IIS Configuration

### 1. Create Application Pool

1. Open **IIS Manager** (inetmgr)
2. Right-click **Application Pools** → **Add Application Pool**
3. Configure:
   - **Name:** `WFS3Words`
   - **.NET CLR Version:** `No Managed Code` (important!)
   - **Managed pipeline mode:** `Integrated`
4. Click **OK**
5. Select the new pool → **Advanced Settings:**
   - **Identity:** `ApplicationPoolIdentity` (or specific service account)
   - **Start Mode:** `AlwaysRunning` (optional, for better performance)
   - **Idle Time-out (minutes):** `0` (optional, prevents shutdown)

### 2. Create Website or Application

**Option A: New Website (Recommended for dedicated server)**

1. Right-click **Sites** → **Add Website**
2. Configure:
   - **Site name:** `WFS3Words`
   - **Application pool:** `WFS3Words`
   - **Physical path:** `C:\inetpub\WFS3Words` (or your chosen location)
   - **Binding:**
     - Type: `http` or `https`
     - IP address: `All Unassigned` or specific IP
     - Port: `80` (http) or `443` (https)
     - Host name: `wfs3words.yourdomain.com` (optional)

**Option B: Application under Default Website**

1. Right-click **Default Web Site** → **Add Application**
2. Configure:
   - **Alias:** `wfs3words`
   - **Application pool:** `WFS3Words`
   - **Physical path:** `C:\inetpub\wwwroot\wfs3words`

### 3. Deploy Application Files

1. Copy all files from `publish/` to the physical path (e.g., `C:\inetpub\WFS3Words`)
2. Ensure IIS has read permissions on the directory:
   ```powershell
   $path = "C:\inetpub\WFS3Words"
   $acl = Get-Acl $path
   $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
       "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
   $acl.AddAccessRule($rule)
   Set-Acl $path $acl
   ```

### 4. Configure Application Settings

1. **Edit appsettings.json** in the deployed directory:
   ```json
   {
     "What3Words": {
       "ApiKey": "YOUR_PRODUCTION_API_KEY"
     }
   }
   ```

2. **Alternative: Use Environment Variables (More Secure)**

   In IIS Manager:
   - Select your site/application
   - Click **Configuration Editor**
   - Section: `system.webServer/aspNetCore`
   - Click `environmentVariables` → **Edit Collection**
   - Add:
     - Name: `What3Words__ApiKey`
     - Value: `YOUR_PRODUCTION_API_KEY`

### 5. Configure web.config (Auto-generated, but verify)

The `web.config` should be auto-generated during publish. Verify it contains:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\WFS3Words.Api.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

**For logging (troubleshooting), temporarily set:**
```xml
stdoutLogEnabled="true"
```
Then create a `logs` folder in the application directory.

### 6. Start the Application

1. In IIS Manager, select your site
2. Click **Browse** on the right panel
3. Or navigate to: `http://localhost/` or your configured URL

## Testing the Deployment

### 1. Health Check
```powershell
Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing
```

### 2. WFS GetCapabilities
```powershell
Invoke-WebRequest -Uri "http://localhost/wfs?service=WFS&request=GetCapabilities" -UseBasicParsing
```

## Troubleshooting

### Common Issues

**1. HTTP Error 500.19 - Internal Server Error**
- **Cause:** ASP.NET Core Module not installed
- **Fix:** Install the .NET Hosting Bundle and restart IIS

**2. HTTP Error 500.30 - ANCM In-Process Start Failure**
- **Cause:** Runtime version mismatch or missing dependencies
- **Fix:**
  - Verify .NET 8.0 runtime is installed
  - Check Application Event Log for details
  - Enable stdout logging in web.config

**3. Application Pool Crashes**
- Enable stdout logging
- Check `logs\stdout_*.log` files
- Check Windows Event Viewer → Application logs

**4. Permission Denied Errors**
- Ensure `IIS_IUSRS` has read access to application folder
- If writing logs, ensure write permissions on logs folder

### Enable Detailed Logging

1. **Edit web.config:**
   ```xml
   <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
   ```

2. **Create logs folder:**
   ```powershell
   mkdir C:\inetpub\WFS3Words\logs
   icacls "C:\inetpub\WFS3Words\logs" /grant "IIS_IUSRS:(OI)(CI)M"
   ```

3. **Recycle application pool and retry**

4. **Check log files** in `logs\` directory

### Verify ASP.NET Core Module

```powershell
Get-WindowsFeature -Name Web-Server
Get-WindowsFeature -Name Web-Asp-Net45
```

Check IIS modules:
```powershell
Get-WebConfiguration -Filter '/system.webServer/globalModules/add[@name="AspNetCoreModuleV2"]'
```

## Security Best Practices

### 1. Use HTTPS
- Install SSL certificate
- Redirect HTTP to HTTPS
- Configure in IIS binding or use URL Rewrite

### 2. Secure API Key
- Store in environment variables (not appsettings.json)
- Use Azure Key Vault or similar for production
- Never commit API keys to source control

### 3. Configure Firewall
```powershell
New-NetFirewallRule -DisplayName "WFS3Words HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "WFS3Words HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

### 4. Restrict Access (Optional)
Configure IP restrictions in IIS if needed:
- Select site → **IP Address and Domain Restrictions**
- Add allowed/denied IP ranges

## Updating the Application

1. **Stop the application pool:**
   ```powershell
   Stop-WebAppPool -Name "WFS3Words"
   ```

2. **Replace files** in deployment directory

3. **Start the application pool:**
   ```powershell
   Start-WebAppPool -Name "WFS3Words"
   ```

Alternatively, use `app_offline.htm` for zero-downtime deployments.

## Monitoring and Maintenance

### Performance Counters
Monitor in Windows Performance Monitor:
- ASP.NET Core requests/sec
- Memory usage
- CPU usage

### Application Logs
- Check IIS logs: `C:\inetpub\logs\LogFiles\`
- Application logs via stdout (if enabled)
- Windows Event Viewer

### Health Monitoring
Set up automated health checks:
```powershell
# Example scheduled task
$url = "http://localhost/health"
$response = Invoke-WebRequest -Uri $url -UseBasicParsing
if ($response.StatusCode -ne 200) {
    # Send alert
}
```

## Additional Resources

- [Host ASP.NET Core on Windows with IIS](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [.NET Downloads](https://dotnet.microsoft.com/download)
- [What3Words API Documentation](https://developer.what3words.com/)
- [OGC WFS Standards](https://www.ogc.org/standards/wfs)
