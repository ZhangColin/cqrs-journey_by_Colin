using Infrastructure.EventSourcing;

namespace Registration.Contracts.Events {
    public class OrderRegistrantAssigned: VersionedEvent {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
}