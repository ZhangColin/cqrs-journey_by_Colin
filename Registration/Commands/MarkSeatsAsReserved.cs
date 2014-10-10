using System;
using System.Collections.Generic;
using Infrastructure.Messaging;
using Registration.Contracts;

namespace Registration.Commands {
    public class MarkSeatsAsReserved: ICommand {
        public Guid Id { get; private set; }

        public MarkSeatsAsReserved() {
            Id = Guid.NewGuid();
            Seats = new List<SeatQuantity>();
        }

        public List<SeatQuantity> Seats { get; set; }

        public Guid OrderId { get; set; }

        public DateTime Expiration { get; set; }
    }
}