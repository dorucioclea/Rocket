﻿using System.Threading.Tasks;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.IdentityServer.Devices;
using Aiwins.Rocket.IdentityServer.Grants;
using Aiwins.Rocket.Timing;
using Aiwins.Rocket.Uow;
using Microsoft.Extensions.Options;

namespace Aiwins.Rocket.IdentityServer.Tokens {
    public class TokenCleanupService : ITransientDependency {
        protected IPersistentGrantRepository PersistentGrantRepository { get; }
        protected IDeviceFlowCodesRepository DeviceFlowCodesRepository { get; }
        protected IClock Clock { get; }
        protected TokenCleanupOptions Options { get; }

        public TokenCleanupService (
            IPersistentGrantRepository persistentGrantRepository,
            IDeviceFlowCodesRepository deviceFlowCodesRepository,
            IClock clock,
            IOptions<TokenCleanupOptions> options) {
            PersistentGrantRepository = persistentGrantRepository;
            DeviceFlowCodesRepository = deviceFlowCodesRepository;
            Clock = clock;
            Options = options.Value;
        }

        public virtual async Task CleanAsync () {
            await RemoveGrantsAsync ();

            await RemoveDeviceCodesAsync ();
        }

        [UnitOfWork]
        protected virtual async Task RemoveGrantsAsync () {
            for (int i = 0; i < Options.CleanupLoopCount; i++) {
                var persistentGrants = await PersistentGrantRepository
                    .GetListByExpirationAsync (Clock.Now, Options.CleanupBatchSize);

                //TODO: Can be optimized if the repository implements the batch deletion
                foreach (var persistentGrant in persistentGrants) {
                    await PersistentGrantRepository
                        .DeleteAsync (persistentGrant);
                }

                //No need to continue to query if it gets more than max items.
                if (persistentGrants.Count < Options.CleanupBatchSize) {
                    break;
                }
            }
        }

        protected virtual async Task RemoveDeviceCodesAsync () {
            for (int i = 0; i < Options.CleanupLoopCount; i++) {
                var deviceFlowCodeses = await DeviceFlowCodesRepository
                    .GetListByExpirationAsync (Clock.Now, Options.CleanupBatchSize);

                //TODO: Can be optimized if the repository implements the batch deletion
                foreach (var deviceFlowCodes in deviceFlowCodeses) {
                    await DeviceFlowCodesRepository
                        .DeleteAsync (deviceFlowCodes);
                }

                //No need to continue to query if it gets more than max items.
                if (deviceFlowCodeses.Count < Options.CleanupBatchSize) {
                    break;
                }
            }
        }
    }
}