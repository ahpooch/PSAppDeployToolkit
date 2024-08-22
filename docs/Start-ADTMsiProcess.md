---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Start-ADTMsiProcess

## SYNOPSIS
Executes msiexec.exe to perform actions such as install, uninstall, patch, repair, or active setup for MSI and MSP files or MSI product codes.

## SYNTAX

```
Start-ADTMsiProcess [[-Action] <String>] [-Path] <String> [[-Transforms] <String[]>] [[-Parameters] <String>]
 [[-AddParameters] <String>] [-SecureParameters] [[-Patches] <String[]>] [[-LoggingOptions] <String>]
 [[-LogName] <String>] [[-WorkingDirectory] <String>] [-SkipMSIAlreadyInstalledCheck]
 [-IncludeUpdatesAndHotfixes] [-NoWait] [-PassThru] [[-IgnoreExitCodes] <String[]>]
 [[-PriorityClass] <ProcessPriorityClass>] [-NoExitOnProcessFailure] [-RepairFromSource] [<CommonParameters>]
```

## DESCRIPTION
This function utilizes msiexec.exe to handle various operations on MSI and MSP files, as well as MSI product codes.
The operations include installation, uninstallation, patching, repair, and setting up active configurations.

If the -Action parameter is set to "Install" and the MSI is already installed, the function will terminate without performing any actions.

The function automatically sets default switches for msiexec based on preferences defined in the XML configuration file.
Additionally, it generates a log file name and creates a verbose log for all msiexec operations, ensuring detailed tracking.

The MSI or MSP file is expected to reside in the "Files" subdirectory of the App Deploy Toolkit, with transform files expected to be in the same directory as the MSI file.

## EXAMPLES

### EXAMPLE 1
```
Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi'
```

Install an MSI.

### EXAMPLE 2
```
Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -Transform 'Adobe_FlashPlayer_11.2.202.233_x64_EN_01.mst' -Parameters '/QN'
```

Install an MSI, applying a transform and overriding the default MSI toolkit parameters.

### EXAMPLE 3
```
[PSObject]$ExecuteMSIResult = Start-ADTMsiProcess -Action 'Install' -Path 'Adobe_FlashPlayer_11.2.202.233_x64_EN.msi' -PassThru
```

Install an MSI and stores the result of the execution into a variable by using the -PassThru option.

### EXAMPLE 4
```
Start-ADTMsiProcess -Action 'Uninstall' -Path '{26923b43-4d38-484f-9b9e-de460746276c}'
```

Uninstall an MSI using a product code.

### EXAMPLE 5
```
Start-ADTMsiProcess -Action 'Patch' -Path 'Adobe_Reader_11.0.3_EN.msp'
```

Install an MSP.

## PARAMETERS

### -Action
Specifies the action to be performed.
Available options: Install, Uninstall, Patch, Repair, ActiveSetup.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: Install
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The file path to the MSI/MSP or the product code of the installed MSI.

```yaml
Type: String
Parameter Sets: (All)
Aliases: FilePath

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Transforms
The name(s) of the transform file(s) to be applied to the MSI.
The transform files should be in the same directory as the MSI file.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Parameters
Overrides the default parameters specified in the XML configuration file.
The install default is: "REBOOT=ReallySuppress /QB!".
The uninstall default is: "REBOOT=ReallySuppress /QN".

```yaml
Type: String
Parameter Sets: (All)
Aliases: Arguments

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AddParameters
Adds additional parameters to the default set specified in the XML configuration file.
The install default is: "REBOOT=ReallySuppress /QB!".
The uninstall default is: "REBOOT=ReallySuppress /QN".

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SecureParameters
Hides all parameters passed to the MSI or MSP file from the toolkit log file.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Patches
The name(s) of the patch (MSP) file(s) to be applied to the MSI for the "Install" action.
The patch files should be in the same directory as the MSI file.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LoggingOptions
Overrides the default logging options specified in the XML configuration file.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Overrides the default log file name.
The default log file name is generated from the MSI file name.
If LogName does not end in .log, it will be automatically appended.

For uninstallations, by default the product code is resolved to the DisplayName and version of the application.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 8
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WorkingDirectory
Overrides the working directory.
The working directory is set to the location of the MSI file.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 9
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SkipMSIAlreadyInstalledCheck
Skips the check to determine if the MSI is already installed on the system.
Default is: $false.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeUpdatesAndHotfixes
Include matches against updates and hotfixes in results.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoWait
Immediately continue after executing the process.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns ExitCode, STDOut, and STDErr output from the process.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -IgnoreExitCodes
List the exit codes to ignore or * to ignore all exit codes.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 10
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PriorityClass
Specifies priority class for the process.
Options: Idle, Normal, High, AboveNormal, BelowNormal, RealTime.
Default: Normal

```yaml
Type: ProcessPriorityClass
Parameter Sets: (All)
Aliases:
Accepted values: Normal, Idle, High, RealTime, BelowNormal, AboveNormal

Required: False
Position: 11
Default value: Normal
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoExitOnProcessFailure
Specifies whether the function shouldn't call Close-ADTSession when the process returns an exit code that is considered an error/failure.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -RepairFromSource
Specifies whether we should repair from source.
Also rewrites local cache.
Default: $false

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### PSADT.Types.ProcessResult
### Returns a PSObject with the results of the installation if -PassThru is specified.
### - ExitCode
### - StdOut
### - StdErr
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
