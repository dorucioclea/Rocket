﻿using Aiwins.Rocket.Reflection;

namespace Aiwins.Rocket.TenantManagement
{
    public static class TenantManagementPermissions
    {
        public const string GroupName = "RocketTenantManagement";

        public static class Tenants
        {
            public const string Default = GroupName + ".Tenants";
            public const string Create = Default + ".Create";
            public const string Update = Default + ".Update";
            public const string Delete = Default + ".Delete";
            public const string ManageFeatures = Default + ".ManageFeatures";
            public const string ManageConnectionStrings = Default + ".ManageConnectionStrings";
        }

        public static string[] GetAll()
        {
            return ReflectionHelper.GetPublicConstantsRecursively(typeof(TenantManagementPermissions));
        }
    }
}