using System;
using System.Collections.Generic;
using System.Linq;
using Conference.Common;
using Infrastructure.EventSourcing;
using Registration.Contracts;
using Registration.Events;

namespace Registration {
    public class SeatsAvailability: EventSourced, IMementoOriginator {
        private readonly Dictionary<Guid, int> _remainingSeats = new Dictionary<Guid, int>();
        private readonly Dictionary<Guid, List<SeatQuantity>> _pendingReservations =
            new Dictionary<Guid, List<SeatQuantity>>();

        public SeatsAvailability(Guid id): base(id) {
            this.Handles<AvailableSeatsChanged>(this.OnAvailableSeatsChanged);
            this.Handles<SeatsReserved>(this.OnSeatsReserved);
            this.Handles<SeatsReservationCommitted>(this.OnSeatsReservationCommitted);
            this.Handles<SeatsReservationCancelled>(this.OnSeatsReservationCancelled);
        }

        public SeatsAvailability(Guid id, IEnumerable<IVersionedEvent> history): this(id) {
            this.LoadFrom(history);
        }

        public SeatsAvailability(Guid id, IMemento memento, IEnumerable<IVersionedEvent> history): this(id) {
            Memento state = (Memento)memento;
            this.Version = state.Version;

            this._remainingSeats.AddRange(state.RemainingSeats);
            this._pendingReservations.AddRange(state.PendingReservations);

            this.LoadFrom(history);
        }

        public void AddSeats(Guid seatType, int quantity) {
            this.Update(new AvailableSeatsChanged(){Seats = new []{new SeatQuantity(seatType, quantity) }});
        }
        
        public void RemoveSeats(Guid seatType, int quantity) {
            this.Update(new AvailableSeatsChanged(){Seats = new []{new SeatQuantity(seatType, -quantity) }});
        }

        public void MakeReservation(Guid reservationId, IEnumerable<SeatQuantity> wantedSeats) {
            var wantedList = wantedSeats.ToList();
            if(wantedList.Any(x=>!this._remainingSeats.ContainsKey(x.SeatType))) {
                throw new ArgumentOutOfRangeException("wantedSeats");
            }

            var difference = new Dictionary<Guid, SeatDifference>();
            foreach(var seatQuantity in wantedList) {
                var item = GetOrAdd(difference, seatQuantity.SeatType);
                item.Wanted = seatQuantity.Quantity;
                item.Remaining = this._remainingSeats[seatQuantity.SeatType];
            }

            List<SeatQuantity> existing;
            if(this._pendingReservations.TryGetValue(reservationId, out  existing)) {
                foreach(var seatQuantity in existing) {
                    GetOrAdd(difference, seatQuantity.SeatType).Existing = seatQuantity.Quantity;
                }
            }

            var reservation = new SeatsReserved() {
                ReservationId = reservationId,
                ReservationDetails =
                    difference.Select(x => new SeatQuantity(x.Key, x.Value.Actual)).Where(x => x.Quantity != 0).ToList(),
                AvailableSeatsChanged =
                    difference.Select(x => new SeatQuantity(x.Key, -x.Value.DeltaSinceLast)).Where(x => x.Quantity != 0)
                        .ToList()
            };
            this.Update(reservation);
        }

        public void CancelReservation(Guid reservationId) {
            List<SeatQuantity> reservation;
            if(this._pendingReservations.TryGetValue(reservationId, out reservation)) {
                this.Update(new SeatsReservationCancelled() {
                    ReservationId = reservationId,
                    AvailableSeatsChanged = reservation.Select(x=>new SeatQuantity(x.SeatType, x.Quantity)).ToList()
                });
            }
        }

        public void CommitReservation(Guid reservationId) {
            if(this._pendingReservations.ContainsKey(reservationId)) {
                this.Update(new SeatsReservationCommitted(){ReservationId = reservationId});
            }
        }

        private class SeatDifference {
            public int Wanted { get; set; } 
            public int Existing { get; set; } 
            public int Remaining { get; set; }

            public int Actual {
                get { return Math.Min(this.Wanted, Math.Max(this.Remaining, 0) + this.Existing); }
            }

            public int DeltaSinceLast {
                get { return this.Actual - this.Existing; }
            }
        }

        private static TValue GetOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TValue: new() {
            TValue value;

            if(!dictionary.TryGetValue(key, out value)) {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }

        private void OnAvailableSeatsChanged(AvailableSeatsChanged e) {
            foreach(var seat in e.Seats) {
                int newValue = seat.Quantity;
                int remaining;
                if(this._remainingSeats.TryGetValue(seat.SeatType, out remaining)) {
                    newValue += remaining;
                }

                this._remainingSeats[seat.SeatType] = newValue;
            }
        }

        private void OnSeatsReserved(SeatsReserved e) {
            var details = e.ReservationDetails.ToList();
            if(details.Count>0) {
                this._pendingReservations[e.ReservationId] = details;
            }
            else {
                this._pendingReservations.Remove(e.ReservationId);
            }

            foreach(var seat in e.AvailableSeatsChanged) {
                this._remainingSeats[seat.SeatType] = this._remainingSeats[seat.SeatType] + seat.Quantity;
            }
        }

        private void OnSeatsReservationCommitted(SeatsReservationCommitted e) {
            this._pendingReservations.Remove(e.ReservationId);
        }

        private void OnSeatsReservationCancelled(SeatsReservationCancelled e) {
            this._pendingReservations.Remove(e.ReservationId);

            foreach(var seat in e.AvailableSeatsChanged) {
                this._remainingSeats[seat.SeatType] = this._remainingSeats[seat.SeatType] + seat.Quantity;
            }
        }

        public IMemento SaveToMemento() {
            return new Memento() {
                Version = this.Version,
                RemainingSeats = this._remainingSeats.ToArray(),
                PendingReservations = this._pendingReservations.ToArray()
            };
        }

        internal class Memento: IMemento {
            public int Version { get; internal set; }
            internal KeyValuePair<Guid, int>[] RemainingSeats { get; set; } 
            internal KeyValuePair<Guid, List<SeatQuantity>>[] PendingReservations { get; set; } 
        }
    }
}