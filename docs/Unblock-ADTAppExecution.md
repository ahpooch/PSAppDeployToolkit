---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Unblock-ADTAppExecution

## SYNOPSIS
Unblocks the execution of applications performed by the Block-ADTAppExecution function.

## SYNTAX

```
Unblock-ADTAppExecution [[-Tasks] <CimInstance[]>] [<CommonParameters>]
```

## DESCRIPTION
This function is called by the Close-ADTSession function or when the script itself is called with the parameters -CleanupBlockedApps.
It undoes the actions performed by Block-ADTAppExecution, allowing previously blocked applications to execute.

## EXAMPLES

### EXAMPLE 1
```
Unblock-ADTAppExecution
```

Unblocks the execution of applications that were previously blocked by Block-ADTAppExecution.

## PARAMETERS

### -Tasks
Specify the scheduled tasks to unblock.

```yaml
Type: CimInstance[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: (& $Script:CommandTable.'Get-ScheduledTask' -TaskName "$($MyInvocation.MyCommand.Module.Name)_*_BlockedApps" -ErrorAction Ignore)
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

### None
### This function does not generate any output.
## NOTES
An active ADT session is NOT required to use this function.

It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to undo the actions performed by Block-ADTAppExecution.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
