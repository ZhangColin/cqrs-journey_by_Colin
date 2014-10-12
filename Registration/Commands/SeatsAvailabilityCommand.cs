using System;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class SeatsAvailabilityCommand: ICommand, IMessageSessionProvider {
        public Guid Id { get; private set; }
        public Guid ConferenceId { get; set; }

        public SeatsAvailabilityCommand() {
            Id=Guid.NewGuid();
        }

        string IMessageSessionProvider.SessionId {
            get { return this.SessionId; }
        }

        protected string SessionId {
            get { return "SeatsAvailability_" + this.ConferenceId; }
        }
    }
}