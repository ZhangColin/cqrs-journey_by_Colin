using System;
using System.ComponentModel.DataAnnotations;
using Infrastructure.Messaging;

namespace Registration.Commands {
    public class AssignRegistrantDetails: ICommand {
        public Guid Id { get; private set; }
        public Guid OrderId { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string FirstName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string LastName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [RegularExpression(@"[\w-]+(\.?[\w-])*\@[\w-]+(\.[\w-]+)+", 
            ErrorMessageResourceType = typeof(Resources), ErrorMessageResourceName = "InvalidEmail")]
        public string Email { get; set; }

        public AssignRegistrantDetails() {
            Id = Guid.NewGuid();
        }
    }
}