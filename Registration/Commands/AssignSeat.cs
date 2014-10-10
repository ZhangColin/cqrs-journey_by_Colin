using System;
using Infrastructure.Messaging;
using Registration.Contracts;

namespace Registration.Commands {
    public class AssignSeat: ICommand {
        public Guid Id { get; private set; }

        public AssignSeat() {
            this.Id = Guid.NewGuid();
        }

        public Guid SeatAssignmentsId { get; set; }
        public int Position { get; set; }
        public PersonalInfo Attendee { get; set; }
    }
}