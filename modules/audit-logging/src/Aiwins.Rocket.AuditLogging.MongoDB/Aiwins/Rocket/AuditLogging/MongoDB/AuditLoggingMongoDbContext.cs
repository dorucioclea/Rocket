﻿using MongoDB.Driver;
using Aiwins.Rocket.Data;
using Aiwins.Rocket.MongoDB;

namespace Aiwins.Rocket.AuditLogging.MongoDB
{
    [ConnectionStringName(RocketAuditLoggingDbProperties.ConnectionStringName)]
    public class AuditLoggingMongoDbContext : RocketMongoDbContext, IAuditLoggingMongoDbContext
    {
        public IMongoCollection<AuditLog> AuditLogs => Collection<AuditLog>();

        protected override void CreateModel(IMongoModelBuilder modelBuilder)
        {
            base.CreateModel(modelBuilder);

            modelBuilder.ConfigureAuditLogging();
        }
    }
}
