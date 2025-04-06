# PowerShell script to set up Playwright environment
# Equivalent to the Dockerfile setup

# Set environment variables
$env:PLAYWRIGHT_BROWSERS_PATH = "C:\ms-playwright"
$env:DEBIAN_FRONTEND = "noninteractive"
$env:TZ = "America/Los_Angeles"

# Create pwuser (if running as administrator)
if (-not (Get-LocalUser -Name "pwuser" -ErrorAction SilentlyContinue)) {
    New-LocalUser -Name "pwuser" -Password (ConvertTo-SecureString "Playwright123!" -AsPlainText -Force)
    Add-LocalGroupMember -Group "Users" -Member "pwuser"
}

# Create ms-playwright directory
New-Item -ItemType Directory -Force -Path $env:PLAYWRIGHT_BROWSERS_PATH

# Copy the Playwright package
Copy-Item -Path ".\dist\*" -Destination "C:\tmp\playwright-dotnet" -Recurse -Force

# Install Playwright browsers and dependencies
& "C:\tmp\playwright-dotnet\playwright.ps1" install --with-deps

# Mark the Docker image (if needed)
# & "C:\tmp\playwright-dotnet\playwright.ps1" mark-docker-image "your-repository/playwright-dotnet:latest"

# Set permissions for the ms-playwright directory
$acl = Get-Acl $env:PLAYWRIGHT_BROWSERS_PATH
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
Set-Acl $env:PLAYWRIGHT_BROWSERS_PATH $acl

# Cleanup
Remove-Item -Path "C:\tmp" -Recurse -Force

Write-Host "Playwright environment setup completed successfully!" 