using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Conference.Contracts;
using Infrastructure.EventSourcing;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Registration.Commands;
using Registration.Contracts;
using Registration.Events;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Registration.Handlers {
    public class ConferenceViewModelGenerator:
        IEventHandler<ConferenceCreated>,
        IEventHandler<ConferenceUpdated>,
        IEventHandler<ConferencePublished>,
        IEventHandler<ConferenceUnpublished>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>,
        IEventHandler<AvailableSeatsChanged>,
        IEventHandler<SeatsReserved>,
        IEventHandler<SeatsReservationCancelled> {

        private readonly Func<ConferenceRegistrationDbContext> _contextFactory;
        private readonly ICommandBus _commandBus;

        public ConferenceViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory, ICommandBus commandBus) {
            this._contextFactory = contextFactory;
            this._commandBus = commandBus;
        }

        public void Handle(ConferenceCreated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Find<ReadModel.Conference>(@event.SourceId);
                if(dto!=null) {
                    Trace.TraceWarning("Ignoring ConferenceCreated event for conference with ID {0} as it was already created.", @event.SourceId);
                }
                else {
                    context.Set<ReadModel.Conference>().Add(new ReadModel.Conference(
                        @event.SourceId,
                        @event.Slug,
                        @event.Name,
                        @event.Description,
                        @event.Location,
                        @event.Tagline,
                        @event.TwitterSearch,
                        @event.StartDate));

                    context.SaveChanges();
                }
            }
        }

        public void Handle(ConferenceUpdated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var confDto = context.Find<ReadModel.Conference>(@event.SourceId);

                if(confDto!=null) {
                    confDto.Code = @event.Slug;
                    confDto.Description = @event.Description;
                    confDto.Location = @event.Location;
                    confDto.Name = @event.Name;
                    confDto.StartDate = @event.StartDate;
                    confDto.Tagline = @event.Tagline;
                    confDto.TwitterSearch = @event.TwitterSearch;

                    context.SaveChanges();
                }
                else {
                    throw new InvalidOperationException(
                        string.Format("Failed to locate Conference read model for updated conference with id {0}.",
                            @event.SourceId));
                }
            }
        }

        public void Handle(ConferencePublished @event) {
            using (var context = this._contextFactory.Invoke()) {
                var confDto = context.Find<ReadModel.Conference>(@event.SourceId);

                if (confDto != null) {
                    confDto.IsPublished = true;

                    context.Save(confDto);
                }
                else {
                    throw new InvalidOperationException(
                        string.Format("Failed to locate Conference read model for published conference with id {0}.",
                            @event.SourceId));
                }
            }
        }

        public void Handle(ConferenceUnpublished @event) {
            using (var context = this._contextFactory.Invoke()) {
                var confDto = context.Find<ReadModel.Conference>(@event.SourceId);

                if (confDto != null) {
                    confDto.IsPublished = false;

                    context.Save(confDto);
                }
                else {
                    throw new InvalidOperationException(
                        string.Format("Failed to locate Conference read model for unpublished conference with id {0}.",
                            @event.SourceId));
                }
            }
        }

        public void Handle(SeatCreated @event) {
            using(var context = this._contextFactory.Invoke()) {
                var dto = context.Find<SeatType>(@event.SourceId);
                if(dto!=null) {
                    Trace.TraceWarning(
                        "Ignoring SeatCreated event for seat type with ID {0} as it was already created.",
                        @event.SourceId);
                }
                else {
                    dto = new SeatType(
                        @event.SourceId,
                        @event.ConferenceId,
                        @event.Name,
                        @event.Description,
                        @event.Price,
                        @event.Quantity);

                    this._commandBus.Send(new AddSeats() {
                        ConferenceId = @event.ConferenceId,
                        SeatType = @event.SourceId,
                        Quantity = @event.Quantity
                    });

                    context.Save(dto);
                }
            }
        }

        public void Handle(SeatUpdated @event) {
            using (var context = this._contextFactory.Invoke()) {
                var dto = context.Find<SeatType>(@event.SourceId);
                if (dto != null) {
                    dto.Description = @event.Description;
                    dto.Name = @event.Name;
                    dto.Price = @event.Price;

                    var diff = @event.Quantity - dto.Quantity;

                    dto.Quantity = @event.Quantity;

                    context.Save(dto);

                    if(diff > 0) {
                        this._commandBus.Send(new AddSeats() {
                            ConferenceId = @event.ConferenceId,
                            SeatType = @event.SourceId,
                            Quantity = diff
                        });
                    }
                    else {
                        this._commandBus.Send(new RemoveSeats() {
                            ConferenceId = @event.ConferenceId,
                            SeatType = @event.SourceId,
                            Quantity = Math.Abs(diff)
                        });
                    }
                }
                else {
                    throw new InvalidOperationException(
                        string.Format("Failed to locate Seat Type read model being updated with id {0}.",
                            @event.SourceId));
                }
            }
        }

        public void Handle(AvailableSeatsChanged @event) {
            this.UpdateAvailableQuantity(@event, @event.Seats);
        }

        public void Handle(SeatsReserved @event) {
            this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
        }

        public void Handle(SeatsReservationCancelled @event) {
            this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
        }

        private void UpdateAvailableQuantity(IVersionedEvent @event, IEnumerable<SeatQuantity> seats) {
            using (var context = this._contextFactory.Invoke()) {
                var seatDtos = context.Set<SeatType>().Where(x => x.ConferenceId == @event.SourceId).ToList();

                if(seatDtos.Count>0) {
                    var maxSeatsAvailabilityVersion = seatDtos.Max(x => x.SeatsAvailabilityVersion);

                    if(maxSeatsAvailabilityVersion>=@event.Version) {
                        Trace.TraceWarning(
                            "Ignoring availability update message with version {1} for seat types with conference id {0}, last known version {2}",
                            @event.SourceId, @event.Version, maxSeatsAvailabilityVersion);

                        return;
                    }

                    foreach(var seat in seats) {
                        var seatDto = seatDtos.FirstOrDefault(x => x.Id == seat.SeatType);

                        if(seatDto!=null) {
                            seatDto.AvailableQuantity += seat.Quantity;
                            seatDto.SeatsAvailabilityVersion = @event.Version;
                        }
                        else {
                            Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.", seat.SeatType);
                        }
                    }

                    context.SaveChanges();
                }
                else {
                    Trace.TraceError(
                        "Failed to locate Seat Types read model for updated seat availability, with conference id {0}.",
                        @event.SourceId);
                }
            }
        }
    }
}