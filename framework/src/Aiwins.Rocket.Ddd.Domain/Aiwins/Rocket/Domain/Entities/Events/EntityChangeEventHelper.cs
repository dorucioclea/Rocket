using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aiwins.Rocket.Auditing;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Domain.Entities.Events.Distributed;
using Aiwins.Rocket.DynamicProxy;
using Aiwins.Rocket.EventBus;
using Aiwins.Rocket.EventBus.Distributed;
using Aiwins.Rocket.EventBus.Local;
using Aiwins.Rocket.Uow;

namespace Aiwins.Rocket.Domain.Entities.Events {
    /// <summary>
    /// Used to trigger entity change events.
    /// </summary>
    public class EntityChangeEventHelper : IEntityChangeEventHelper, ITransientDependency {
        public ILocalEventBus LocalEventBus { get; set; }
        public IDistributedEventBus DistributedEventBus { get; set; }

        protected IUnitOfWorkManager UnitOfWorkManager { get; }
        protected IEntityToEtoMapper EntityToEtoMapper { get; }

        public EntityChangeEventHelper (
            IUnitOfWorkManager unitOfWorkManager,
            IEntityToEtoMapper entityToEtoMapper) {
            UnitOfWorkManager = unitOfWorkManager;
            EntityToEtoMapper = entityToEtoMapper;

            LocalEventBus = NullLocalEventBus.Instance;
            DistributedEventBus = NullDistributedEventBus.Instance;
        }

        public async Task TriggerEventsAsync (EntityChangeReport changeReport) {
            await TriggerEventsInternalAsync (changeReport);

            if (changeReport.IsEmpty () || UnitOfWorkManager.Current == null) {
                return;
            }

            await UnitOfWorkManager.Current.SaveChangesAsync ();
        }

        public virtual async Task TriggerEntityCreatingEventAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityCreatingEventData<>),
                entity,
                true
            );
        }

        public virtual async Task TriggerEntityCreatedEventOnUowCompletedAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityCreatedEventData<>),
                entity,
                false
            );

            var eto = EntityToEtoMapper.Map (entity);
            if (eto != null) {
                await TriggerEventWithEntity (
                    DistributedEventBus,
                    typeof (EntityCreatedEto<>),
                    eto,
                    false
                );
            }
        }

        public virtual async Task TriggerEntityUpdatingEventAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityUpdatingEventData<>),
                entity,
                true
            );
        }

        public virtual async Task TriggerEntityUpdatedEventOnUowCompletedAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityUpdatedEventData<>),
                entity,
                false
            );

            var eto = EntityToEtoMapper.Map (entity);
            if (eto != null) {
                await TriggerEventWithEntity (
                    DistributedEventBus,
                    typeof (EntityUpdatedEto<>),
                    eto,
                    false
                );
            }
        }

        public virtual async Task TriggerEntityDeletingEventAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityDeletingEventData<>),
                entity,
                true
            );
        }

        public virtual async Task TriggerEntityDeletedEventOnUowCompletedAsync (object entity) {
            await TriggerEventWithEntity (
                LocalEventBus,
                typeof (EntityDeletedEventData<>),
                entity,
                false
            );

            var eto = EntityToEtoMapper.Map (entity);
            if (eto != null) {
                await TriggerEventWithEntity (
                    DistributedEventBus,
                    typeof (EntityDeletedEto<>),
                    EntityToEtoMapper.Map (entity),
                    false
                );
            }
        }

        protected virtual async Task TriggerEventsInternalAsync (EntityChangeReport changeReport) {
            await TriggerEntityChangeEvents (changeReport.ChangedEntities);
            await TriggerLocalEvents (changeReport.DomainEvents);
            await TriggerDistributedEvents (changeReport.DistributedEvents);
        }

        protected virtual async Task TriggerEntityChangeEvents (List<EntityChangeEntry> changedEntities) {
            foreach (var changedEntity in changedEntities) {
                switch (changedEntity.ChangeType) {
                    case EntityChangeType.Created:
                        await TriggerEntityCreatingEventAsync (changedEntity.Entity);
                        await TriggerEntityCreatedEventOnUowCompletedAsync (changedEntity.Entity);
                        break;
                    case EntityChangeType.Updated:
                        await TriggerEntityUpdatingEventAsync (changedEntity.Entity);
                        await TriggerEntityUpdatedEventOnUowCompletedAsync (changedEntity.Entity);
                        break;
                    case EntityChangeType.Deleted:
                        await TriggerEntityDeletingEventAsync (changedEntity.Entity);
                        await TriggerEntityDeletedEventOnUowCompletedAsync (changedEntity.Entity);
                        break;
                    default:
                        throw new RocketException ("Unknown EntityChangeType: " + changedEntity.ChangeType);
                }
            }
        }

        protected virtual async Task TriggerLocalEvents (List<DomainEventEntry> localEvents) {
            foreach (var localEvent in localEvents) {
                await LocalEventBus.PublishAsync (localEvent.EventData.GetType (), localEvent.EventData);
            }
        }

        protected virtual async Task TriggerDistributedEvents (List<DomainEventEntry> distributedEvents) {
            foreach (var distributedEvent in distributedEvents) {
                await DistributedEventBus.PublishAsync (distributedEvent.EventData.GetType (), distributedEvent.EventData);
            }
        }

        protected virtual async Task TriggerEventWithEntity (IEventBus eventPublisher, Type genericEventType, object entity, bool triggerInCurrentUnitOfWork) {
            var entityType = ProxyHelper.UnProxy (entity).GetType ();
            var eventType = genericEventType.MakeGenericType (entityType);

            if (triggerInCurrentUnitOfWork || UnitOfWorkManager.Current == null) {
                await eventPublisher.PublishAsync (eventType, Activator.CreateInstance (eventType, entity));
                return;
            }

            UnitOfWorkManager.Current.OnCompleted (() => eventPublisher.PublishAsync (eventType, Activator.CreateInstance (eventType, entity)));
        }
    }
}