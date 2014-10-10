using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using Conference.Contracts;
using Infrastructure.Messaging.Handling;
using Registration.Contracts;
using Registration.Contracts.Events;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.Handlers {
    public class PricedOrderViewModelGenerator:
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderTotalsCalculated>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderExpired>,
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated> {
        private readonly Func<ConferenceRegistrationDbContext> _contextFactory;
        private readonly ObjectCache _seatDescriptionsCache;

        public PricedOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory) {
            this._contextFactory = contextFactory;
            this._seatDescriptionsCache = MemoryCache.Default;
        }

        public void Handle(OrderPlaced @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = new PricedOrder {
                    OrderId = @event.SourceId,
                    ReservationExpirationDate = @event.ReservationAutoExpiration,
                    OrderVersion = @event.Version
                };
                context.Set<PricedOrder>().Add(dto);

                try {
                    context.SaveChanges();
                }
                catch(DbUpdateException) {
                    Trace.TraceWarning(
                        "Ignoring OrderPlaced message with version {1} for order id {0}. This could be caused because the message was already handled and the PricedOrder entity was already created. ",
                        dto.OrderId, @event.Version);
                }
            }
        }

        public void Handle(OrderTotalsCalculated @event) {
            var seatTypeIds = @event.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).Distinct().ToArray();
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Query<PricedOrder>().Include(x => x.Lines).First(x => x.OrderId == @event.SourceId);

                if(!WasNotAlreadyHandled(dto, @event.Version)) {
                    return;
                }

                var linesSet = context.Set<PricedOrderLine>();
                foreach(var line in dto.Lines.ToList()) {
                    linesSet.Remove(line);
                }

                List<PricedOrderLineSeatTypeDescription> seatTypeDescriptions = GetSeatTypeDescriptions(seatTypeIds, context);

                for(int i = 0; i < @event.Lines.Length; i++) {
                    var orderLine = @event.Lines[i];
                    var line = new PricedOrderLine() {
                        LineTotal = orderLine.LineTotal,
                        Position = i
                    };

                    var seatOrderLine = orderLine as SeatOrderLine;
                    if(seatOrderLine!=null) {
                        line.Description = seatTypeDescriptions.Where(x=>x.SeatTypeId==seatOrderLine.SeatType)
                            .Select(x=>x.Name).FirstOrDefault();
                        line.UnitPrice = seatOrderLine.UnitPrice;
                        line.Quantity = seatOrderLine.Quantity;
                    }

                    dto.Lines.Add(line);
                }

                dto.Total = @event.Total;
                dto.IsFreeOfCharge = @event.IsFreeOfCharge;
                dto.OrderVersion = @event.Version;

                context.SaveChanges();
            }
        }

        public void Handle(OrderConfirmed @event) {
            using (var context = this._contextFactory.Invoke()) {
                var dto = context.Find<PricedOrder>(@event.SourceId);
                if(this.WasNotAlreadyHandled(dto, @event.Version)) {
                    dto.ReservationExpirationDate = null;
                    dto.OrderVersion = @event.Version;
                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderExpired @event) {
            using (var context = this._contextFactory.Invoke()) {
                var pricedOrder = new PricedOrder() {OrderId = @event.SourceId};
                var set = context.Set<PricedOrder>();
                set.Attach(pricedOrder);
                set.Remove(pricedOrder);

                try {
                    context.SaveChanges();
                }
                catch(DbUpdateConcurrencyException) {
                    Trace.TraceWarning(
                        "Ignoring priced order expiration message with version {1} for order id {0}. This could be caused because the message was already handled and the entity was already deleted.",
                        pricedOrder.OrderId,
                        @event.Version);
                }
            }
        }

        public void Handle(SeatAssignmentsCreated @event) {
            using (var context = this._contextFactory.Invoke()) {
                var dto = context.Find<PricedOrder>(@event.OrderId);
                dto.AssignmentsId = @event.SourceId;

                context.SaveChanges();
            }
        }

        public void Handle(SeatCreated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Find<PricedOrderLineSeatTypeDescription>(@event.SourceId);
                if(dto == null) {
                    dto = new PricedOrderLineSeatTypeDescription() {SeatTypeId = @event.SourceId};
                    context.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
                }
                dto.Name = @event.Name;
                context.SaveChanges();
            }
        }

        public void Handle(SeatUpdated @event) {
            using (var context = this._contextFactory.Invoke()) {
                var dto = context.Find<PricedOrderLineSeatTypeDescription>(@event.SourceId);
                if (dto == null) {
                    dto = new PricedOrderLineSeatTypeDescription() { SeatTypeId = @event.SourceId };
                    context.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
                }
                dto.Name = @event.Name;
                context.SaveChanges();

                this._seatDescriptionsCache.Set("SeatDescription_" + dto.SeatTypeId, 
                    dto, new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5) });
            }
        }

        private bool WasNotAlreadyHandled(PricedOrder pricedOrder, int eventVersion) {
            if(eventVersion>pricedOrder.OrderVersion) {
                return true;
            }
            else if(eventVersion==pricedOrder.OrderVersion) {
                Trace.TraceWarning(
                    "Ignoring duplicate priced order update message with version {1} for order id {0}",
                    pricedOrder.OrderId,
                    eventVersion);

                return false;
            }
            else {
                Trace.TraceWarning(
                    @"Ignoring an older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order. Nevertheless, this warning can be expected in a migration scenario.",
                    pricedOrder.OrderId,
                    eventVersion,
                    pricedOrder.OrderVersion);
                return false;
            }
        }

        private List<PricedOrderLineSeatTypeDescription> GetSeatTypeDescriptions(IEnumerable<Guid> seatTypeIds,
            ConferenceRegistrationDbContext context) {
            var result = new List<PricedOrderLineSeatTypeDescription>();
            var notCached = new List<Guid>();

            PricedOrderLineSeatTypeDescription cached;

            foreach(var seatTypeId in seatTypeIds) {
                cached =
                    (PricedOrderLineSeatTypeDescription)this._seatDescriptionsCache.Get("SeatDescription_" + seatTypeId);

                if(cached==null) {
                    notCached.Add(seatTypeId);
                }
                else {
                    result.Add(cached);
                }
            }

            if(notCached.Count>0) {
                var notCachedArray = notCached.ToArray();
                var seatTypeDescriptions = context.Query<PricedOrderLineSeatTypeDescription>()
                    .Where(x => notCachedArray.Contains(x.SeatTypeId)).ToList();

                foreach(var seatType in seatTypeDescriptions) {
                    var desc = (PricedOrderLineSeatTypeDescription)
                        this._seatDescriptionsCache.AddOrGetExisting("SeatDescription_" + seatType.SeatTypeId,
                            seatType, new CacheItemPolicy {AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5)})
                        ?? seatType;

                    result.Add(desc);
                }
            }

            return result;
        }
    }
}