// Grants to Roles via Snowhouse
// snowsql -c snowhouse -f "SnowhouseGrantsToUsers.sql" -D DEPLOYMENT='deploymentoftheaccountyouwant' -D ACCOUNT_ID=accountidofaccountyouwant -o output_format=csv -o header=true -o timing=false -o friendly=false > "GRANTS_TO_ROLES.csv"

select
    t.granted_on as created_on,
    t.modified_on as modified_on,
    case when t.priv = 'U' then 'USAGE'
    when t.priv = 'T' then 'TRUNCATE'
    when t.priv = 'UP' then 'UPDATE'
    when t.priv = 'S' then 'SELECT'
    when t.priv = 'I' then 'INSERT'
    when t.priv = 'D' then 'DELETE'
    when t.priv = 'RU' then 'REFERENCE USAGE'
    when t.priv = 'OW' then 'OPERATE'
    when t.priv = 'O' then 'OWNERSHIP'
    when t.priv = 'R' then 'REFERENCES'
    when t.priv = 'MG' then 'MANAGE GRANTS'
    when t.priv = 'MN' then 'MONITOR'
    when t.priv = 'MNU' then 'MONITOR USAGE'
    when t.priv = 'MNE' then 'MONITOR EXECUTION'
    when t.priv = 'MNS' then 'MONITOR SECURITY'
    when t.priv = 'MNL' then 'MONITOR SELF SERVICE'
    when t.priv = 'MD' then 'MODIFY'
    when t.priv = 'MDL' then 'MODIFY SELF SERVICE'
    when t.priv = 'CT' then 'CREATE TABLE'
    when t.priv = 'CET' then 'CREATE EXTERNAL TABLE'
    when t.priv = 'CU' then 'CREATE USER'
    when t.priv = 'CR' then 'CREATE ROLE'
    when t.priv = 'CD' then 'CREATE DATABASE'
    when t.priv = 'CS' then 'CREATE SCHEMA'
    when t.priv = 'CSH' then 'CREATE SHARE'
    when t.priv = 'CST' then 'CREATE STAGE'
    when t.priv = 'CSQ' then 'CREATE SEQUENCE'
    when t.priv = 'CV' then 'CREATE VIEW'
    when t.priv = 'CMV' then 'CREATE MATERIALIZED VIEW'
    when t.priv = 'CFF' then 'CREATE FILE FORMAT'
    when t.priv = 'CFN' then 'CREATE FUNCTION'
    when t.priv = 'CPR' then 'CREATE PROCEDURE'
    when t.priv = 'CRM' then 'CREATE RESOURCE MONITOR'
    when t.priv = 'CP' then 'CREATE POLICY'
    when t.priv = 'CPP' then 'CREATE PIPE'
    when t.priv = 'CSTR' then 'CREATE STREAM'
    when t.priv = 'CTA' then 'CREATE TASK'
    when t.priv = 'CACC' then 'CREATE ACCOUNT'
    when t.priv = 'CINT' then 'CREATE INTEGRATION'
    when t.priv = 'CUDT' then 'CREATE TYPE'
    when t.priv = 'RD' then 'READ'
    when t.priv = 'WR' then 'WRITE'
    when t.priv = 'MGLOB' then 'MANAGE GLOBAL OBJECTS'
    when t.priv = 'MOA' then 'MANAGE ORGANIZATION ACCOUNTS'
    when t.priv = 'ISH' then 'IMPORT SHARE'
    when t.priv = 'APO' then 'ATTACH POLICY'
    when t.priv = 'SSUP' then 'SECURITY SUPPORT'
    when t.priv = 'MORG' then 'MANAGE ORGANIZATION'
    when t.priv = 'EXE' then 'EXECUTE TASK'
    when t.priv = 'ASO' then 'ADD SEARCH OPTIMIZATION'
    when t.priv = 'RB' then 'REBUILD'
    when t.priv = 'CMP' then 'CREATE MASKING POLICY'
    when t.priv = 'CTT' then 'CREATE TEMPORARY TABLE'
    end as privilege,
    t.type as granted_on,
    t.name as name,
    t.table_catalog as table_catalog,
    t.table_schema as table_schema,
    'ROLE' as granted_to,
    t2.dpo:"RoleDPO:primary".name::string as grantee_name,
    t.grant_option as grant_option,
    case when t.grantor_role_id = 0 then NULL
    else t3.dpo:"RoleDPO:primary".name::string end as granted_by,
    t.deleted_on as deleted_on
    from
    (
        (
        -- Schema
            select s2.dpo:"SchemaDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s3.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s2.dpo:"SchemaDPO:primary".name::string as table_schema,
            'SCHEMA' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.schema_etl_v as s2
            on s2.dpo:"SchemaDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s3
            on s2.dpo:"SchemaDPO:primary".parentId = s3.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'S'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s3.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s3.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Table
            select
            s2.dpo:"TableDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            case
            when s2.dpo:"TableDPO:primary".kindId = 9 then 'EXTERNAL TABLE'
            when s2.dpo:"TableDPO:primary".kindId = 3 then 'VIEW'
            when s2.dpo:"TableDPO:primary".kindId = 5 then 'TABLE FUNCTION'
            when s2.dpo:"TableDPO:primary".kindId = 8 then 'MATERIALIZED VIEW'
            else 'TABLE'
            end as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.table_etl_v as s2
            on s2.dpo:"TableDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"TableDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and (s1.dpo:"GrantRecDPO:changelog".securableType::string = 'T'
                or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'ET'
                or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'TF'
                or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'V'
                or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'MV')
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"TableDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s2.dpo:"TableDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- User
            select
            s2.dpo:"UserDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'USER' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.user_etl_v as s2
            on s2.dpo:"UserDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'U'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"UserDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"UserDPO:primary".tempId::int = 0
            and s2.dpo:"UserDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Role
            select
            s2.dpo:"RoleDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'ROLE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.role_etl_v as s2
            on s2.dpo:"RoleDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'R'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"RoleDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"RoleDPO:primary".deletedOn::int is NULL
            and s2.dpo:"RoleDPO:primary".tempId::int = 0
            and name not like 'SR/%'
        )
        union all
        (
        -- Warehouse
            select
            s2.dpo:"WarehouseDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'WAREHOUSE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.warehouse_etl_v as s2
            on s2.dpo:"WarehouseDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'W'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"WarehouseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"WarehouseDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Resource Monitor
            select
            s2.dpo:"ResourceMonitorDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'RESOURCE MONITOR' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.resource_monitor_etl_v as s2
            on s2.dpo:"ResourceMonitorDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'RM'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"ResourceMonitorDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"ResourceMonitorDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Database
            select
            s2.dpo:"DatabaseDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s2.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            NULL as table_schema,
            'DATABASE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.database_etl_v as s2
            on s2.dpo:"DatabaseDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'D'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s2.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Stage
            select
            s2.dpo:"StageDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'STAGE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.stage_etl_v as s2
            on s2.dpo:"StageDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"StageDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'ST'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"StageDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"StageDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Sequence
            select
            s2.dpo:"SequenceDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'SEQUENCE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.sequence_etl_v as s2
            on s2.dpo:"SequenceDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"SequenceDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SQ'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"SequenceDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"SequenceDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- FileFormat
            select
            s2.dpo:"FileFormatDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'FILE FORMAT' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.file_format_etl_v as s2
            on s2.dpo:"FileFormatDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"FileFormatDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'FF'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"FileFormatDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"FileFormatDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- PIPE
            select
            s2.dpo:"PipeDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'PIPE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.pipe_etl_v as s2
            on s2.dpo:"PipeDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"PipeDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'PI'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"PipeDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"PipeDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- MANAGED_ACCOUNT
            select
            s2.dpo:"ManagedAccountDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'MANAGED ACCOUNT' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.managed_account_etl_v as s2
            on s2.dpo:"ManagedAccountDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'MA'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"ManagedAccountDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"ManagedAccountDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- ACCOUNT
            select
            s2.dpo:"AccountDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'ACCOUNT' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.account_etl_v as s2
            on s2.dpo:"AccountDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'A'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            -- and s2.dpo:"AccountDPO:primary".id::int = &ACCOUNT_ID
            and s2.dpo:"AccountDPO:primary".id = &ACCOUNT_ID::string
            and s2.dpo:"AccountDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Function, Procedure
            select
            s2.dpo:"FunctionDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            case when s1.dpo:"GrantRecDPO:changelog".securableType::string = 'FN'
            then 'FUNCTION'
            else 'PROCEDURE' end as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.function_etl_v as s2
            on s2.dpo:"FunctionDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"FunctionDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where (s1.dpo:"GrantRecDPO:changelog".securableType::string = 'FN' or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SP')
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"FunctionDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"FunctionDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Share
            select
            s2.dpo:"ShareDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'SHARE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.share_etl_v as s2
            on s2.dpo:"ShareDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SH'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"ShareDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"ShareDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Stream
            select
            s2.dpo:"StreamDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'STREAM' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.stream_etl_v as s2
            on s2.dpo:"StreamDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.dpo:"StreamDPO:primary".parentId = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'STR'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"StreamDPO:primary".accountId::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"StreamDPO:primary".deletedOn::int is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Task
            select
            s2.name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            s4.dpo:"DatabaseDPO:primary".name::string as table_catalog,
            s3.dpo:"SchemaDPO:primary".name::string as table_schema,
            'TASK' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.user_task_etl_v as s2
            on s2.id = s1.dpo:"GrantRecDPO:changelog".securableId
            left join snowhouse_import.&DEPLOYMENT.schema_etl_v as s3
            on s2.parent_Id = s3.dpo:"SchemaDPO:primary".id
            left join snowhouse_import.&DEPLOYMENT.database_etl_v as s4
            on s3.dpo:"SchemaDPO:primary".parentId = s4.dpo:"DatabaseDPO:primary".id
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'TA'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.account_Id::int = &ACCOUNT_ID
            and s3.dpo:"SchemaDPO:primary".accountId::int = &ACCOUNT_ID
            and s4.dpo:"DatabaseDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.deleted_On is NULL
            and s3.dpo:"SchemaDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".deletedOn::int is NULL
            and s4.dpo:"DatabaseDPO:primary".tempId::int = 0
        )
        union all
        (
        -- Integration
            select
            s2.dpo:"IntegrationDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'INTEGRATION' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.integration_etl_v as s2
            on s2.dpo:"IntegrationDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'IN'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"IntegrationDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"IntegrationDPO:primary".deletedOn::int is NULL
        )
        union all
        (
        -- Transaction
            select
            s2.dpo:"TransactionDPO:primary".name::string as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'TRANSACTION' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.transaction_etl_v as s2
            on s2.dpo:"TransactionDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'TR'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"TransactionDPO:primary".accountId::int = &ACCOUNT_ID
        )
        union all
        (
        -- Session Variable
            select
            s2.dpo:"SessionVariableDPO:primary".name as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'SESSION VARIABLE' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.session_variable_etl_v as s2
            on s2.dpo:"SessionVariableDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SV'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"SessionVariableDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"SessionVariableDPO:primary".deletedOn is NULL
        )
        union all
        (
        -- Network Policy
            select
            s2.dpo:"NetworkPolicyDPO:primary".name as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'NETWORK POLICY' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.network_policy_etl_v as s2
            on s2.dpo:"NetworkPolicyDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'NP'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"NetworkPolicyDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"NetworkPolicyDPO:primary".deletedOn::int is NULL

        )
        union all
        (
            -- Notification Subscription
            select
            s2.dpo:"NotificationSubscriptionDPO:primary".name as name,
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            'NOTIFICATION SUBSCRIPTION' as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            inner join snowhouse_import.&DEPLOYMENT.notification_subscription_etl_v as s2
            on s2.dpo:"NotificationSubscriptionDPO:primary".id = s1.dpo:"GrantRecDPO:changelog".securableId
            where s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SUB'
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
            and s2.dpo:"NotificationSubscriptionDPO:primary".accountId::int = &ACCOUNT_ID
            and s2.dpo:"NotificationSubscriptionDPO:primary".deletedOn::int is NULL
        )
        union all
        (
            select
            '' as name,
            -- We don't have these info in Snowhouse, so DELETED OBJECTS MAY SHOW UP HERE.
            case when s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0 then NULL
            else (s1.dpo:"GrantRecDPO:changelog".deletedOn::int/1000)::timestamp_ltz end as deleted_on,
            NULL as table_catalog,
            NULL as table_schema,
            case when s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SHF' then 'SHARE FOREIGN'
            when s1.dpo:"GrantRecDPO:changelog".securableType::string = 'VA' then 'VALUES'
            when s1.dpo:"GrantRecDPO:changelog".securableType::string = 'CONN' then 'CONNECTION'
            when s1.dpo:"GrantRecDPO:changelog".securableType::string = 'UDT' then 'USER DEFINED TYPE'
            end as type,
            s1.dpo:"GrantRecDPO:changelog".priv::string as priv,
            (s1.dpo:"GrantRecDPO:changelog".grantedOn::int/1000)::timestamp_ltz as granted_on,
            s1.dpo:"GrantRecDPO:changelog".grantOption::boolean as grant_option,
            s1.dpo:"GrantRecDPO:changelog".granteeRoleId::int as grantee_role_id,
            s1.dpo:"GrantRecDPO:changelog".grantorRoleId::int as grantor_role_id,
            (s1.dpo:"GrantRecDPO:changelog".modifiedOn::int/1000)::timestamp_ltz as modified_on
            from snowhouse_import.&DEPLOYMENT.grant_rec_etl_v as s1
            where s1.dpo:"GrantRecDPO:changelog".deletedOn::int = 0
            and (s1.dpo:"GrantRecDPO:changelog".securableType::string = 'SHF'
            or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'VA'
            or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'CONN'
            or s1.dpo:"GrantRecDPO:changelog".securableType::string = 'UDT')
            and s1.dpo:"GrantRecDPO:changelog".granteeAccountId::int = &ACCOUNT_ID
        )
    ) as t
    left join snowhouse_import.&DEPLOYMENT.role_etl_v as t2
    on t2.dpo:"RoleDPO:primary".id::int = t.grantee_role_id
    left join snowhouse_import.&DEPLOYMENT.role_etl_v as t3
    on t3.dpo:"RoleDPO:primary".id::int = t.grantor_role_id
    where t2.dpo:"RoleDPO:primary".deletedOn::int is NULL
    and t2.dpo:"RoleDPO:primary".name::string not like 'SR/%'
    -- SNOW-181198 hide grants from roles not present in default namespace.
    and t2.dpo:"RoleDPO:primary".tempId::int = 0
    and (t.priv = 'U'
        or t.priv = 'T'
        or t.priv = 'UP'
        or t.priv = 'S'
        or t.priv = 'I'
        or t.priv = 'D'
        or t.priv = 'RU'
        or t.priv = 'OW'
        or t.priv = 'O'
        or t.priv = 'R'
        or t.priv = 'MG'
        or t.priv = 'MN'
        or t.priv = 'MNU'
        or t.priv = 'MNE'
        or t.priv = 'MNS'
        or t.priv = 'MNL'
        or t.priv = 'MD'
        or t.priv = 'MDL'
        or t.priv = 'CT'
        or t.priv = 'CET'
        or t.priv = 'CU'
        or t.priv = 'CR'
        or t.priv = 'CD'
        or t.priv = 'CS'
        or t.priv = 'CSH'
        or t.priv = 'CST'
        or t.priv = 'CSQ'
        or t.priv = 'CV'
        or t.priv = 'CMV'
        or t.priv = 'CFF'
        or t.priv = 'CFN'
        or t.priv = 'CPR'
        or t.priv = 'CRM'
        or t.priv = 'CP'
        or t.priv = 'CPP'
        or t.priv = 'CSTR'
        or t.priv = 'CTA'
        or t.priv = 'CACC'
        or t.priv = 'CINT'
        or t.priv = 'CUDT'
        or t.priv = 'RD'
        or t.priv = 'WR'
        or t.priv = 'MGLOB'
        or t.priv = 'MOA'
        or t.priv = 'ISH'
        or t.priv = 'APO'
        or t.priv = 'SSUP'
        or t.priv = 'MORG'
        or t.priv = 'EXE'
        or t.priv = 'ASO'
        or t.priv = 'RB'
        or t.priv = 'CMP'
        or t.priv = 'CTT')