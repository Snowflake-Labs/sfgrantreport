// Copyright (c) 2021 Snowflake Inc. All rights reserved.

// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at

//   http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

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