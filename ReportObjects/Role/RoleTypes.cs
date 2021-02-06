using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{

    public enum RoleType
    {
        Unknown,

        // ACCOUNTADMIN, SECURITYADMIN, USERADMIN, SYSADMIN
        BuiltIn,
        
        // Child of USERADMIN, OKTA_PROVISIONER, AAD_PROVISIONER and GENERIC_SCIM_PROVISIONER
        SCIM,

        // Child of SECURITYADMIN or USERADMIN
        RoleManagement,

        // Child of SYSADMIN, grants to other Roles only
        Functional,

        // Child of Functional role, grants to some objects
        Access,

        // Not Child of SYSADMIN, grants to other Roles only
        FunctionalNotUnderSysadmin,

        // Not Child of SYSADMIN, Functional role, grants to some objects
        AccessNotUnderSysadmin,

        // Not under ACCOUNTADMIN
        NotUnderAccountAdmin
    }
}