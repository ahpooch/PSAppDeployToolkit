---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Get-ADTStringTable

## SYNOPSIS
Retrieves the string database from the ADT module.

## SYNTAX

```
Get-ADTStringTable [<CommonParameters>]
```

## DESCRIPTION
The Get-ADTStringTable function returns the string database if it has been initialized.
If the string database is not initialized, it throws an error indicating that Initialize-ADTModule should be called before using this function.

## EXAMPLES

### EXAMPLE 1
```
Get-ADTStringTable
```

This example retrieves the string database from the ADT module.

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### This function does not take any pipeline input.
## OUTPUTS

### System.Collections.Hashtable
### Returns a hashtable containing the string database.
## NOTES
An active ADT session is NOT required to use this function.

Requires: The module should be initialized using Initialize-ADTModule

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
