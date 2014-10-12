using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class ExpireRegistrationProcess: ICommand {
        public Guid Id { get; private set; }

        public ExpireRegistrationProcess() {
            Id=Guid.NewGuid();
        }

        public Guid ProcessId { get; set; }
    }
}