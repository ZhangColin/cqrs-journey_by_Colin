using Registration.Contracts;

namespace Registration.ReadModel {
    public class OrderSeat {
        public int Position { get; set; }
        public string SeatName { get; set; }

        public OrderSeat(int position, string seatName) {
            this.Position = position;
            this.SeatName = seatName;

            this.Attendee = new PersonalInfo();
        }

        public PersonalInfo Attendee { get; set; }
    }
}