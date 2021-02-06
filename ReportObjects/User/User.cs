using System;
using System.Globalization;

namespace Snowflake.GrantReport.ReportObjects
{
    /// <summary>
    /// Encapsulates SHOW USERS output:
    /// name
    /// created_on
    /// login_name
    /// display_name
    /// first_name
    /// last_name
    /// email
    /// mins_to_unlock
    /// days_to_expiry
    /// comment
    /// disabled
    /// must_change_password
    /// snowflake_lock
    /// default_warehouse
    /// default_namespace
    /// default_role
    /// ext_authn_duo
    /// ext_authn_uid
    /// mins_to_bypass_mfa
    /// owner
    /// last_success_login
    /// expires_at_time
    /// locked_until_time
    /// has_password
    /// has_rsa_public_key
    /// 
    /// and DESCRIBE USER someone output:
    /// property	description
    /// COMMENT	user comment associated to an object in the dictionary
    /// DAYS_TO_EXPIRY	User record will be treated as expired after specified number of days
    /// DEFAULT_NAMESPACE	Default database namespace prefix for this user
    /// DEFAULT_ROLE	Primary principal of user session will be set to this role
    /// DEFAULT_WAREHOUSE	Default warehouse for this user
    /// DISABLED	Whether the user is disabled
    /// DISPLAY_NAME	Display name of the associated object
    /// EMAIL	Email address of the user
    /// EXT_AUTHN_DUO	Whether Duo Security is enabled as second factor authentication
    /// EXT_AUTHN_UID	External authentication ID of the user
    /// FIRST_NAME	First name of the user
    /// LAST_NAME	Last name of the user
    /// LOGIN_NAME	Login name of the user
    /// MIDDLE_NAME	Middle name of the user
    /// MINS_TO_BYPASS_MFA	Temporary bypass MFA for the user for a specified number of minutes
    /// MINS_TO_BYPASS_NETWORK_POLICY	Temporary bypass network policy on the user for a specified number of minutes
    /// MINS_TO_UNLOCK	Temporary lock on the user will be removed after specified number of minutes
    /// MUST_CHANGE_PASSWORD	User must change the password
    /// NAME	Name
    /// PASSWORD	Password of the user
    /// PASSWORD_LAST_SET_TIME	The timestamp on which the last non-null password was set for the user. Default to null if no password has been set yet.
    /// RSA_PUBLIC_KEY_2_FP	Fingerprint of user's second RSA public key.
    /// RSA_PUBLIC_KEY_FP	Fingerprint of user's RSA public key.
    /// SNOWFLAKE_LOCK	Whether the user or account is locked by Snowflake
    /// SNOWFLAKE_SUPPORT	Snowflake Support is allowed to use the user or account
    /// </summary>
    public class User
    {
        public string COMMENT { get; set; }
   
        public int? DAYS_TO_EXPIRY { get; set; }
        
        public string DEFAULT_NAMESPACE { get; set; }
        
        public string DEFAULT_ROLE { get; set; }
        
        public string DEFAULT_WAREHOUSE { get; set; }
        
        public bool DISABLED { get; set; }

        public string DISPLAY_NAME { get; set; }

        public string EMAIL { get; set; }

        public bool EXT_AUTHN_DUO { get; set; }
        
        public string EXT_AUTHN_UID { get; set; }

        public string FIRST_NAME { get; set; }

        public string LAST_NAME { get; set; }

        public string LOGIN_NAME { get; set; }

        public string MIDDLE_NAME { get; set; }

        public int? MINS_TO_BYPASS_MFA { get; set; }
        
        public int? MINS_TO_BYPASS_NETWORK_POLICY { get; set; }
        
        public int? MINS_TO_UNLOCK { get; set; }

        public bool MUST_CHANGE_PASSWORD { get; set; }

        public string NAME { get; set; }

        public DateTime? PASSWORD_LAST_SET_TIME { get; set; }

        public DateTime? PASSWORD_LAST_SET_TIMEUTC
        {
            get
            {
                if (this.PASSWORD_LAST_SET_TIME != null) 
                {
                    return this.PASSWORD_LAST_SET_TIME.Value.ToUniversalTime();
                }
                else
                { 
                    return null;
                }
            }
            set
            {
                /// Do nothing
            }
        }

        public string RSA_PUBLIC_KEY_2_FP { get; set; }

        public string RSA_PUBLIC_KEY_FP { get; set; }

        public bool SNOWFLAKE_LOCK { get; set; }

        public bool SNOWFLAKE_SUPPORT { get; set; }


        public DateTime CreatedOn { get; set; }

        public DateTime CreatedOnUTC
        {
            get
            {
                return this.CreatedOn.ToUniversalTime();
            }
            set
            {
                /// Do nothing
            }
        }

        public string Owner { get; set; }

        public bool IsObjectIdentifierSpecialCharacters { get; set; }

        public bool IsSSOEnabled
        { 
            get 
            {
                if (this.PASSWORD_LAST_SET_TIME != null) 
                {
                    return false;
                }
                else 
                {
                    return true;
                }
            }
        }

        public DateTime? LastLogon { get; set; }

        public DateTime? LastLogonUTC 
        {
            get
            {
                if (this.LastLogon != null) 
                {
                    return this.LastLogon.Value.ToUniversalTime();
                }
                else
                { 
                    return null;
                }
            }
            set
            {
                /// Do nothing
            }
        }

        public override String ToString()
        {
            return String.Format(
                "User: {0}/{1}, SSO {2}",
                this.NAME,
                this.LOGIN_NAME,
                this.IsSSOEnabled ? "on" : "off");
        }
    }
}
