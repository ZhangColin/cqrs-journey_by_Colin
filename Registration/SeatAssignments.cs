using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Infrastructure.EventSourcing;
using Infrastructure.Utils;
using Registration.Contracts;
using Registration.Contracts.Events;

namespace Registration {
    public class SeatAssignments: EventSourced {
        private class SeatAssignment {
            public SeatAssignment() {
                Attendee = new PersonalInfo();
            }

            public PersonalInfo Attendee { get; set; }
            public Guid SeatType { get; set; }
            public int Position { get; set; }
        }

        private Dictionary<int, SeatAssignment> _seats = new Dictionary<int, SeatAssignment>();

        static SeatAssignments() {
            Mapper.CreateMap<SeatAssigned, SeatAssignment>();
            Mapper.CreateMap<SeatUnassigned, SeatAssignment>();
            Mapper.CreateMap<SeatAssignmentUpdated, SeatAssignment>();
        }

        public SeatAssignments(Guid orderId, IEnumerable<SeatQuantity> seats): this(GuidUtil.NewSequentialId()) {
            var i = 0;
            var all = new List<SeatAssignmentsCreated.SeatAssignmentInfo>();
            foreach(var seatQuantity in seats) {
                for(int j = 0; j < seatQuantity.Quantity; j++) {
                    all.Add(new SeatAssignmentsCreated.SeatAssignmentInfo(){Position = i++, SeatType = seatQuantity.SeatType});
                }
            }

            base.Update(new SeatAssignmentsCreated(){OrderId = orderId, Seats = all});
        }

        public SeatAssignments(Guid id, IEnumerable<IVersionedEvent> history): this(id) {
            this.LoadFrom(history);
        }

        private SeatAssignments(Guid id): base(id) {
            this.Handles<SeatAssignmentsCreated>(this.OnCreated);
            this.Handles<SeatAssigned>(this.OnSeatAssigned);
            this.Handles<SeatUnassigned>(this.OnSeatUnassigned);
            this.Handles<SeatAssignmentUpdated>(this.OnSeatAssignmentUpdated);
        }

        public void AssignSeat(int position, PersonalInfo attendee) {
            if(string.IsNullOrEmpty(attendee.Email)) {
                throw new ArgumentNullException("attendee.Email");
            }

            SeatAssignment current;
            if (!this._seats.TryGetValue(position, out current)) {
                throw new ArgumentOutOfRangeException("position");
            }

            if(!attendee.Email.Equals(current.Attendee.Email)) {
                if(current.Attendee.Email!=null) {
                    this.Update(new SeatUnassigned(this.Id){Position = position});
                }

                this.Update(new SeatAssigned(this.Id) {
                    Position = position,
                    SeatType = current.SeatType,
                    Attendee = attendee
                });
            }
            else if(!string.Equals(attendee.FirstName, current.Attendee.FirstName) || !string.Equals(attendee.LastName, current.Attendee.LastName)) {
                this.Update(new SeatAssignmentUpdated(this.Id) {
                    Position = position,
                    Attendee = attendee
                });
            }
        }

        public void Unassign(int position) {
            SeatAssignment current;
            if(!this._seats.TryGetValue(position, out current)) {
                throw new ArgumentOutOfRangeException("position");
            }

            if(current.Attendee.Email!=null) {
                this.Update(new SeatUnassigned(this.Id){Position = position});
            }
        }

        private void OnCreated(SeatAssignmentsCreated e) {
            this._seats = e.Seats.ToDictionary(x => x.Position,
                x => new SeatAssignment() {Position = x.Position, SeatType = x.SeatType});
        }

        private void OnSeatAssigned(SeatAssigned e) {
            this._seats[e.Position] = Mapper.Map(e, new SeatAssignment());
        }

        private void OnSeatUnassigned(SeatUnassigned e) {
            this._seats[e.Position] = Mapper.Map(e, new SeatAssignment() {SeatType = this._seats[e.Position].SeatType});
        }

        private void OnSeatAssignmentUpdated(SeatAssignmentUpdated e) {
            this._seats[e.Position] = Mapper.Map(e, new SeatAssignment() {
                SeatType = this._seats[e.Position].SeatType,
                Attendee = new PersonalInfo() {Email = this._seats[e.Position].Attendee.Email}
            });
        }
    }
}