using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aiwins.Rocket.Auditing;
using Aiwins.Rocket.Data;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Domain.Entities;
using Aiwins.Rocket.Domain.Entities.Events;
using Aiwins.Rocket.Domain.Repositories;
using Aiwins.Rocket.EntityFrameworkCore.EntityHistory;
using Aiwins.Rocket.EntityFrameworkCore.Modeling;
using Aiwins.Rocket.EntityFrameworkCore.ValueConverters;
using Aiwins.Rocket.Guids;
using Aiwins.Rocket.MultiTenancy;
using Aiwins.Rocket.ObjectExtending;
using Aiwins.Rocket.Pinyin;
using Aiwins.Rocket.Reflection;
using Aiwins.Rocket.Timing;
using Aiwins.Rocket.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiwins.Rocket.EntityFrameworkCore {
    public abstract class RocketDbContext<TDbContext> : DbContext, IRocketEfCoreDbContext, ITransientDependency
    where TDbContext : DbContext {
        protected virtual Guid? CurrentTenantId => CurrentTenant?.Id;

        protected virtual bool IsMultiTenantFilterEnabled => DataFilter?.IsEnabled<IMultiTenant> () ?? false;

        protected virtual bool IsSoftDeleteFilterEnabled => DataFilter?.IsEnabled<ISoftDelete> () ?? false;

        public ICurrentTenant CurrentTenant { get; set; }

        public IGuidGenerator GuidGenerator { get; set; }

        public IDataFilter DataFilter { get; set; }

        public IEntityChangeEventHelper EntityChangeEventHelper { get; set; }

        public IAuditPropertySetter AuditPropertySetter { get; set; }

        public IEntityHistoryHelper EntityHistoryHelper { get; set; }

        public IAuditingManager AuditingManager { get; set; }

        public IUnitOfWorkManager UnitOfWorkManager { get; set; }

        public IClock Clock { get; set; }

        public ILogger<RocketDbContext<TDbContext>> Logger { get; set; }

        private static readonly MethodInfo ConfigureBasePropertiesMethodInfo
            = typeof (RocketDbContext<TDbContext>)
            .GetMethod (
                nameof (ConfigureBaseProperties),
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private static readonly MethodInfo ConfigureValueConverterMethodInfo
            = typeof (RocketDbContext<TDbContext>)
            .GetMethod (
                nameof (ConfigureValueConverter),
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        private static readonly MethodInfo ConfigureValueGeneratedMethodInfo
            = typeof (RocketDbContext<TDbContext>)
            .GetMethod (
                nameof (ConfigureValueGenerated),
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        protected RocketDbContext (DbContextOptions<TDbContext> options) : base (options) {
            GuidGenerator = SimpleGuidGenerator.Instance;
            EntityChangeEventHelper = NullEntityChangeEventHelper.Instance;
            EntityHistoryHelper = NullEntityHistoryHelper.Instance;
            Logger = NullLogger<RocketDbContext<TDbContext>>.Instance;
        }

        protected override void OnModelCreating (ModelBuilder modelBuilder) {
            base.OnModelCreating (modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes ()) {
                ConfigureBasePropertiesMethodInfo
                    .MakeGenericMethod (entityType.ClrType)
                    .Invoke (this, new object[] { modelBuilder, entityType });

                ConfigureValueConverterMethodInfo
                    .MakeGenericMethod (entityType.ClrType)
                    .Invoke (this, new object[] { modelBuilder, entityType });

                ConfigureValueGeneratedMethodInfo
                    .MakeGenericMethod (entityType.ClrType)
                    .Invoke (this, new object[] { modelBuilder, entityType });
            }
        }

        public override async Task<int> SaveChangesAsync (bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
            try {
                var auditLog = AuditingManager?.Current?.Log;

                List<EntityChangeInfo> entityChangeList = null;
                if (auditLog != null) {
                    entityChangeList = EntityHistoryHelper.CreateChangeList (ChangeTracker.Entries ().ToList ());
                }

                var changeReport = ApplyRocketConcepts ();

                var result = await base.SaveChangesAsync (acceptAllChangesOnSuccess, cancellationToken);

                await EntityChangeEventHelper.TriggerEventsAsync (changeReport);

                if (auditLog != null) {
                    EntityHistoryHelper.UpdateChangeList (entityChangeList);
                    auditLog.EntityChanges.AddRange (entityChangeList);
                    Logger.LogDebug ($"Added {entityChangeList.Count} entity changes to the current audit log");
                }

                return result;
            } catch (DbUpdateConcurrencyException ex) {
                throw new RocketDbConcurrencyException (ex.Message, ex);
            } finally {
                ChangeTracker.AutoDetectChangesEnabled = true;
            }
        }

        public virtual void Initialize (RocketEfCoreDbContextInitializationContext initializationContext) {
            if (initializationContext.UnitOfWork.Options.Timeout.HasValue && Database.IsRelational () && !Database.GetCommandTimeout ().HasValue) {
                Database.SetCommandTimeout (initializationContext.UnitOfWork.Options.Timeout.Value.TotalSeconds.To<int> ());
            }

            ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            // ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;

            ChangeTracker.Tracked += ChangeTracker_Tracked;
        }

        protected virtual void ChangeTracker_Tracked (object sender, EntityTrackedEventArgs e) {
            FillExtraPropertiesForTrackedEntities (e);
        }

        protected virtual void FillExtraPropertiesForTrackedEntities (EntityTrackedEventArgs e) {
            var entityType = e.Entry.Metadata.ClrType;
            if (entityType == null) {
                return;
            }

            if (!(e.Entry.Entity is IHasExtraProperties entity)) {
                return;
            }

            if (!e.FromQuery) {
                return;
            }

            var objectExtension = ObjectExtensionManager.Instance.GetOrNull (entityType);
            if (objectExtension == null) {
                return;
            }

            foreach (var property in objectExtension.GetProperties ()) {
                if (!property.IsMappedToFieldForEfCore ()) {
                    continue;
                }
                /* 检查“currentValue != null“将会非常有用:
                 * 假设实体拥有额外的信息,
                 * 并创建了一个字段用于存储额外的信息
                 * 实体更新的时候不会删除Json中的旧值
                 */

                var currentValue = e.Entry.CurrentValues[property.Name];
                if (currentValue != null) {
                    entity.ExtraProperties[property.Name] = currentValue;
                }
            }
        }

        protected virtual EntityChangeReport ApplyRocketConcepts () {
            var changeReport = new EntityChangeReport ();

            foreach (var entry in ChangeTracker.Entries ().ToList ()) {
                ApplyRocketConcepts (entry, changeReport);
            }

            return changeReport;
        }

        protected virtual void ApplyRocketConcepts (EntityEntry entry, EntityChangeReport changeReport) {
            switch (entry.State) {
                case EntityState.Added:
                    ApplyRocketConceptsForAddedEntity (entry, changeReport);
                    break;
                case EntityState.Modified:
                    ApplyRocketConceptsForModifiedEntity (entry, changeReport);
                    break;
                case EntityState.Deleted:
                    ApplyRocketConceptsForDeletedEntity (entry, changeReport);
                    break;
            }

            HandleExtraPropertiesOnSave (entry);

            AddDomainEvents (changeReport, entry.Entity);
        }

        protected virtual void HandleExtraPropertiesOnSave (EntityEntry entry) {
            if (entry.State.IsIn (EntityState.Deleted, EntityState.Unchanged)) {
                return;
            }

            var entityType = entry.Metadata.ClrType;
            if (entityType == null) {
                return;
            }

            if (!(entry.Entity is IHasExtraProperties entity)) {
                return;
            }

            var objectExtension = ObjectExtensionManager.Instance.GetOrNull (entityType);
            if (objectExtension == null) {
                return;
            }

            foreach (var property in objectExtension.GetProperties ()) {
                if (!entity.HasProperty (property.Name)) {
                    continue;
                }

                entry.Property (property.Name).CurrentValue = entity.GetProperty (property.Name);
            }
        }

        protected virtual void ApplyRocketConceptsForAddedEntity (EntityEntry entry, EntityChangeReport changeReport) {
            CheckAndSetId (entry);
            SetNewPySpelling (entry);
            SetConcurrencyStampIfNull (entry);
            SetCreationAuditProperties (entry);
            changeReport.ChangedEntities.Add (new EntityChangeEntry (entry.Entity, EntityChangeType.Created));
        }

        protected virtual void ApplyRocketConceptsForModifiedEntity (EntityEntry entry, EntityChangeReport changeReport) {
            UpdatePySpelling (entry);
            UpdateConcurrencyStamp (entry);
            SetModificationAuditProperties (entry);

            if (entry.Entity is ISoftDelete && entry.Entity.As<ISoftDelete> ().IsDeleted) {
                SetDeletionAuditProperties (entry);
                changeReport.ChangedEntities.Add (new EntityChangeEntry (entry.Entity, EntityChangeType.Deleted));
            } else {
                changeReport.ChangedEntities.Add (new EntityChangeEntry (entry.Entity, EntityChangeType.Updated));
            }
        }

        protected virtual void ApplyRocketConceptsForDeletedEntity (EntityEntry entry, EntityChangeReport changeReport) {
            if (TryCancelDeletionForSoftDelete (entry)) {
                UpdateConcurrencyStamp (entry);
                SetDeletionAuditProperties (entry);
            }

            changeReport.ChangedEntities.Add (new EntityChangeEntry (entry.Entity, EntityChangeType.Deleted));
        }

        protected virtual bool IsHardDeleted (EntityEntry entry) {
            var hardDeletedEntities = UnitOfWorkManager?.Current?.Items.GetOrDefault (UnitOfWorkItemNames.HardDeletedEntities) as HashSet<IEntity>;
            if (hardDeletedEntities == null) {
                return false;
            }

            return hardDeletedEntities.Contains (entry.Entity);
        }

        protected virtual void AddDomainEvents (EntityChangeReport changeReport, object entityAsObj) {
            var generatesDomainEventsEntity = entityAsObj as IGeneratesDomainEvents;
            if (generatesDomainEventsEntity == null) {
                return;
            }

            var localEvents = generatesDomainEventsEntity.GetLocalEvents ()?.ToArray ();
            if (localEvents != null && localEvents.Any ()) {
                changeReport.DomainEvents.AddRange (localEvents.Select (eventData => new DomainEventEntry (entityAsObj, eventData)));
                generatesDomainEventsEntity.ClearLocalEvents ();
            }

            var distributedEvents = generatesDomainEventsEntity.GetDistributedEvents ()?.ToArray ();
            if (distributedEvents != null && distributedEvents.Any ()) {
                changeReport.DistributedEvents.AddRange (distributedEvents.Select (eventData => new DomainEventEntry (entityAsObj, eventData)));
                generatesDomainEventsEntity.ClearDistributedEvents ();
            }
        }

        protected virtual void UpdatePySpelling (EntityEntry entry) {
            var entity = entry.Entity as IPySpelling;
            if (entity == null) {
                return;
            }
            // 当更新实体对象Name属性的时候更新拼音简写属性，检查Name属性值是否发生了更新
            if (Entry (entity).Property (x => x.Name).CurrentValue != Entry (entity).Property (x => x.Name).OriginalValue) {
                entity.FullPySpelling = entity.Name.IsNullOrEmpty () ? string.Empty : entity.Name.FullPySpelling ();
                entity.FirstPySpelling = entity.Name.IsNullOrEmpty () ? string.Empty : entity.Name.FirstPySpelling ();
            }
        }

        protected virtual void SetNewPySpelling (EntityEntry entry) {
            var entity = entry.Entity as IPySpelling;
            if (entity == null) {
                return;
            }

            // 当创建实现了IPySpelling实体对象对拼音简写属性赋值
            entity.FullPySpelling = entity.Name.IsNullOrEmpty () ? string.Empty : entity.Name.FullPySpelling ();
            entity.FirstPySpelling = entity.Name.IsNullOrEmpty () ? string.Empty : entity.Name.FirstPySpelling ();
        }

        protected virtual void UpdateConcurrencyStamp (EntityEntry entry) {
            var entity = entry.Entity as IHasConcurrencyStamp;
            if (entity == null) {
                return;
            }

            Entry (entity).Property (x => x.ConcurrencyStamp).OriginalValue = entity.ConcurrencyStamp;
            entity.ConcurrencyStamp = Guid.NewGuid ().ToString ("N");
        }

        protected virtual void SetConcurrencyStampIfNull (EntityEntry entry) {
            var entity = entry.Entity as IHasConcurrencyStamp;
            if (entity == null) {
                return;
            }

            if (entity.ConcurrencyStamp != null) {
                return;
            }

            entity.ConcurrencyStamp = Guid.NewGuid ().ToString ("N");
        }

        protected virtual bool TryCancelDeletionForSoftDelete (EntityEntry entry) {
            if (!(entry.Entity is ISoftDelete)) {
                return false;
            }

            if (IsHardDeleted (entry)) {
                return false;
            }

            entry.Reload ();
            entry.State = EntityState.Modified;
            entry.Entity.As<ISoftDelete> ().IsDeleted = true;
            return true;
        }

        protected virtual void CheckAndSetId (EntityEntry entry) {
            if (entry.Entity is IEntity<Guid> entityWithGuidId) {
                TrySetGuidId (entry, entityWithGuidId);
            }
        }

        protected virtual void TrySetGuidId (EntityEntry entry, IEntity<Guid> entity) {
            if (entity.Id != default) {
                return;
            }

            var idProperty = entry.Property ("Id").Metadata.PropertyInfo;

            // 检查实体是否应用自动生成数据库标识特性
            var dbGeneratedAttr = ReflectionHelper
                .GetSingleAttributeOrDefault<DatabaseGeneratedAttribute> (
                    idProperty
                );

            if (dbGeneratedAttr != null && dbGeneratedAttr.DatabaseGeneratedOption != DatabaseGeneratedOption.None) {
                return;
            }

            EntityHelper.TrySetId (
                entity,
                () => GuidGenerator.Create (),
                true
            );
        }

        protected virtual void SetCreationAuditProperties (EntityEntry entry) {
            AuditPropertySetter?.SetCreationProperties (entry.Entity);
        }

        protected virtual void SetModificationAuditProperties (EntityEntry entry) {
            AuditPropertySetter?.SetModificationProperties (entry.Entity);
        }

        protected virtual void SetDeletionAuditProperties (EntityEntry entry) {
            AuditPropertySetter?.SetDeletionProperties (entry.Entity);
        }

        protected virtual void ConfigureBaseProperties<TEntity> (ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class {
            if (mutableEntityType.IsOwned ()) {
                return;
            }

            modelBuilder.Entity<TEntity> ().ConfigureByConvention ();

            ConfigureGlobalFilters<TEntity> (modelBuilder, mutableEntityType);
        }

        protected virtual void ConfigureGlobalFilters<TEntity> (ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class {
            if (mutableEntityType.BaseType == null && ShouldFilterEntity<TEntity> (mutableEntityType)) {
                var filterExpression = CreateFilterExpression<TEntity> ();
                if (filterExpression != null) {
                    modelBuilder.Entity<TEntity> ().HasQueryFilter (filterExpression);
                }
            }
        }

        protected virtual void ConfigureValueConverter<TEntity> (ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class {
            if (mutableEntityType.BaseType == null &&
                !typeof (TEntity).IsDefined (typeof (DisableDateTimeNormalizationAttribute), true) &&
                !typeof (TEntity).IsDefined (typeof (OwnedAttribute), true) &&
                !mutableEntityType.IsOwned ()) {
                if (Clock == null || !Clock.SupportsMultipleTimezone) {
                    return;
                }

                var dateTimeValueConverter = new RocketDateTimeValueConverter (Clock);

                var dateTimePropertyInfos = typeof (TEntity).GetProperties ()
                    .Where (property =>
                        (property.PropertyType == typeof (DateTime) ||
                            property.PropertyType == typeof (DateTime?)) &&
                        property.CanWrite &&
                        !property.IsDefined (typeof (DisableDateTimeNormalizationAttribute), true)
                    ).ToList ();

                dateTimePropertyInfos.ForEach (property => {
                    modelBuilder
                        .Entity<TEntity> ()
                        .Property (property.Name)
                        .HasConversion (dateTimeValueConverter);
                });
            }
        }

        protected virtual void ConfigureValueGenerated<TEntity> (ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class {
            if (!typeof (IEntity<Guid>).IsAssignableFrom (typeof (TEntity))) {
                return;
            }

            var idPropertyBuilder = modelBuilder.Entity<TEntity> ().Property (x => ((IEntity<Guid>) x).Id);
            if (idPropertyBuilder.Metadata.PropertyInfo.IsDefined (typeof (DatabaseGeneratedAttribute), true)) {
                return;
            }

            idPropertyBuilder.ValueGeneratedNever ();
        }

        protected virtual bool ShouldFilterEntity<TEntity> (IMutableEntityType entityType) where TEntity : class {
            if (typeof (IMultiTenant).IsAssignableFrom (typeof (TEntity))) {
                return true;
            }

            if (typeof (ISoftDelete).IsAssignableFrom (typeof (TEntity))) {
                return true;
            }

            return false;
        }

        protected virtual Expression<Func<TEntity, bool>> CreateFilterExpression<TEntity> ()
        where TEntity : class {
            Expression<Func<TEntity, bool>> expression = null;

            if (typeof (ISoftDelete).IsAssignableFrom (typeof (TEntity))) {
                expression = e => !IsSoftDeleteFilterEnabled || !EF.Property<bool> (e, "IsDeleted");
            }

            if (typeof (IMultiTenant).IsAssignableFrom (typeof (TEntity))) {
                Expression<Func<TEntity, bool>> multiTenantFilter = e => !IsMultiTenantFilterEnabled || EF.Property<Guid> (e, "TenantId") == CurrentTenantId;
                expression = expression == null ? multiTenantFilter : CombineExpressions (expression, multiTenantFilter);
            }

            return expression;
        }

        protected virtual Expression<Func<T, bool>> CombineExpressions<T> (Expression<Func<T, bool>> expression1, Expression<Func<T, bool>> expression2) {
            var parameter = Expression.Parameter (typeof (T));

            var leftVisitor = new ReplaceExpressionVisitor (expression1.Parameters[0], parameter);
            var left = leftVisitor.Visit (expression1.Body);

            var rightVisitor = new ReplaceExpressionVisitor (expression2.Parameters[0], parameter);
            var right = rightVisitor.Visit (expression2.Body);

            return Expression.Lambda<Func<T, bool>> (Expression.AndAlso (left, right), parameter);
        }

        class ReplaceExpressionVisitor : ExpressionVisitor {
            private readonly Expression _oldValue;
            private readonly Expression _newValue;

            public ReplaceExpressionVisitor (Expression oldValue, Expression newValue) {
                _oldValue = oldValue;
                _newValue = newValue;
            }

            public override Expression Visit (Expression node) {
                if (node == _oldValue) {
                    return _newValue;
                }

                return base.Visit (node);
            }
        }
    }
}