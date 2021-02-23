// Grants to Users via Snowhouse
// snowsql -c snowhouse -f "SnowhouseGrantsToUsers.sql" -D DEPLOYMENT='deploymentoftheaccountyouwant' -D ACCOUNT_ID=accountidofaccountyouwant -o output_format=csv -o header=true -o timing=false -o friendly=false > "GRANTS_TO_USERS.csv"

select
  (user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".grantedOn::int/1000)::timestamp_ltz AS created_on
  , coalesce(
      least(
        iff(
          user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".deletedOn::int = 0
          , NULL
          , (user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".deletedOn::int/1000)::timestamp_ltz
        )
        , (user_etl.dpo:"UserDPO:primary".deletedOn::int/1000)::timestamp_ltz
      )
      , (user_etl.dpo:"UserDPO:primary".deletedOn::int/1000)::timestamp_ltz
      , iff(
          user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".deletedOn::int = 0
          , NULL
          , (user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".deletedOn::int/1000)::timestamp_ltz
        )
    )
    as deleted_on
  , granted_role_etl.dpo:"RoleDPO:primary".name::string as role
  , 'USER' AS granted_to
  , user_etl.dpo:"UserDPO:primary".name::string AS grantee_name
  , grantor_role_etl.dpo:"RoleDPO:primary".name::string AS granted_by
from snowhouse_import.&DEPLOYMENT.user_role_grant_etl_v as user_role_grant_etl
left join snowhouse_import.&DEPLOYMENT.user_etl_v as user_etl
on user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".userAccountId::int = &ACCOUNT_ID
and user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".userId::int = user_etl.dpo:"UserDPO:primary".id::int
left join snowhouse_import.&DEPLOYMENT.role_etl_v as granted_role_etl
on user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".grantedRoleAccountId::int = &ACCOUNT_ID
and user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".grantedRoleId::int = granted_role_etl.dpo:"RoleDPO:primary".id::int
left join snowhouse_import.&DEPLOYMENT.role_etl_v as grantor_role_etl
on user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".grantorAccountId::int = &ACCOUNT_ID
and user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".grantorRoleId::int = grantor_role_etl.dpo:"RoleDPO:primary".id::int
where user_role_grant_etl.dpo:"UserRoleGrantDPO:changelog".userAccountId::int = &ACCOUNT_ID
and user_etl.dpo:"UserDPO:primary".tempId::int = 0
and nvl(grantor_role_etl.dpo:"RoleDPO:primary".tempId::int, 0) = 0
and granted_role_etl.dpo:"RoleDPO:primary".tempId::int = 0;