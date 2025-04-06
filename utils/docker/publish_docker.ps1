# PowerShell script to publish Docker images
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('stable', 'canary')]
    [string]$ReleaseChannel,
    
    [Parameter(Mandatory=$true)]
    [string]$DockerHubUsername
)

# Set error handling
$ErrorActionPreference = "Stop"

# Get the version from Version.props
$versionProps = Get-Content "../../src/Common/Version.props" -Raw
$versionMatch = [regex]::Match($versionProps, '<AssemblyVersion>(.*?)</AssemblyVersion>')
if (-not $versionMatch.Success) {
    Write-Error "Could not find AssemblyVersion in Version.props"
    exit 1
}
$PW_VERSION = $versionMatch.Groups[1].Value

# Validate release channel
if ($ReleaseChannel -eq "stable" -and $PW_VERSION -match "next") {
    Write-Error "ERROR: cannot publish stable docker with Playwright version '$PW_VERSION'"
    exit 1
}

# Define image tags
$MCR_IMAGE_NAME = "playwright-dotnet"
$REGISTRY = "${DockerHubUsername}"
$JAMMY_TAGS = @("v${PW_VERSION}-jammy")
$NOBLE_TAGS = @("v${PW_VERSION}", "v${PW_VERSION}-noble")

# Function to install ORAS if needed
function Install-OrasIfNeeded {
    if (Test-Path "oras/oras.exe") {
        return
    }
    $version = "1.1.0"
    $url = "https://github.com/oras-project/oras/releases/download/v${version}/oras_${version}_windows_amd64.zip"
    Invoke-WebRequest -Uri $url -OutFile "oras.zip"
    New-Item -ItemType Directory -Force -Path "oras"
    Expand-Archive -Path "oras.zip" -DestinationPath "oras"
    Remove-Item "oras.zip"
}

# Function to attach EOL manifest
function Attach-EolManifest {
    param([string]$image)
    $today = Get-Date -Format "yyyy-MM-dd"
    Install-OrasIfNeeded
    & "./oras/oras.exe" attach --artifact-type application/vnd.microsoft.artifact.lifecycle --annotation "vnd.microsoft.artifact.lifecycle.end-of-life.date=$today" $image
}

# Function to tag and push images
function Tag-AndPush {
    param(
        [string]$source,
        [string]$target
    )
    Write-Host "-- tagging: $target"
    docker tag $source $target
    docker push $target
    Attach-EolManifest $target
}

# Function to publish docker images with arch suffix
function Publish-DockerImagesWithArchSuffix {
    param(
        [string]$Flavor,
        [string]$Arch
    )
    
    $TAGS = @()
    if ($Flavor -eq "jammy") {
        $TAGS = $JAMMY_TAGS
    }
    elseif ($Flavor -eq "noble") {
        $TAGS = $NOBLE_TAGS
    }
    else {
        Write-Error "ERROR: unknown flavor - $Flavor. Must be either 'jammy' or 'noble'"
        exit 1
    }

    if ($Arch -ne "amd64" -and $Arch -ne "arm64") {
        Write-Error "ERROR: unknown arch - $Arch. Must be either 'amd64' or 'arm64'"
        exit 1
    }

    # Prune docker images to avoid platform conflicts
    docker system prune -fa
    & "$PSScriptRoot/build.ps1" "--$Arch" $Flavor "${MCR_IMAGE_NAME}:localbuild"

    foreach ($TAG in $TAGS) {
        Tag-AndPush "${MCR_IMAGE_NAME}:localbuild" "${REGISTRY}/${MCR_IMAGE_NAME}:${TAG}-${Arch}"
    }
}

# Function to publish docker manifest
function Publish-DockerManifest {
    param(
        [string]$Flavor,
        [string]$Arch1,
        [string]$Arch2
    )
    
    $TAGS = @()
    if ($Flavor -eq "jammy") {
        $TAGS = $JAMMY_TAGS
    }
    elseif ($Flavor -eq "noble") {
        $TAGS = $NOBLE_TAGS
    }
    else {
        Write-Error "ERROR: unknown flavor - $Flavor. Must be either 'jammy' or 'noble'"
        exit 1
    }

    foreach ($TAG in $TAGS) {
        $BASE_IMAGE_TAG = "${REGISTRY}/${MCR_IMAGE_NAME}:${TAG}"
        $IMAGE_NAMES = ""
        
        if ($Arch1 -eq "arm64" -or $Arch1 -eq "amd64") {
            $IMAGE_NAMES += " ${BASE_IMAGE_TAG}-$Arch1"
        }
        if ($Arch2 -eq "arm64" -or $Arch2 -eq "amd64") {
            $IMAGE_NAMES += " ${BASE_IMAGE_TAG}-$Arch2"
        }
        
        docker manifest create "${BASE_IMAGE_TAG}" $IMAGE_NAMES
        docker manifest push "${BASE_IMAGE_TAG}"
    }
}


# Publish images for both architectures
Publish-DockerImagesWithArchSuffix -Flavor "jammy" -Arch "amd64"
Publish-DockerImagesWithArchSuffix -Flavor "jammy" -Arch "arm64"
Publish-DockerManifest -Flavor "jammy" -Arch1 "amd64" -Arch2 "arm64"

Publish-DockerImagesWithArchSuffix -Flavor "noble" -Arch "amd64"
Publish-DockerImagesWithArchSuffix -Flavor "noble" -Arch "arm64"
Publish-DockerManifest -Flavor "noble" -Arch1 "amd64" -Arch2 "arm64" 