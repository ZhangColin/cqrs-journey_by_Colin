using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class OrderPlaced: VersionedEvent {
        public Guid ConferenceId { get; set; }
        public IEnumerable<SeatQuantity> Seats { get; set; }
        public DateTime ReservationAutoExpiration { get; set; }
        public string AccessCode { get; set; }
    }
}