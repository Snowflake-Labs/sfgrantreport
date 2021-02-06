using System;
using System.Collections.Generic;
using System.Linq;

namespace Snowflake.GrantReport.ReportObjects
{
    public class Grant
    {
        Dictionary<string, string> privilegeNamesShortDict = new Dictionary<string, string>
        {
            // Common
            {"USAGE", "U"},
            {"OWNERSHIP", "O"},

            {"MODIFY", "M"},
            {"MONITOR", "MON"},

            // Database
            {"CREATE SCHEMA", "SCHM"},
            {"IMPORTED PRIVILEGES", "IMP_PRV"},
            {"REFERENCE_USAGE", "REF_USG"},

            // Schema
            {"ADD SEARCH OPTIMIZATION", "SEO"},
            {"CREATE EXTERNAL TABLE", "TBL_EXT"},
            {"CREATE FILE FORMAT", "FF"},
            {"CREATE FUNCTION", "FUNC"},
            {"CREATE MASKING POLICY", "MSKPOL"},
            {"CREATE MATERIALIZED VIEW", "MV"},
            {"CREATE PIPE", "PIPE"},
            {"CREATE PROCEDURE", "PROC"},
            {"CREATE SEQUENCE", "SEQ"},
            {"CREATE STAGE", "STG"},
            {"CREATE STREAM", "STRM"},
            {"CREATE TABLE", "TBL"},
            {"CREATE TASK", "TASK"},
            {"CREATE TEMPORARY TABLE", "TBL_TMP"},
            {"CREATE VIEW", "VIEW"},

            // Table
            {"INSERT", "C"},
            {"SELECT", "R"},
            {"UPDATE", "U"},
            {"DELETE", "D"},
            {"TRUNCATE", "T"},

            // View
            {"REBUILD", "RBLD"},
            {"REFERENCES", "REF"}
        };

        Dictionary<string, int> privilegeOrderDict = new Dictionary<string, int>
        {
            // Common
            {"USAGE",       1},
            {"OWNERSHIP",   2},

            // Database
            {"CREATE SCHEMA",       120},
            {"IMPORTED PRIVILEGES", 121},
            {"REFERENCE_USAGE",     122},

            // Schema
            {"CREATE TABLE",                200},
            {"CREATE TEMPORARY TABLE",      201},
            {"CREATE EXTERNAL TABLE",       202},
            {"CREATE VIEW",                 203},
            {"CREATE MATERIALIZED VIEW",    204},
            {"CREATE PROCEDURE",            205},
            {"CREATE FUNCTION",             206},
            {"CREATE STAGE",                207},
            {"CREATE FILE FORMAT",          208},
            {"CREATE TASK",                 209},
            {"CREATE PIPE",                 210},
            {"CREATE SEQUENCE",             211},
            {"CREATE STREAM",               212},
            {"CREATE MASKING POLICY",       213},

            {"ADD SEARCH OPTIMIZATION",     200},

            // Table
            {"INSERT",      10},
            {"SELECT",      11},
            {"UPDATE",      12},
            {"DELETE",      13},
            {"TRUNCATE",    14},

            // View
            {"REBUILD",     15},
            {"REFERENCES",  16},

            // Common
            {"MODIFY",      50},
            {"MONITOR",     51}
        };

        public DateTime CreatedOn { get; set; }

        public DateTime CreatedOnUTC
        {
            get
            {
                return this.CreatedOn.ToUniversalTime();
            }
            set
            {
                // Do nothing
            }
        }

        public string DBName { get; set; }

        public string EntityName { get; set; }

        public string GrantedBy { get; set; }

        public string GrantedTo { get; set; }

        public bool WithGrantOption { get; set; }

        private string _objectName = String.Empty;
        public string ObjectName 
        { 
            get
            {
                return this._objectName;
            }
            set
            {
                this._objectName = value;

                string[] nameParts = this._objectName.Split('.');
                if (nameParts.Length == 0)
                {
                    this.EntityName = this._objectName.Trim('"');
                }
                else if (nameParts.Length == 1)
                {
                    this.EntityName = this._objectName.Trim('"');
                }
                else if (nameParts.Length == 2)
                {
                    this.DBName = nameParts[0].Trim('"');
                    this.SchemaName = nameParts[1].Trim('"');
                    this.EntityName = nameParts[1].Trim('"');
                }
                else if (nameParts.Length == 3)
                {
                    this.DBName = nameParts[0].Trim('"');
                    this.SchemaName = nameParts[1].Trim('"');
                    this.EntityName = nameParts[2].Trim('"');
                }
            }
        }

        public string ObjectNameUnquoted
        {
            get 
            {
                if (this.DBName.Length == 0)
                {
                    return this.EntityName;
                }
                else
                {
                    if (this.ObjectType == "SCHEMA")
                    {
                        return String.Format("{0}.{1}", this.DBName, this.EntityName);
                    }
                    else
                    {
                        return String.Format("{0}.{1}.{2}", this.DBName, this.SchemaName, this.EntityName);
                    }
                }
            }
        }

        public string ObjectType { get; set; }

        public string Privilege { get; set; }

        public string PrivilegeDisplayShort
        { 
            get
            {
                string shortName = String.Empty;
                if (privilegeNamesShortDict.TryGetValue(this.Privilege, out shortName) == true)
                {
                    if (this.WithGrantOption == true)
                    {
                        return String.Format("{0}+", shortName);
                    }
                    else
                    {
                        return shortName;
                    }
                }
                else
                {
                    // Take first two characters
                    string[] words = this.Privilege.Split(' ');
                    List<string> shorterWords = new List<string>(words.Length);
                    foreach (string word in words)
                    {
                        shorterWords.Add(word.Substring(0, 2));

                    }
                    shortName = String.Join('_', shorterWords.ToArray());
                    return shortName;
                }
            }
        }

        public int PrivilegeOrder
        { 
            get
            {
                int order = -1;
                if (privilegeOrderDict.TryGetValue(this.Privilege, out order) == true)
                {
                    return order;
                }
                else
                {
                    return 1000;
                }
            }
        }

        public string PrivilegeDisplayLong
        { 
            get
            {
                if (this.WithGrantOption == true)
                {
                    return String.Format("{0}+", this.Privilege);
                }
                else
                {
                    return this.Privilege;
                }
            }
        }

        public string SchemaName { get; set; }

        public string UniqueIdentifier
        { 
            get
            {
                return String.Format("{0}-{1}-{2}-{3}-{4}", this.Privilege, this.ObjectType, this.ObjectName, this.GrantedTo, this.GrantedBy);
            }        
        }

        public override String ToString()
        {
            return String.Format(
                "Grant: {0} on {1} [{2}] to {3} is granted by {4} with grant option {5}",
                this.Privilege,
                this.ObjectName,
                this.ObjectType,
                this.GrantedTo,
                this.GrantedBy,
                this.WithGrantOption ? "On" : "Off");
        }
    }
}
