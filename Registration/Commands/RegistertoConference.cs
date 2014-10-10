using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using Infrastructure.Messaging;
using Registration.Contracts;

namespace Registration.Commands {
    public class RegisterToConference: ICommand, IValidatableObject {
        public RegisterToConference() {
            Id = Guid.NewGuid();
            Seats = new List<SeatQuantity>();
        }
        public Guid Id { get; private set; }
        public Guid OrderId { get; set; }
        public Guid ConferenceId { get; set; }
        public ICollection<SeatQuantity> Seats { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
            if(this.Seats==null || !this.Seats.Any(x=>x.Quantity>0)) {
                return new[] {new ValidationResult("One or more items are required.", new[] {"Seats"})};
            }
            else if(this.Seats.Any(x=>x.Quantity<0)) {
                return new[] {new ValidationResult("Invalid registration..", new[] {"Seats"})};
            }

            return Enumerable.Empty<ValidationResult>();
        }
    }
}