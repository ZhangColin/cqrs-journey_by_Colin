using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class UnassignSeat: ICommand {
        public UnassignSeat() {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public Guid SeatAssignmentsId { get; set; }
        public int Position { get; set; }
    }
}