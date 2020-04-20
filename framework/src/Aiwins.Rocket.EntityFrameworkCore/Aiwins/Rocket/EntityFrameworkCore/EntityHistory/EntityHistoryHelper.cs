using System;
using System.Collections.Generic;
using System.Linq;
using Aiwins.Rocket.Auditing;
using Aiwins.Rocket.Data;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Domain.Entities;
using Aiwins.Rocket.Json;
using Aiwins.Rocket.MultiTenancy;
using Aiwins.Rocket.Reflection;
using Aiwins.Rocket.Timing;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aiwins.Rocket.EntityFrameworkCore.EntityHistory {
    public class EntityHistoryHelper : IEntityHistoryHelper, ITransientDependency {
        public ILogger<EntityHistoryHelper> Logger { get; set; }

        protected IAuditingStore AuditingStore { get; }
        protected IJsonSerializer JsonSerializer { get; }
        protected RocketAuditingOptions Options { get; }
        protected IAuditingHelper AuditingHelper { get; }

        private readonly IClock _clock;

        public EntityHistoryHelper (
            IAuditingStore auditingStore,
            IOptions<RocketAuditingOptions> options,
            IClock clock,
            IJsonSerializer jsonSerializer,
            IAuditingHelper auditingHelper) {
            _clock = clock;
            AuditingStore = auditingStore;
            JsonSerializer = jsonSerializer;
            AuditingHelper = auditingHelper;
            Options = options.Value;

            Logger = NullLogger<EntityHistoryHelper>.Instance;
        }

        public virtual List<EntityChangeInfo> CreateChangeList (ICollection<EntityEntry> entityEntries) {
            var list = new List<EntityChangeInfo> ();

            foreach (var entry in entityEntries) {
                if (!ShouldSaveEntityHistory (entry)) {
                    continue;
                }

                var entityChange = CreateEntityChangeOrNull (entry);
                if (entityChange == null) {
                    continue;
                }

                list.Add (entityChange);
            }

            return list;
        }

        [CanBeNull]
        private EntityChangeInfo CreateEntityChangeOrNull (EntityEntry entityEntry) {
            var entity = entityEntry.Entity;

            EntityChangeType changeType;
            switch (entityEntry.State) {
                case EntityState.Added:
                    changeType = EntityChangeType.Created;
                    break;
                case EntityState.Deleted:
                    changeType = EntityChangeType.Deleted;
                    break;
                case EntityState.Modified:
                    changeType = IsDeleted (entityEntry) ? EntityChangeType.Deleted : EntityChangeType.Updated;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                default:
                    return null;
            }

            var entityId = GetEntityId (entity);
            if (entityId == null && changeType != EntityChangeType.Created) {
                return null;
            }

            var entityType = entity.GetType ();
            var entityChange = new EntityChangeInfo {
                ChangeType = changeType,
                EntityEntry = entityEntry,
                EntityId = entityId,
                EntityTypeFullName = entityType.FullName,
                PropertyChanges = GetPropertyChanges (entityEntry),
                EntityTenantId = GetTenantId (entity)
            };

            return entityChange;
        }

        protected virtual Guid? GetTenantId (object entity) {
            if (!(entity is IMultiTenant multiTenantEntity)) {
                return null;
            }

            return multiTenantEntity.TenantId;
        }

        private DateTimeOffset GetChangeTime (EntityChangeInfo entityChange) {
            var entity = entityChange.EntityEntry.As<EntityEntry> ().Entity;
            switch (entityChange.ChangeType) {
                case EntityChangeType.Created:
                    return (entity as IHasCreationTime)?.CreationTime ?? _clock.Now;
                case EntityChangeType.Deleted:
                    return (entity as IHasDeletionTime)?.DeletionTime ?? _clock.Now;
                case EntityChangeType.Updated:
                    return (entity as IHasModificationTime)?.LastModificationTime ?? _clock.Now;
                default:
                    throw new RocketException ($"Unknown {nameof(EntityChangeInfo)}: {entityChange}");
            }
        }

        private string GetEntityId (object entityAsObj) {
            if (!(entityAsObj is IEntity entity)) {
                throw new RocketException ($"Entities should implement the {typeof(IEntity).AssemblyQualifiedName} interface! Given entity does not implement it: {entityAsObj.GetType().AssemblyQualifiedName}");
            }

            var keys = entity.GetKeys ();
            if (keys.All (k => k == null)) {
                return null;
            }

            return keys.JoinAsString (",");
        }

        /// <summary>
        /// 获取实体属性的变更
        /// </summary>
        private List<EntityPropertyChangeInfo> GetPropertyChanges (EntityEntry entityEntry) {
            var propertyChanges = new List<EntityPropertyChangeInfo> ();
            var properties = entityEntry.Metadata.GetProperties ();
            var isCreated = IsCreated (entityEntry);
            var isDeleted = IsDeleted (entityEntry);

            foreach (var property in properties) {
                var propertyEntry = entityEntry.Property (property.Name);
                if (ShouldSavePropertyHistory (propertyEntry, isCreated || isDeleted)) {
                    propertyChanges.Add (new EntityPropertyChangeInfo {
                        NewValue = isDeleted ? null : JsonSerializer.Serialize (propertyEntry.CurrentValue).TruncateWithPostfix (EntityPropertyChangeInfo.MaxValueLength),
                            OriginalValue = isCreated ? null : JsonSerializer.Serialize (propertyEntry.OriginalValue).TruncateWithPostfix (EntityPropertyChangeInfo.MaxValueLength),
                            PropertyName = property.Name,
                            PropertyTypeFullName = property.ClrType.GetFirstGenericArgumentIfNullable ().FullName
                    });
                }
            }

            return propertyChanges;
        }

        private bool IsCreated (EntityEntry entityEntry) {
            return entityEntry.State == EntityState.Added;
        }

        private bool IsDeleted (EntityEntry entityEntry) {
            if (entityEntry.State == EntityState.Deleted) {
                return true;
            }

            var entity = entityEntry.Entity;
            return entity is ISoftDelete && entity.As<ISoftDelete> ().IsDeleted;
        }

        private bool ShouldSaveEntityHistory (EntityEntry entityEntry, bool defaultValue = false) {
            if (entityEntry.State == EntityState.Detached ||
                entityEntry.State == EntityState.Unchanged) {
                return false;
            }

            var entityType = entityEntry.Metadata.ClrType;

            if (!EntityHelper.IsEntity (entityType)) {
                return false;
            }

            if (AuditingHelper.IsEntityHistoryEnabled (entityType)) {
                return true;
            }

            return defaultValue;
        }

        private bool ShouldSavePropertyHistory (PropertyEntry propertyEntry, bool defaultValue) {
            if (propertyEntry.Metadata.Name == "Id") {
                return false;
            }

            var propertyInfo = propertyEntry.Metadata.PropertyInfo;
            if (propertyInfo != null && propertyInfo.IsDefined (typeof (DisableAuditingAttribute), true)) {
                return false;
            }

            var entityType = propertyEntry.EntityEntry.Entity.GetType ();
            if (entityType.IsDefined (typeof (DisableAuditingAttribute), true)) {
                if (propertyInfo == null || !propertyInfo.IsDefined (typeof (AuditedAttribute), true)) {
                    return false;
                }
            }

            if (propertyEntry.IsModified) {
                return true;
            }

            return defaultValue;
        }

        /// <summary>
        /// Updates change time, entity id and foreign keys after SaveChanges is called.
        /// </summary>
        public void UpdateChangeList (List<EntityChangeInfo> entityChanges) {
            foreach (var entityChange in entityChanges) {
                /* Update change time */

                entityChange.ChangeTime = GetChangeTime (entityChange);

                /* Update entity id */

                var entityEntry = entityChange.EntityEntry.As<EntityEntry> ();
                entityChange.EntityId = GetEntityId (entityEntry.Entity);

                /* Update foreign keys */

                var foreignKeys = entityEntry.Metadata.GetForeignKeys ();

                foreach (var foreignKey in foreignKeys) {
                    foreach (var property in foreignKey.Properties) {
                        var propertyEntry = entityEntry.Property (property.Name);
                        var propertyChange = entityChange.PropertyChanges.FirstOrDefault (pc => pc.PropertyName == property.Name);

                        if (propertyChange == null) {
                            if (!(propertyEntry.OriginalValue?.Equals (propertyEntry.CurrentValue) ?? propertyEntry.CurrentValue == null)) {
                                // Add foreign key
                                entityChange.PropertyChanges.Add (new EntityPropertyChangeInfo {
                                    NewValue = JsonSerializer.Serialize (propertyEntry.CurrentValue),
                                        OriginalValue = JsonSerializer.Serialize (propertyEntry.OriginalValue),
                                        PropertyName = property.Name,
                                        PropertyTypeFullName = property.ClrType.GetFirstGenericArgumentIfNullable ().FullName
                                });
                            }

                            continue;
                        }

                        if (propertyChange.OriginalValue == propertyChange.NewValue) {
                            var newValue = JsonSerializer.Serialize (propertyEntry.CurrentValue);
                            if (newValue == propertyChange.NewValue) {
                                // No change
                                entityChange.PropertyChanges.Remove (propertyChange);
                            } else {
                                // Update foreign key
                                propertyChange.NewValue = newValue.TruncateWithPostfix (EntityPropertyChangeInfo.MaxValueLength);
                            }
                        }
                    }
                }
            }
        }
    }
}