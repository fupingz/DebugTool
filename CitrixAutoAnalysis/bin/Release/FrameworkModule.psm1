#
# Copyright Citrix Systems, Inc. All rights reserved.
#

Set-StrictMode -Version 2.0

# Any failure is a terminating failure.
$ErrorActionPreference = 'Stop'
$ReportErrorShowStackTrace = $true
$ReportErrorShowInnerException = $true

$AdminConsoleOperationModeKey = 'AdminConsoleOperationMode'
$AdminConsoleIsEnabledKey = 'AdminConsoleIsEnabled'
$IsXenDesktopSideBySideDeploymentKey = 'IsXenDesktopSideBySideDeployment'

function CustomModuleInitialization
{
}

<#
 .SYNOPSIS
   Gets the Admin Console operating mode, e.g. "FirstUse" or "Full".

 .DESCRIPTION
   Cmdlet used to get the Admin Console operating mode, which is aggregated from the modes of the various plugins.

 .LINK
   Set-DSAdminConsoleOperationMode
#>
function Get-DSAdminConsoleOperationMode()
{
    ReloadFrameworkController

    $modeValue = Get-DSFrameworkProperty -Key $AdminConsoleOperationModeKey

    $returnObject = New-Object PSObject
    $returnObject | Add-Member -MemberType NoteProperty -Name "AdminConsoleOperationMode" -Value $modeValue

    Write-Output $returnObject
}

<#
 .SYNOPSIS
   Sets the Admin Console operating mode, e.g. "FirstUse" or "Full".

 .DESCRIPTION
   Cmdlet used to set the Admin Console operating mode, which is aggregated from the modes of the various plugins.

 .LINK
   Get-DSAdminConsoleOperationMode
#>
function Set-DSAdminConsoleOperationMode([Parameter(Mandatory=$true)] [string] $mode)
{
    ReloadFrameworkController

    Set-DSFrameworkProperty -Key $AdminConsoleOperationModeKey -Value $mode

    # Also set the registry key associated with this value such that
    # installer based solutions such as the XenApp role installer can
    # determine whether the mode has changed to a value it recognizes.
    # It is assumed that this cmdlet will be used to set the 2 values

    Set-ItemProperty -Path HKLM:\SOFTWARE\Citrix\DeliveryServices `
                     -Name $AdminConsoleOperationModeKey `
                     -Value $mode
}

<#
 .SYNOPSIS
   Gets a value indicating whether or not the Admin Console is enabled.

 .DESCRIPTION
   Cmdlet used to get the Admin Console enabled state, which determines which Admin Console features are available to the user.
#>
function Get-DSAdminConsoleIsEnabled()
{
    $isEnabled = Get-DSFrameworkProperty -Key $AdminConsoleIsEnabledKey

    $returnObject = New-Object PSObject
    $returnObject | Add-Member -MemberType NoteProperty -Name "AdminConsoleIsEnabled" -Value $isEnabled

    Write-Output $returnObject
}

<#
 .SYNOPSIS
   Sets a value indicating whether or not the Admin Console is enabled.

 .Description
   Cmdlet used to set the Admin Console enabled state, which determines which Admin Console features are available to the user.
#>
function Set-DSAdminConsoleIsEnabled([Parameter(Mandatory=$true)] [bool] $IsEnabled)
{
    ReloadFrameworkController

    Set-DSFrameworkProperty -Key $AdminConsoleIsEnabledKey -Value $IsEnabled
}

<#
 .SYNOPSIS
   Gets a value indicating whether this is a XenDesktop side-by-side deployment.
#>
function Get-DSIsXenDesktopSideBySideDeployment()
{
    $isXenDesktopSideBySideDeployment = Get-DSFrameworkProperty -Key $IsXenDesktopSideBySideDeploymentKey

    $returnObject = New-Object PSObject
    $returnObject | Add-Member -MemberType NoteProperty -Name "IsXenDesktopSideBySideDeployment" -Value $isXenDesktopSideBySideDeployment

    Write-Output $returnObject
}

<#
 .SYNOPSIS
   Sets a value indicating whether this is a XenDesktop side-by-side deployment.
#>
function Set-DSIsXenDesktopSideBySideDeployment([bool]$isXenDesktopSideBySideDeployment)
{
    ReloadFrameworkController

    Set-DSFrameworkProperty -Key $IsXenDesktopSideBySideDeploymentKey -Value $isXenDesktopSideBySideDeployment
}

<#
 .SYNOPSIS
   Gets a value indicating whether this is a Multi Tenant deployment.
#>
function Get-DSIsMultiTenantDeployment()
{
    ReloadFrameworkController

    $controller = Get-DSFrameworkController

    $returnObject = New-Object PSObject
    $returnObject | Add-Member -MemberType NoteProperty -Name "IsMultiTenantDeployment" -Value $controller.IsMultipleTenancyMode

    Write-Output $returnObject
}

#---------------------------------------------------------------------
# Common Module Code Start
#---------------------------------------------------------------------

Set-Variable -Name 'LoadedSnapinModules' -Value @() -Scope Script

#---------------------------------------------------------------------
# Snapin management
#---------------------------------------------------------------------
Function LoadSnapins([string[]]$snapins)
{
    # Find out if there are any snapins that need loading
    $snapinsToLoad = @()
    foreach ($snapin in @($snapins))
    {
        $module = $LoadedSnapinModules | Where {$_ -eq $snapin}
        if (!$module)
        {
            $module = (Get-PSSnapin -Name $snapin -ErrorAction SilentlyContinue)
            if ($module -eq $null)
            {
                $snapinsToLoad = $snapinsToLoad + $snapin
            }
        }
    }

    # If there are any that need loading, attempt to do so now
    if ($snapinsToLoad.Count -gt 0)
    {
        # Cannot cache list of loaded snapins as they are registered
        # dynamically when installing new feature classes.
        # It turns out that repeatedly getting snapins one by one (using -Name parameter)
        # is much quicker than retrieving full list and then filtering it out.

        foreach ($snapin in @($snapinsToLoad))
        {
            # Get the snapin dll file

            $snapinModule = Get-PSSnapin -Registered -Name $snapin -ErrorAction SilentlyContinue
            $snapinFile = $snapinModule.ModuleName

            if (!$snapinFile)
            {
                Write-Error "No such snapin '$snapin'"
            }

            Write-Host "Loading '$snapinFile'"

            # Adding the snapin via the module mechanism isolates the add
            # to the module scope, allowing multiple load requests to
            # take place in different modules without causing an error

            Import-Module -name $snapinFile

            # Update the list of loaded snapin module names
            Set-Variable -Name 'LoadedSnapinModules' -Value ($LoadedSnapinModules + $snapin) -Scope Script
        }
    }
}

#---------------------------------------------------------------------
# Perform common initialization.
#---------------------------------------------------------------------
Function InitializeModule()
{
    # Load very commonly and always-installed snapins.
    LoadSnapins @("Citrix.DeliveryServices.Framework.Commands", `
                  "Citrix.DeliveryServices.ConfigurationProvider", `
                  "Citrix.DeliveryServices.Web.Commands")

    CustomModuleInitialization
}

#---------------------------------------------------------------------
# Initialize the module.
#---------------------------------------------------------------------
InitializeModule

#---------------------------------------------------------------------
# Common Module Code End
#---------------------------------------------------------------------

# -----------------------------------------------------------------------
# Export the functions
# -----------------------------------------------------------------------
Export-ModuleMember -Function "Get-DSAdminConsoleOperationMode"
Export-ModuleMember -Function "Set-DSAdminConsoleOperationMode"
Export-ModuleMember -Function "Get-DSAdminConsoleIsEnabled"
Export-ModuleMember -Function "Set-DSAdminConsoleIsEnabled"
Export-ModuleMember -Function "Get-DSIsXenDesktopSideBySideDeployment"
Export-ModuleMember -Function "Set-DSIsXenDesktopSideBySideDeployment"
Export-ModuleMember -Function "Get-DSIsMultiTenantDeployment"
Export-ModuleMember -Function "Get-DSHostDomainJoined"

# SIG # Begin signature block
# MIIdWAYJKoZIhvcNAQcCoIIdSTCCHUUCAQExCzAJBgUrDgMCGgUAMGkGCisGAQQB
# gjcCAQSgWzBZMDQGCisGAQQBgjcCAR4wJgIDAQAABBAfzDtgWUsITrck0sYpfvNR
# AgEAAgEAAgEAAgEAAgEAMCEwCQYFKw4DAhoFAAQUWcRnG/Lf4YrX2TQMPEtilCUd
# ToWgggqHMIIFMDCCBBigAwIBAgIQBAkYG1/Vu2Z1U0O1b5VQCDANBgkqhkiG9w0B
# AQsFADBlMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYD
# VQQLExB3d3cuZGlnaWNlcnQuY29tMSQwIgYDVQQDExtEaWdpQ2VydCBBc3N1cmVk
# IElEIFJvb3QgQ0EwHhcNMTMxMDIyMTIwMDAwWhcNMjgxMDIyMTIwMDAwWjByMQsw
# CQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cu
# ZGlnaWNlcnQuY29tMTEwLwYDVQQDEyhEaWdpQ2VydCBTSEEyIEFzc3VyZWQgSUQg
# Q29kZSBTaWduaW5nIENBMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA
# +NOzHH8OEa9ndwfTCzFJGc/Q+0WZsTrbRPV/5aid2zLXcep2nQUut4/6kkPApfmJ
# 1DcZ17aq8JyGpdglrA55KDp+6dFn08b7KSfH03sjlOSRI5aQd4L5oYQjZhJUM1B0
# sSgmuyRpwsJS8hRniolF1C2ho+mILCCVrhxKhwjfDPXiTWAYvqrEsq5wMWYzcT6s
# cKKrzn/pfMuSoeU7MRzP6vIK5Fe7SrXpdOYr/mzLfnQ5Ng2Q7+S1TqSp6moKq4Tz
# rGdOtcT3jNEgJSPrCGQ+UpbB8g8S9MWOD8Gi6CxR93O8vYWxYoNzQYIH5DiLanMg
# 0A9kczyen6Yzqf0Z3yWT0QIDAQABo4IBzTCCAckwEgYDVR0TAQH/BAgwBgEB/wIB
# ADAOBgNVHQ8BAf8EBAMCAYYwEwYDVR0lBAwwCgYIKwYBBQUHAwMweQYIKwYBBQUH
# AQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdpY2VydC5jb20wQwYI
# KwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFz
# c3VyZWRJRFJvb3RDQS5jcnQwgYEGA1UdHwR6MHgwOqA4oDaGNGh0dHA6Ly9jcmw0
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcmwwOqA4oDaG
# NGh0dHA6Ly9jcmwzLmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RD
# QS5jcmwwTwYDVR0gBEgwRjA4BgpghkgBhv1sAAIEMCowKAYIKwYBBQUHAgEWHGh0
# dHBzOi8vd3d3LmRpZ2ljZXJ0LmNvbS9DUFMwCgYIYIZIAYb9bAMwHQYDVR0OBBYE
# FFrEuXsqCqOl6nEDwGD5LfZldQ5YMB8GA1UdIwQYMBaAFEXroq/0ksuCMS1Ri6en
# IZ3zbcgPMA0GCSqGSIb3DQEBCwUAA4IBAQA+7A1aJLPzItEVyCx8JSl2qB1dHC06
# GsTvMGHXfgtg/cM9D8Svi/3vKt8gVTew4fbRknUPUbRupY5a4l4kgU4QpO4/cY5j
# DhNLrddfRHnzNhQGivecRk5c/5CxGwcOkRX7uq+1UcKNJK4kxscnKqEpKBo6cSgC
# PC6Ro8AlEeKcFEehemhor5unXCBc2XGxDI+7qPjFEmifz0DLQESlE/DmZAwlCEIy
# sjaKJAL+L3J+HNdJRZboWR3p+nRka7LrZkPas7CM1ekN3fYBIM6ZMWM9CBoYs4Gb
# T8aTEAb8B4H6i9r5gkn3Ym6hU/oSlBiFLpKR6mhsRDKyZqHnGKSaZFHvMIIFTzCC
# BDegAwIBAgIQA4DLJ+ImWPOIPLBisV8TETANBgkqhkiG9w0BAQsFADByMQswCQYD
# VQQGEwJVUzEVMBMGA1UEChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cuZGln
# aWNlcnQuY29tMTEwLwYDVQQDEyhEaWdpQ2VydCBTSEEyIEFzc3VyZWQgSUQgQ29k
# ZSBTaWduaW5nIENBMB4XDTE3MDgyNTAwMDAwMFoXDTE4MDgyODEyMDAwMFowgYsx
# CzAJBgNVBAYTAlVTMQswCQYDVQQIEwJGTDEXMBUGA1UEBxMORnQuIExhdWRlcmRh
# bGUxHTAbBgNVBAoTFENpdHJpeCBTeXN0ZW1zLCBJbmMuMRgwFgYDVQQLEw9MQ00o
# cG93ZXJzaGVsbCkxHTAbBgNVBAMTFENpdHJpeCBTeXN0ZW1zLCBJbmMuMIIBIjAN
# BgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvxP6yVDQbQtrqiJo9W2suwQgEc1t
# l4f3rI3RGJyETQd5V89ybgyBC4jxUOTe+GW17ZjS+TEoxHi7+BsRWd7STiX/GGPS
# M2WRLez3JEsppzpmM7uodWOmWSGC0FqnAJ6wmc5EV+lbMZZi1U5pu0Y4OPJ5YlBO
# u3/dk32aGoORpsUGtuhhMUwjuMBFZ6QFlkExlarYsgbk4TQ7Ieg8U7WiPWN4s8Hk
# K1SKxDYyvcJ6KssPwlWFD+GQukQvAbZY90Q0KjR0uHKJYkvtRTQYcMU3C6QfvIx5
# Td8lfiRIes6A4ppF05WxpP92r4wzUE6UGNXwzIHwbUMU0yrAK5femRCorQIDAQAB
# o4IBxTCCAcEwHwYDVR0jBBgwFoAUWsS5eyoKo6XqcQPAYPkt9mV1DlgwHQYDVR0O
# BBYEFOTohEywkQuuOa4vweBtuJ135KAzMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUE
# DDAKBggrBgEFBQcDAzB3BgNVHR8EcDBuMDWgM6Axhi9odHRwOi8vY3JsMy5kaWdp
# Y2VydC5jb20vc2hhMi1hc3N1cmVkLWNzLWcxLmNybDA1oDOgMYYvaHR0cDovL2Ny
# bDQuZGlnaWNlcnQuY29tL3NoYTItYXNzdXJlZC1jcy1nMS5jcmwwTAYDVR0gBEUw
# QzA3BglghkgBhv1sAwEwKjAoBggrBgEFBQcCARYcaHR0cHM6Ly93d3cuZGlnaWNl
# cnQuY29tL0NQUzAIBgZngQwBBAEwgYQGCCsGAQUFBwEBBHgwdjAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tME4GCCsGAQUFBzAChkJodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRTSEEyQXNzdXJlZElEQ29kZVNp
# Z25pbmdDQS5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAQEADd/R
# 5pBjYfXp8on3TaDyFfcCeH9lDDLrTWCQsi1Zcl2qqWBIzqM7NnBWdIW8ZEdUmY6m
# mfya0QS41p563bxvuLdxiXmtfp4CX7Z25XAKmuD8z+RB4WVrIfoXvLOwQw9/58Z9
# snMt00+TQL9sAfUZBuWh2e128jFdQEf9ODTZr6L6SiUpZyaIWMrLGDb9e48qbm8q
# w1kg+3HgisqdpIMiCVugBvIVw3FRmXtTXnotvjEpe9i8ePek/wVeq51rPKaHD7bR
# 9sYV2R/87AWt7IOog1p2g24PpbRK7YmPbf8d8dG/aAnH0EbsIp3lGj0GtqxkECvI
# jd3JW3jtncDN/BPTSDGCEjswghI3AgEBMIGGMHIxCzAJBgNVBAYTAlVTMRUwEwYD
# VQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xMTAv
# BgNVBAMTKERpZ2lDZXJ0IFNIQTIgQXNzdXJlZCBJRCBDb2RlIFNpZ25pbmcgQ0EC
# EAOAyyfiJljziDywYrFfExEwCQYFKw4DAhoFAKBwMBAGCisGAQQBgjcCAQwxAjAA
# MBkGCSqGSIb3DQEJAzEMBgorBgEEAYI3AgEEMBwGCisGAQQBgjcCAQsxDjAMBgor
# BgEEAYI3AgEVMCMGCSqGSIb3DQEJBDEWBBQHZ0qGxzGqFj8zQShcH8ncDaWx4DAN
# BgkqhkiG9w0BAQEFAASCAQBZkXvELz6ypvMDJa6AIl2SBM/kuTe4djBzGEMz7VBS
# sWTv/fmNCI3R4xoVk5AmAGPqSTSesZl94d9JlywOADUGIAVkCJZ9Ibt7bDzh+X/7
# M7r8mp+rY4bKo61mfb6E9F75jzufxa0akmlvk6GxccoO3/NVpz2no2sHp7WIRRWT
# lXRxC1Z93jR0TGaxnPSzd8hsckWzq3/3K3Gd8j1doCInBAFXxVlMnpgIF0TZUP2V
# hdhxgjS7zIcbkej1V2wwfeaUyu0I5GniR37Sl1v2MEO8svv6hVa38PSM2RIHEYN9
# T9u92uCpyMHBBmClNsk15NMwn0pBjRrShVVNYvSjPF2VoYIQFzCCEBMGCisGAQQB
# gjcDAwExghADMIIP/wYJKoZIhvcNAQcCoIIP8DCCD+wCAQMxCzAJBgUrDgMCGgUA
# MGcGCyqGSIb3DQEJEAEEoFgEVjBUAgEBBglghkgBhv1sBwEwITAJBgUrDgMCGgUA
# BBSABDJODFsZ18H0q8zvUiaSYjc/kAIQSUa2psSli7sl9bH79twKUBgPMjAxNzEx
# MTcxMzQ0NTVaoIINPzCCBmowggVSoAMCAQICEAMBmgI6/1ixa9bV6uYX8GYwDQYJ
# KoZIhvcNAQEFBQAwYjELMAkGA1UEBhMCVVMxFTATBgNVBAoTDERpZ2lDZXJ0IElu
# YzEZMBcGA1UECxMQd3d3LmRpZ2ljZXJ0LmNvbTEhMB8GA1UEAxMYRGlnaUNlcnQg
# QXNzdXJlZCBJRCBDQS0xMB4XDTE0MTAyMjAwMDAwMFoXDTI0MTAyMjAwMDAwMFow
# RzELMAkGA1UEBhMCVVMxETAPBgNVBAoTCERpZ2lDZXJ0MSUwIwYDVQQDExxEaWdp
# Q2VydCBUaW1lc3RhbXAgUmVzcG9uZGVyMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A
# MIIBCgKCAQEAo2Rd/Hyz4II14OD2xirmSXU7zG7gU6mfH2RZ5nxrf2uMnVX4kuOe
# 1VpjWwJJUNmDzm9m7t3LhelfpfnUh3SIRDsZyeX1kZ/GFDmsJOqoSyyRicxeKPRk
# tlC39RKzc5YKZ6O+YZ+u8/0SeHUOplsU/UUjjoZEVX0YhgWMVYd5SEb3yg6Np95O
# X+Koti1ZAmGIYXIYaLm4fO7m5zQvMXeBMB+7NgGN7yfj95rwTDFkjePr+hmHqH7P
# 7IwMNlt6wXq4eMfJBi5GEMiN6ARg27xzdPpO2P6qQPGyznBGg+naQKFZOtkVCVeZ
# VjCT88lhzNAIzGvsYkKRrALA76TwiRGPdwIDAQABo4IDNTCCAzEwDgYDVR0PAQH/
# BAQDAgeAMAwGA1UdEwEB/wQCMAAwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwgwggG/
# BgNVHSAEggG2MIIBsjCCAaEGCWCGSAGG/WwHATCCAZIwKAYIKwYBBQUHAgEWHGh0
# dHBzOi8vd3d3LmRpZ2ljZXJ0LmNvbS9DUFMwggFkBggrBgEFBQcCAjCCAVYeggFS
# AEEAbgB5ACAAdQBzAGUAIABvAGYAIAB0AGgAaQBzACAAQwBlAHIAdABpAGYAaQBj
# AGEAdABlACAAYwBvAG4AcwB0AGkAdAB1AHQAZQBzACAAYQBjAGMAZQBwAHQAYQBu
# AGMAZQAgAG8AZgAgAHQAaABlACAARABpAGcAaQBDAGUAcgB0ACAAQwBQAC8AQwBQ
# AFMAIABhAG4AZAAgAHQAaABlACAAUgBlAGwAeQBpAG4AZwAgAFAAYQByAHQAeQAg
# AEEAZwByAGUAZQBtAGUAbgB0ACAAdwBoAGkAYwBoACAAbABpAG0AaQB0ACAAbABp
# AGEAYgBpAGwAaQB0AHkAIABhAG4AZAAgAGEAcgBlACAAaQBuAGMAbwByAHAAbwBy
# AGEAdABlAGQAIABoAGUAcgBlAGkAbgAgAGIAeQAgAHIAZQBmAGUAcgBlAG4AYwBl
# AC4wCwYJYIZIAYb9bAMVMB8GA1UdIwQYMBaAFBUAEisTmLKZB+0e36K+Vw0rZwLN
# MB0GA1UdDgQWBBRhWk0ktkkynUoqeRqDS/QeicHKfTB9BgNVHR8EdjB0MDigNqA0
# hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRBc3N1cmVkSURDQS0x
# LmNybDA4oDagNIYyaHR0cDovL2NybDQuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNz
# dXJlZElEQ0EtMS5jcmwwdwYIKwYBBQUHAQEEazBpMCQGCCsGAQUFBzABhhhodHRw
# Oi8vb2NzcC5kaWdpY2VydC5jb20wQQYIKwYBBQUHMAKGNWh0dHA6Ly9jYWNlcnRz
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydEFzc3VyZWRJRENBLTEuY3J0MA0GCSqGSIb3
# DQEBBQUAA4IBAQCdJX4bM02yJoFcm4bOIyAPgIfliP//sdRqLDHtOhcZcRfNqRu8
# WhY5AJ3jbITkWkD73gYBjDf6m7GdJH7+IKRXrVu3mrBgJuppVyFdNC8fcbCDlBkF
# azWQEKB7l8f2P+fiEUGmvWLZ8Cc9OB0obzpSCfDscGLTYkuw4HOmksDTjjHYL+Nt
# FxMG7uQDthSr849Dp3GdId0UyhVdkkHa+Q+B0Zl0DSbEDn8btfWg8cZ3BigV6diT
# 5VUW8LsKqxzbXEgnZsijiwoc5ZXarsQuWaBh3drzbaJh6YoLbewSGL33VVRAA5Ir
# a8JRwgpIr7DUbuD0FAo6G+OPPcqvao173NhEMIIGzTCCBbWgAwIBAgIQBv35A5YD
# reoACus/J7u6GzANBgkqhkiG9w0BAQUFADBlMQswCQYDVQQGEwJVUzEVMBMGA1UE
# ChMMRGlnaUNlcnQgSW5jMRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSQwIgYD
# VQQDExtEaWdpQ2VydCBBc3N1cmVkIElEIFJvb3QgQ0EwHhcNMDYxMTEwMDAwMDAw
# WhcNMjExMTEwMDAwMDAwWjBiMQswCQYDVQQGEwJVUzEVMBMGA1UEChMMRGlnaUNl
# cnQgSW5jMRkwFwYDVQQLExB3d3cuZGlnaWNlcnQuY29tMSEwHwYDVQQDExhEaWdp
# Q2VydCBBc3N1cmVkIElEIENBLTEwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEK
# AoIBAQDogi2Z+crCQpWlgHNAcNKeVlRcqcTSQQaPyTP8TUWRXIGf7Syc+BZZ3561
# JBXCmLm0d0ncicQK2q/LXmvtrbBxMevPOkAMRk2T7It6NggDqww0/hhJgv7HxzFI
# gHweog+SDlDJxofrNj/YMMP/pvf7os1vcyP+rFYFkPAyIRaJxnCI+QWXfaPHQ90C
# 6Ds97bFBo+0/vtuVSMTuHrPyvAwrmdDGXRJCgeGDboJzPyZLFJCuWWYKxI2+0s4G
# rq2Eb0iEm09AufFM8q+Y+/bOQF1c9qjxL6/siSLyaxhlscFzrdfx2M8eCnRcQrho
# frfVdwonVnwPYqQ/MhRglf0HBKIJAgMBAAGjggN6MIIDdjAOBgNVHQ8BAf8EBAMC
# AYYwOwYDVR0lBDQwMgYIKwYBBQUHAwEGCCsGAQUFBwMCBggrBgEFBQcDAwYIKwYB
# BQUHAwQGCCsGAQUFBwMIMIIB0gYDVR0gBIIByTCCAcUwggG0BgpghkgBhv1sAAEE
# MIIBpDA6BggrBgEFBQcCARYuaHR0cDovL3d3dy5kaWdpY2VydC5jb20vc3NsLWNw
# cy1yZXBvc2l0b3J5Lmh0bTCCAWQGCCsGAQUFBwICMIIBVh6CAVIAQQBuAHkAIAB1
# AHMAZQAgAG8AZgAgAHQAaABpAHMAIABDAGUAcgB0AGkAZgBpAGMAYQB0AGUAIABj
# AG8AbgBzAHQAaQB0AHUAdABlAHMAIABhAGMAYwBlAHAAdABhAG4AYwBlACAAbwBm
# ACAAdABoAGUAIABEAGkAZwBpAEMAZQByAHQAIABDAFAALwBDAFAAUwAgAGEAbgBk
# ACAAdABoAGUAIABSAGUAbAB5AGkAbgBnACAAUABhAHIAdAB5ACAAQQBnAHIAZQBl
# AG0AZQBuAHQAIAB3AGgAaQBjAGgAIABsAGkAbQBpAHQAIABsAGkAYQBiAGkAbABp
# AHQAeQAgAGEAbgBkACAAYQByAGUAIABpAG4AYwBvAHIAcABvAHIAYQB0AGUAZAAg
# AGgAZQByAGUAaQBuACAAYgB5ACAAcgBlAGYAZQByAGUAbgBjAGUALjALBglghkgB
# hv1sAxUwEgYDVR0TAQH/BAgwBgEB/wIBADB5BggrBgEFBQcBAQRtMGswJAYIKwYB
# BQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEFBQcwAoY3aHR0
# cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENB
# LmNydDCBgQYDVR0fBHoweDA6oDigNoY0aHR0cDovL2NybDMuZGlnaWNlcnQuY29t
# L0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDA6oDigNoY0aHR0cDovL2NybDQu
# ZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENBLmNybDAdBgNVHQ4E
# FgQUFQASKxOYspkH7R7for5XDStnAs0wHwYDVR0jBBgwFoAUReuir/SSy4IxLVGL
# p6chnfNtyA8wDQYJKoZIhvcNAQEFBQADggEBAEZQPsm3KCSnOB22WymvUs9S6TFH
# q1Zce9UNC0Gz7+x1H3Q48rJcYaKclcNQ5IK5I9G6OoZyrTh4rHVdFxc0ckeFlFbR
# 67s2hHfMJKXzBBlVqefj56tizfuLLZDCwNK1lL1eT7EF0g49GqkUW6aGMWKoqDPk
# mzmnxPXOHXh2lCVz5Cqrz5x2S+1fwksW5EtwTACJHvzFebxMElf+X+EevAJdqP77
# BzhPDcZdkbkPZ0XN1oPt55INjbFpjE/7WeAjD9KqrgB87pxCDs+R1ye3Fu4Pw718
# CqDuLAhVhSK46xgaTfwqIa1JMYNHlXdx3LEbS0scEJx3FMGdTy9alQgpECYxggIs
# MIICKAIBATB2MGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IEFz
# c3VyZWQgSUQgQ0EtMQIQAwGaAjr/WLFr1tXq5hfwZjAJBgUrDgMCGgUAoIGMMBoG
# CSqGSIb3DQEJAzENBgsqhkiG9w0BCRABBDAcBgkqhkiG9w0BCQUxDxcNMTcxMTE3
# MTM0NDU1WjAjBgkqhkiG9w0BCQQxFgQUQOOlG1Qvou7BAEaEV63PHBNrJ3IwKwYL
# KoZIhvcNAQkQAgwxHDAaMBgwFgQUYU0nHZEC4wFpgiSH/eXeAKNSsB0wDQYJKoZI
# hvcNAQEBBQAEggEAf6rHnMwGwTq09l8w5CZKY8+QOfCj40kXdvEw7FHeQK1qbLfk
# mVfUkdVw8/MAU/ZZ7XQF0BwqcjMUPt/iKmPoghJZaS/9G/bgstNn6feII9DtSWsX
# ghGsgpF/bjZuSOinrIDsKJwQDqjgs00muC2lBrZ39mtHX+O766xZ/4nDtSZlgAlw
# r1Vgmqea8cxYLs5yausdJB4oOFRE/o1ud5HISPnmrVUqBlm6zvwH+VnXeCNRoyEe
# 2m/SbsJClhpAO1ZWADJIAePjl2S5AmdeVqz8egO94EWld6IEvKkxSk3I5qrHJGkD
# YMUyMq/ewMMXhiB6z5+rJekQ8OPzixT8FmbrxQ==
# SIG # End signature block
