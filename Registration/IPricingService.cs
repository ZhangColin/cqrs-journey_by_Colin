using System;
using System.Collections.Generic;
using Registration.Contracts;

namespace Registration {
    public interface IPricingService {
        OrderTotal CalculateTotal(Guid conferenceId, ICollection<SeatQuantity> seatItems);
    }
}