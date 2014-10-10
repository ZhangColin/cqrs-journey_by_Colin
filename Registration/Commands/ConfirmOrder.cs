using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class ConfirmOrder: ICommand {
        public Guid Id { get; private set; }

        public ConfirmOrder() {
            Id = Guid.NewGuid();
        }

        public Guid OrderId { get; set; }
    }
}