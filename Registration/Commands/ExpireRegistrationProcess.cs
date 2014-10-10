using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class ExpireRegistrationProcess: ICommand {
        public Guid Id { get; private set; }

        public ExpireRegistrationProcess() {
            Id=new Guid();
        }

        public Guid ProcessId { get; set; }
    }
}