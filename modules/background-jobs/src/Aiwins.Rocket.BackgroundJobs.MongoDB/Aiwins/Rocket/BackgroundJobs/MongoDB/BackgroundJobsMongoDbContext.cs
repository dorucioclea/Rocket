﻿using Aiwins.Rocket.Data;
using Aiwins.Rocket.MongoDB;
using MongoDB.Driver;

namespace Aiwins.Rocket.BackgroundJobs.MongoDB {
    [ConnectionStringName (BackgroundJobsDbProperties.ConnectionStringName)]
    public class BackgroundJobsMongoDbContext : RocketMongoDbContext, IBackgroundJobsMongoDbContext {
        public IMongoCollection<BackgroundJobRecord> BackgroundJobs { get; set; }

        protected override void CreateModel (IMongoModelBuilder modelBuilder) {
            base.CreateModel (modelBuilder);

            modelBuilder.ConfigureBackgroundJobs ();
        }
    }
}