using System;

namespace Infrastructure.Sql.Processes {
    public class UndispatchedMessages {
        public Guid Id { get; set; }
        public string Commands { get; set; }

        protected UndispatchedMessages() {
        }

        public UndispatchedMessages(Guid id) {
            this.Id = id;
        }
    }
}