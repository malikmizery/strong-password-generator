# Pester 3.4 (ships with Windows PowerShell 5.1, which `irm | iex` often runs under):
#   powershell -NoProfile -Command "Invoke-Pester -Path tests -EnableExit"
. (Join-Path $PSScriptRoot '..\install.ps1')

# SHA256 of the ASCII bytes "hello".
$HelloSha256 = '2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824'

Describe 'Get-SpgUpdatedPath' {
    It 'appends the directory when PATH lacks it' {
        Get-SpgUpdatedPath -CurrentPath 'C:\Windows;C:\Tools' -Directory 'C:\Users\me\.local\bin' |
            Should Be 'C:\Windows;C:\Tools;C:\Users\me\.local\bin'
    }

    It 'returns nothing when the directory is present, ignoring case and a trailing backslash' {
        Get-SpgUpdatedPath -CurrentPath 'C:\Windows;c:\users\me\.local\bin\' -Directory 'C:\Users\me\.local\bin' |
            Should BeNullOrEmpty
    }

    It 'recognises an entry written with an environment variable' {
        $env:SPG_TEST_HOME = 'C:\Users\me'
        Get-SpgUpdatedPath -CurrentPath 'C:\Windows;%SPG_TEST_HOME%\.local\bin' -Directory 'C:\Users\me\.local\bin' |
            Should BeNullOrEmpty
        Remove-Item Env:\SPG_TEST_HOME
    }

    It 'adds no leading separator to an empty PATH' {
        Get-SpgUpdatedPath -CurrentPath '' -Directory 'C:\bin' | Should Be 'C:\bin'
    }

    It 'adds no double separator when PATH ends with one' {
        Get-SpgUpdatedPath -CurrentPath 'C:\Windows;' -Directory 'C:\bin' | Should Be 'C:\Windows;C:\bin'
    }
}

Describe 'Assert-SpgChecksum' {
    $file = Join-Path $TestDrive 'spg.exe'
    Set-Content -Path $file -Value 'hello' -NoNewline -Encoding Ascii

    It 'accepts a file matching its entry in the sums file' {
        { Assert-SpgChecksum -File $file -Sums "$HelloSha256  spg.exe`n$('a' * 64)  install.ps1" } | Should Not Throw
    }

    It 'accepts an uppercase hash' {
        { Assert-SpgChecksum -File $file -Sums "$($HelloSha256.ToUpperInvariant())  spg.exe" } | Should Not Throw
    }

    It 'rejects a file whose hash does not match' {
        { Assert-SpgChecksum -File $file -Sums "$('0' * 64)  spg.exe" } | Should Throw 'checksum'
    }

    It 'rejects a sums file with no entry for the file' {
        { Assert-SpgChecksum -File $file -Sums "$HelloSha256  other.exe" } | Should Throw 'spg.exe'
    }
}

Describe 'Install-Spg' {
    # Stand-ins for the release assets; the download mock serves them from TestDrive.
    New-Item -ItemType Directory -Path (Join-Path $TestDrive 'assets') | Out-Null
    Set-Content -Path (Join-Path $TestDrive 'assets\spg.exe') -Value 'hello' -NoNewline -Encoding Ascii

    Mock Invoke-RestMethod { [pscustomobject]@{ tag_name = 'v9.9.9' } }
    Mock Invoke-WebRequest { Copy-Item -Path "TestDrive:\assets\$(Split-Path $Uri -Leaf)" -Destination $OutFile }
    Mock Set-SpgUserPath { }

    Context 'with a valid download' {
        Set-Content -Path (Join-Path $TestDrive 'assets\SHA256SUMS.txt') -Value "$HelloSha256  spg.exe" -Encoding Ascii
        Mock Get-SpgUserPath { 'C:\Windows' }
        $dir = Join-Path $TestDrive 'bin'

        It 'downloads the latest release tag and installs spg.exe' {
            Install-Spg -InstallDir $dir

            Get-Content (Join-Path $dir 'spg.exe') -Raw | Should Be 'hello'
            Assert-MockCalled Invoke-WebRequest -Scope It -Times 1 -ParameterFilter { $Uri -like '*/releases/download/v9.9.9/spg.exe' }
            Assert-MockCalled Invoke-WebRequest -Scope It -Times 1 -ParameterFilter { $Uri -like '*/releases/download/v9.9.9/SHA256SUMS.txt' }
        }

        It 'adds the install directory to the user PATH' {
            Install-Spg -InstallDir $dir

            Assert-MockCalled Set-SpgUserPath -Scope It -Times 1 -Exactly -ParameterFilter { $Value -eq ('C:\Windows;' + (Join-Path $TestDrive 'bin')) }
        }

        It 'replaces an existing spg.exe' {
            Set-Content -Path (Join-Path $dir 'spg.exe') -Value 'old' -NoNewline

            Install-Spg -InstallDir $dir

            Get-Content (Join-Path $dir 'spg.exe') -Raw | Should Be 'hello'
        }

        It 'leaves PATH alone with -NoModifyPath' {
            Install-Spg -InstallDir $dir -NoModifyPath

            Assert-MockCalled Set-SpgUserPath -Scope It -Times 0 -Exactly
        }
    }

    Context 'when PATH already contains the install directory' {
        Set-Content -Path (Join-Path $TestDrive 'assets\SHA256SUMS.txt') -Value "$HelloSha256  spg.exe" -Encoding Ascii
        Mock Get-SpgUserPath { 'C:\Windows;' + (Join-Path $TestDrive 'bin2') }

        It 'does not write PATH' {
            Install-Spg -InstallDir (Join-Path $TestDrive 'bin2')

            Assert-MockCalled Set-SpgUserPath -Scope It -Times 0 -Exactly
        }
    }

    Context 'with a corrupted download' {
        Set-Content -Path (Join-Path $TestDrive 'assets\SHA256SUMS.txt') -Value "$('0' * 64)  spg.exe" -Encoding Ascii
        Mock Get-SpgUserPath { 'C:\Windows' }
        $dir = Join-Path $TestDrive 'bin3'

        It 'fails and installs nothing' {
            { Install-Spg -InstallDir $dir } | Should Throw 'checksum'

            Join-Path $dir 'spg.exe' | Should Not Exist
            Assert-MockCalled Set-SpgUserPath -Scope It -Times 0 -Exactly
        }
    }
}
