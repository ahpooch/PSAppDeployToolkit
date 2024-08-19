﻿#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Mount-ADTWimFile
{
    <#
    .SYNOPSIS
        Mounts a WIM file to a specified directory.

    .DESCRIPTION
        Mounts a WIM file to a specified directory. The function supports mounting by image index or image name. It also provides options to forcefully remove existing directories and return the mounted image details.

    .PARAMETER ImagePath
        Path to the WIM file to be mounted.

        Mandatory: True

    .PARAMETER Path
        Directory where the WIM file will be mounted. The directory must be empty and not have a pre-existing WIM mounted.

        Mandatory: True

    .PARAMETER Index
        Index of the image within the WIM file to be mounted.

        Mandatory: True

    .PARAMETER Name
        Name of the image within the WIM file to be mounted.

        Mandatory: True

    .PARAMETER Force
        Forces the removal of the existing directory if it is not empty.

        Mandatory: False

    .PARAMETER PassThru
        Returns the mounted image details.

        Mandatory: False

    .INPUTS
        None

        This function does not take any piped input.

    .OUTPUTS
        Microsoft.Dism.ImageInfo

        Returns the mounted image details if the PassThru parameter is specified.

    .EXAMPLE
        # Example 1
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1

        Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory.

    .EXAMPLE
        # Example 2
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Name 'Windows 10 Pro'

        Mounts the image named 'Windows 10 Pro' in the 'install.wim' file to the 'C:\Mount' directory.

    .EXAMPLE
        # Example 3
        Mount-ADTWimFile -ImagePath 'C:\Images\install.wim' -Path 'C:\Mount' -Index 1 -Force

        Mounts the first image in the 'install.wim' file to the 'C:\Mount' directory, forcefully removing the existing directory if it is not empty.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [System.IO.FileInfo]$ImagePath,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateScript({
                if (Test-ADTMountedWimPath -Path $_)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path has a pre-existing WIM mounted.'))
                }
                if (& $Script:CommandTable.'Get-ChildItem' -LiteralPath $_ -ErrorAction Ignore)
                {
                    $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Path -ProvidedValue $_ -ExceptionMessage 'The specified path is not empty.'))
                }
                return !!$_
            })]
        [System.IO.DirectoryInfo]$Path,

        [Parameter(Mandatory = $true, ParameterSetName = 'Index')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Index,

        [Parameter(Mandatory = $true, ParameterSetName = 'Name')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Name')]
        [System.Management.Automation.SwitchParameter]$Force,

        [Parameter(Mandatory = $false, ParameterSetName = 'Index')]
        [Parameter(Mandatory = $false, ParameterSetName = 'Name')]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        # Attempt to get specified WIM image before initialising.
        $null = try
        {
            $PSBoundParameters.Remove('PassThru')
            $PSBoundParameters.Remove('Force')
            $PSBoundParameters.Remove('Path')
            & $Script:CommandTable.'Get-WindowsImage' @PSBoundParameters
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        # Announce commencement.
        Write-ADTLogEntry -Message "Mounting WIM file [$ImagePath] to [$Path]."
        try
        {
            try
            {
                # If we're using the force, forcibly remove the existing directory.
                if ([System.IO.Directory]::Exists($Path) -and $Force)
                {
                    Write-ADTLogEntry -Message "Removing pre-existing path [$Path] as [-Force] was provided."
                    Remove-Item -LiteralPath $Path -Force -Confirm:$false
                }

                # If the path doesn't exist, create it.
                if (![System.IO.Directory]::Exists($Path))
                {
                    Write-ADTLogEntry -Message "Creating path [$Path] as it does not exist."
                    $Path = [System.IO.Directory]::CreateDirectory($Path).FullName
                }

                # Mount the WIM file.
                $res = & $Script:CommandTable.'Mount-WindowsImage' @PSBoundParameters -Path $Path -ReadOnly -CheckIntegrity
                Write-ADTLogEntry -Message "Successfully mounted WIM file [$ImagePath]."

                # Store the result within the user's ADTSession if there's an active one.
                if (Test-ADTSessionActive)
                {
                    (Get-ADTSession).GetMountedWimFiles().Add($res)
                }

                # Return the result if we're passing through.
                if ($PassThru)
                {
                    return $res
                }
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage 'Error occurred while attemping to mount WIM file.'
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}