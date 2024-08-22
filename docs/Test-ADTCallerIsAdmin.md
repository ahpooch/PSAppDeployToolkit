---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTCallerIsAdmin

## SYNOPSIS
Checks if the current user has administrative privileges.

## SYNTAX

```
Test-ADTCallerIsAdmin
```

## DESCRIPTION
This function checks if the current user is a member of the Administrators group.
It returns a boolean value indicating whether the user has administrative privileges.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTCallerIsAdmin
```

Checks if the current user has administrative privileges and returns true or false.

## PARAMETERS

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Boolean
### Returns $true if the current user is an administrator, otherwise $false.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
