using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class RejectOrder: ICommand {
        public Guid Id { get; private set; }
        public Guid OrderId { get; set; }

        public RejectOrder() {
            Id = Guid.NewGuid();
        }
    }
}