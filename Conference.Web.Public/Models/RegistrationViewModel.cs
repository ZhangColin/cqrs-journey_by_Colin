using Registration.Commands;
using Registration.ReadModel;

namespace Conference.Web.Public.Models {
    public class RegistrationViewModel {
        public PricedOrder Order { get; set; }
        public AssignRegistrantDetails RegistrantDetails { get; set; }
    }
}