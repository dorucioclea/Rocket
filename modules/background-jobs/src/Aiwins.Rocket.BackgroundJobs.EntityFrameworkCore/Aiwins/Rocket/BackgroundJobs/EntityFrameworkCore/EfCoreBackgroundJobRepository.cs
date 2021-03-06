﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aiwins.Rocket.Domain.Repositories.EntityFrameworkCore;
using Aiwins.Rocket.EntityFrameworkCore;
using Aiwins.Rocket.Timing;
using Microsoft.EntityFrameworkCore;

namespace Aiwins.Rocket.BackgroundJobs.EntityFrameworkCore {
    public class EfCoreBackgroundJobRepository : EfCoreRepository<IBackgroundJobsDbContext, BackgroundJobRecord, Guid>, IBackgroundJobRepository {
        protected IClock Clock { get; }

        public EfCoreBackgroundJobRepository (
            IDbContextProvider<IBackgroundJobsDbContext> dbContextProvider,
            IClock clock) : base (dbContextProvider) {
            Clock = clock;
        }

        public virtual async Task<List<BackgroundJobRecord>> GetWaitingListAsync (int maxResultCount) {
            return await GetWaitingListQuery (maxResultCount)
                .ToListAsync ();
        }

        protected virtual IQueryable<BackgroundJobRecord> GetWaitingListQuery (int maxResultCount) {
            var now = Clock.Now;
            return DbSet
                .Where (t => !t.IsAbandoned && t.NextTryTime <= now)
                .OrderByDescending (t => t.Priority)
                .ThenBy (t => t.TryCount)
                .ThenBy (t => t.NextTryTime)
                .Take (maxResultCount);
        }
    }
}