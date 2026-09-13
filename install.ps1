# Installs spg (strong password generator) for the current user - no admin rights needed:
#
#   irm https://github.com/malikmizery/strong-password-generator/releases/latest/download/install.ps1 | iex
#
# Downloads spg.exe from the latest GitHub release, checks it against the release's SHA256SUMS.txt,
# puts it in %USERPROFILE%\.local\bin and adds that folder to the user PATH. Run again to update.
#   SPG_INSTALL_DIR=<dir>   install somewhere else
#   SPG_NO_MODIFY_PATH=1    leave PATH untouched
#
# Keep this file ASCII-only: Windows PowerShell 5.1 misreads non-ASCII in scripts without a BOM.

function Get-SpgUpdatedPath {
    # Returns $CurrentPath with $Directory appended, or $null when an entry already points there
    # (compared case-insensitively, after expanding %VARS% and ignoring trailing backslashes).
    param([string]$CurrentPath, [string]$Directory)

    $target = $Directory.TrimEnd('\')
    foreach ($entry in $CurrentPath -split ';') {
        if ([Environment]::ExpandEnvironmentVariables($entry).TrimEnd('\') -eq $target) { return $null }
    }

    $existing = $CurrentPath.TrimEnd(';')
    if ($existing) { return "$existing;$Directory" }
    return $Directory
}

function Assert-SpgChecksum {
    # $Sums is sha256sum-style text: one "<hex hash>  <file name>" per line.
    param([string]$File, [string]$Sums)

    $name = Split-Path -Path $File -Leaf
    $expected = $null
    foreach ($line in $Sums -split "`r?`n") {
        $parts = $line.Trim() -split '\s+', 2
        if ($parts.Count -eq 2 -and $parts[1].TrimStart('*') -eq $name) {
            $expected = $parts[0]
            break
        }
    }
    if (-not $expected) { throw "SHA256SUMS.txt has no entry for $name." }

    $actual = (Get-FileHash -Path $File -Algorithm SHA256).Hash
    if ($actual -ne $expected) {
        throw "SHA256 checksum mismatch for $name (expected $expected, got $actual). The download may be corrupted; nothing was installed."
    }
}

function Get-SpgUserPath {
    # Read the raw registry value so entries such as %USERPROFILE%\bin stay unexpanded when written back.
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment')
    try { return [string]$key.GetValue('Path', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames) }
    finally { $key.Dispose() }
}

function Set-SpgUserPath {
    param([string]$Value)

    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
    try { $key.SetValue('Path', $Value, [Microsoft.Win32.RegistryValueKind]::ExpandString) }
    finally { $key.Dispose() }

    # Setting a user variable through .NET broadcasts WM_SETTINGCHANGE, so terminals opened from now on see the new PATH.
    [Environment]::SetEnvironmentVariable('SPG_INSTALL_REFRESH', '1', 'User')
    [Environment]::SetEnvironmentVariable('SPG_INSTALL_REFRESH', $null, 'User')
}

function Install-Spg {
    param(
        [string]$InstallDir = $(if ($env:SPG_INSTALL_DIR) { $env:SPG_INSTALL_DIR } else { Join-Path $env:USERPROFILE '.local\bin' }),
        [switch]$NoModifyPath = ($env:SPG_NO_MODIFY_PATH -eq '1')
    )

    # Function-scoped, so these don't leak into the session that ran `irm | iex`.
    $ErrorActionPreference = 'Stop'
    $ProgressPreference = 'SilentlyContinue'  # the progress bar makes downloads very slow on Windows PowerShell 5.1
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $repo = 'malikmizery/strong-password-generator'
    # Resolve the tag once so spg.exe and its checksum come from the same release.
    $tag = (Invoke-RestMethod -Uri "https://api.github.com/repos/$repo/releases/latest" -UseBasicParsing).tag_name
    $baseUrl = "https://github.com/$repo/releases/download/$tag"

    Write-Host "Downloading spg.exe ($tag)..."
    $tempDir = Join-Path ([IO.Path]::GetTempPath()) ('spg-install-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $tempDir | Out-Null
    try {
        $exe = Join-Path $tempDir 'spg.exe'
        $sums = Join-Path $tempDir 'SHA256SUMS.txt'
        Invoke-WebRequest -Uri "$baseUrl/spg.exe" -OutFile $exe -UseBasicParsing
        Invoke-WebRequest -Uri "$baseUrl/SHA256SUMS.txt" -OutFile $sums -UseBasicParsing
        Assert-SpgChecksum -File $exe -Sums (Get-Content -Path $sums -Raw)
        Write-Host 'SHA256 verified.'

        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
        $target = Join-Path $InstallDir 'spg.exe'
        Move-Item -Path $exe -Destination $target -Force
        Write-Host "Installed: $target"
    }
    finally {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    if ($NoModifyPath) { return }

    $userPath = Get-SpgUpdatedPath -CurrentPath (Get-SpgUserPath) -Directory $InstallDir
    if ($userPath) {
        Set-SpgUserPath -Value $userPath
        Write-Host "Added $InstallDir to your user PATH. Open a new terminal if 'spg' isn't found."
    }

    # Make spg usable straight away in the session that ran `irm | iex`.
    $sessionPath = Get-SpgUpdatedPath -CurrentPath $env:Path -Directory $InstallDir
    if ($sessionPath) { $env:Path = $sessionPath }
    Write-Host "Run 'spg --help' to get started."
}

# Dot-sourcing (as the tests do) only defines the functions; running the script, or `irm | iex`, installs.
if ($MyInvocation.InvocationName -ne '.') { Install-Spg }
