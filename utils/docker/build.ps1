# PowerShell script to build Playwright docker image
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('--arm64', '--amd64')]
    [string]$Platform,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet('jammy', 'noble')]
    [string]$Flavor,
    
    [Parameter(Mandatory=$true)]
    [string]$ImageTag
)

# Function to clean up dist directory
function Cleanup {
    if (Test-Path "dist") {
        Remove-Item -Path "dist" -Recurse -Force
    }
}

# Set up error handling and cleanup
$ErrorActionPreference = "Stop"
$originalLocation = Get-Location
try {
    # Change to script directory
    Set-Location $PSScriptRoot

    # Determine platform settings
    $dockerPlatform = ""
    $dotnetArch = ""
    switch ($Platform) {
        "--arm64" { 
            $dockerPlatform = "linux/arm64"
            $dotnetArch = "linux-arm64"
        }
        "--amd64" { 
            $dockerPlatform = "linux/amd64"
            $dotnetArch = "linux-amd64"
        }
        default {
            Write-Error "ERROR: unknown platform specifier - $Platform. Only --arm64 or --amd64 is supported"
            exit 1
        }
    }

    # Publish dotnet project
    dotnet publish ../../src/Playwright -o dist/ --arch $dotnetArch

    # Build docker image
    docker build --progress=plain --platform $dockerPlatform -t $ImageTag -f "Dockerfile.$Flavor" .
}
finally {
    # Cleanup and restore original location
    Cleanup
    Set-Location $originalLocation
} 