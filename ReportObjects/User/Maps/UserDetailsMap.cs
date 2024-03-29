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

using CsvHelper.Configuration;

namespace Snowflake.GrantReport.ReportObjects
{
    public class UserDetailsMap : ClassMap<User>
    {
        public UserDetailsMap()
        {
            int i = 0;
            
            Map(m => m.NAME).Index(i); i++;
            Map(m => m.LOGIN_NAME).Index(i); i++;
            
            Map(m => m.Owner).Index(i); i++;
            Map(m => m.IsSSOEnabled).Index(i); i++;
            Map(m => m.IsObjectIdentifierSpecialCharacters).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOn), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.CreatedOnUTC), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.LastLogon), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.LastLogonUTC), i); i++;

            Map(m => m.FIRST_NAME).Index(i); i++;
            Map(m => m.LAST_NAME).Index(i); i++;
            Map(m => m.MIDDLE_NAME).Index(i); i++;
            Map(m => m.DISPLAY_NAME).Index(i); i++;
            Map(m => m.EMAIL).Index(i); i++;

            Map(m => m.COMMENT).Index(i); i++;

            Map(m => m.DEFAULT_ROLE).Index(i); i++;
            Map(m => m.DEFAULT_NAMESPACE).Index(i); i++;
            Map(m => m.DEFAULT_WAREHOUSE).Index(i); i++;

            Map(m => m.DISABLED).Index(i); i++;
            Map(m => m.MUST_CHANGE_PASSWORD).Index(i); i++;
            
            Map(m => m.DAYS_TO_EXPIRY).Index(i); i++;
            Map(m => m.MINS_TO_BYPASS_MFA).Index(i); i++;
            Map(m => m.MINS_TO_BYPASS_NETWORK_POLICY).Index(i); i++;
            Map(m => m.MINS_TO_UNLOCK).Index(i); i++;
            
            Map(m => m.SNOWFLAKE_LOCK).Index(i); i++;
            Map(m => m.SNOWFLAKE_SUPPORT).Index(i); i++;

            Map(m => m.EXT_AUTHN_DUO).Index(i); i++;
            Map(m => m.EXT_AUTHN_UID).Index(i); i++;

            Map(m => m.RSA_PUBLIC_KEY_FP).Index(i); i++;
            Map(m => m.RSA_PUBLIC_KEY_2_FP).Index(i); i++;

            CSVMapHelper.SetISO8601DateFormat(Map(m => m.PASSWORD_LAST_SET_TIME), i); i++;
            CSVMapHelper.SetISO8601DateFormat(Map(m => m.PASSWORD_LAST_SET_TIMEUTC), i); i++;
        }
    }
}