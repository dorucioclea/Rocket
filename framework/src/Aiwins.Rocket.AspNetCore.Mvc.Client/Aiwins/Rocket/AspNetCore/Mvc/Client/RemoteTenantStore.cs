﻿using System;
using System.Threading.Tasks;
using Aiwins.Rocket.AspNetCore.Mvc.MultiTenancy;
using Aiwins.Rocket.Caching;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Http.Client.DynamicProxying;
using Aiwins.Rocket.MultiTenancy;
using Aiwins.Rocket.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace Aiwins.Rocket.AspNetCore.Mvc.Client {
    public class RemoteTenantStore : ITenantStore, ITransientDependency {
        protected IHttpClientProxy<IRocketTenantAppService> Proxy { get; }
        protected IHttpContextAccessor HttpContextAccessor { get; }
        protected IDistributedCache<TenantConfiguration> Cache { get; }

        public RemoteTenantStore (
            IHttpClientProxy<IRocketTenantAppService> proxy,
            IHttpContextAccessor httpContextAccessor,
            IDistributedCache<TenantConfiguration> cache) {
            Proxy = proxy;
            HttpContextAccessor = httpContextAccessor;
            Cache = cache;
        }

        public async Task<TenantConfiguration> FindAsync (string name) {
            var cacheKey = CreateCacheKey (name);
            var httpContext = HttpContextAccessor?.HttpContext;

            if (httpContext != null && httpContext.Items[cacheKey] is TenantConfiguration tenantConfiguration) {
                return tenantConfiguration;
            }

            tenantConfiguration = await Cache.GetOrAddAsync (
                cacheKey,
                async () => CreateTenantConfiguration (await Proxy.Service.FindTenantByNameAsync (name)),
                () => new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes (5) //TODO: Should be configurable.
                }
            );

            if (httpContext != null) {
                httpContext.Items[cacheKey] = tenantConfiguration;
            }

            return tenantConfiguration;
        }

        public async Task<TenantConfiguration> FindAsync (Guid id) {
            var cacheKey = CreateCacheKey (id);
            var httpContext = HttpContextAccessor?.HttpContext;

            if (httpContext != null && httpContext.Items[cacheKey] is TenantConfiguration tenantConfiguration) {
                return tenantConfiguration;
            }

            tenantConfiguration = await Cache.GetOrAddAsync (
                cacheKey,
                async () => CreateTenantConfiguration (await Proxy.Service.FindTenantByIdAsync (id)),
                () => new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes (5) //TODO: Should be configurable.
                }
            );

            if (httpContext != null) {
                httpContext.Items[cacheKey] = tenantConfiguration;
            }

            return tenantConfiguration;
        }

        public TenantConfiguration Find (string name) {
            var cacheKey = CreateCacheKey (name);
            var httpContext = HttpContextAccessor?.HttpContext;

            if (httpContext != null && httpContext.Items[cacheKey] is TenantConfiguration tenantConfiguration) {
                return tenantConfiguration;
            }

            tenantConfiguration = Cache.GetOrAdd (
                cacheKey,
                () => AsyncHelper.RunSync (async () => CreateTenantConfiguration (await Proxy.Service.FindTenantByNameAsync (name))),
                () => new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes (5) //TODO: Should be configurable.
                }
            );

            if (httpContext != null) {
                httpContext.Items[cacheKey] = tenantConfiguration;
            }

            return tenantConfiguration;
        }

        public TenantConfiguration Find (Guid id) {
            var cacheKey = CreateCacheKey (id);
            var httpContext = HttpContextAccessor?.HttpContext;

            if (httpContext != null && httpContext.Items[cacheKey] is TenantConfiguration tenantConfiguration) {
                return tenantConfiguration;
            }

            tenantConfiguration = Cache.GetOrAdd (
                cacheKey,
                () => AsyncHelper.RunSync (async () => CreateTenantConfiguration (await Proxy.Service.FindTenantByIdAsync (id))),
                () => new DistributedCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow =
                        TimeSpan.FromMinutes (5) //TODO: Should be configurable.
                }
            );

            if (httpContext != null) {
                httpContext.Items[cacheKey] = tenantConfiguration;
            }

            return tenantConfiguration;
        }

        protected virtual TenantConfiguration CreateTenantConfiguration (FindTenantResultDto tenantResultDto) {
            if (!tenantResultDto.Success || tenantResultDto.TenantId == null) {
                return null;
            }

            return new TenantConfiguration (tenantResultDto.TenantId.Value, tenantResultDto.Name);
        }

        protected virtual string CreateCacheKey (string tenantName) {
            return $"RemoteTenantStore_Name_{tenantName}";
        }

        protected virtual string CreateCacheKey (Guid tenantId) {
            return $"RemoteTenantStore_Id_{tenantId:N}";
        }
    }
}