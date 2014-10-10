using System;
using System.IO;
using System.Linq;
using System.Text;
using AutoMapper;
using Conference.Common;
using Infrastructure.BlobStorage;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Registration.Contracts.Events;
using Registration.ReadModel;

namespace Registration.Handlers {
    public class SeatAssignmentsViewModelGenerator:
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatAssigned>,
        IEventHandler<SeatUnassigned>,
        IEventHandler<SeatAssignmentUpdated> {
        private readonly IConferenceDao _conferenceDao;
        private readonly IBlobStorage _storage;
        private readonly ITextSerializer _serializer;

        static SeatAssignmentsViewModelGenerator() {
            Mapper.CreateMap<SeatAssigned, OrderSeat>();
            Mapper.CreateMap<SeatAssignmentUpdated, OrderSeat>();
        }

        public SeatAssignmentsViewModelGenerator(IConferenceDao conferenceDao, IBlobStorage storage, ITextSerializer serializer) {
            this._conferenceDao = conferenceDao;
            this._storage = storage;
            this._serializer = serializer;
        }

        public static string GetSeatAssignmentsBlobId(Guid sourceId) {
            return "SeatAssignments-" + sourceId;
        }

        public void Handle(SeatAssignmentsCreated @event) {
            var seatTypes = this._conferenceDao.GetSeatTypeNames(@event.Seats.Select(x => x.SeatType))
                .ToDictionary(x => x.Id, x => x.Name);

            var dto = new OrderSeats(@event.SourceId, @event.OrderId,
                @event.Seats.Select(i => new OrderSeat(i.Position, seatTypes.TryGetValue(i.SeatType))));

            this.Save(dto);
        }

        public void Handle(SeatAssigned @event) {
            var dto = this.Find(@event.SourceId);
            var seat = dto.Seats.First(x => x.Position == @event.Position);
            Mapper.Map(@event, seat);

            this.Save(dto);
        }

        public void Handle(SeatUnassigned @event) {
            var dto = this.Find(@event.SourceId);
            var seat = dto.Seats.First(x => x.Position == @event.Position);
            seat.Attendee.Email = seat.Attendee.FirstName = seat.Attendee.LastName = null;

            this.Save(dto);
        }

        public void Handle(SeatAssignmentUpdated @event) {
            var dto = this.Find(@event.SourceId);
            var seat = dto.Seats.First(x => x.Position == @event.Position);
            Mapper.Map(@event, seat);

            this.Save(dto);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2202:Do not dispose objects multiple times",
            Justification = "By design")]
        private OrderSeats Find(Guid id) {
            var dto = this._storage.Find(GetSeatAssignmentsBlobId(id));
            if(dto == null) {
                return null;
            }

            using(var stream = new MemoryStream(dto)) {
                using(var reader = new StreamReader(stream)) {
                    return (OrderSeats)this._serializer.Deserialize(reader);
                }
            }
        }

        private void Save(OrderSeats dto) {
            using(var writer = new StringWriter()) {
                this._serializer.Serialize(writer, dto);
                this._storage.Save(GetSeatAssignmentsBlobId(dto.AssignmentsId), "text/plain",
                    Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }
    }
}