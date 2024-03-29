﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Conference.Common.Utils;
using Infrastructure.EventSourcing;
using Registration.Contracts;
using Registration.Contracts.Events;

namespace Registration {
    public class Order: EventSourced {
        private static readonly TimeSpan ReservationAutoExpiration = TimeSpan.FromMinutes(15);
        private List<SeatQuantity> _seats;
        private bool _isConfirmed;
        private Guid _conferenceId;

        static Order() {
            Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
        }

        protected Order(Guid id): base(id) {
            this.Handles<OrderPlaced>(this.OnOrderPlaced);
            this.Handles<OrderUpdated>(this.OnOrderUpdated);
            this.Handles<OrderPartiallyReserved>(this.OnOrderPartiallyReserved);
            this.Handles<OrderReservationCompleted>(this.OnOrderReservationCompleted);
            this.Handles<OrderExpired>(this.OnOrderExpired);
            this.Handles<OrderPaymentConfirmed>(e=>this.OnOrderConfirmed(Mapper.Map<OrderConfirmed>(e)));
            this.Handles<OrderConfirmed>(this.OnOrderConfirmed);
            this.Handles<OrderRegistrantAssigned>(this.OnOrderRegistrantAssigned);
            this.Handles<OrderTotalsCalculated>(this.OnOrderTotalsCalculated);
        }

        public Order(Guid id, IEnumerable<IVersionedEvent> history): this(id) {
            this.LoadFrom(history);
        }

        public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items, IPricingService pricingService): this(id) {
            var all = ConvertItems(items);
            var totals = pricingService.CalculateTotal(conferenceId, all.AsReadOnly());

            this.Update(new OrderPlaced {
                ConferenceId = conferenceId,
                Seats = all,
                ReservationAutoExpiration = DateTime.UtcNow.Add(ReservationAutoExpiration),
                AccessCode = HandleGenerator.Generate(6)
            });
            this.Update(new OrderTotalsCalculated {
                Total = totals.Total,
                Lines = totals.Lines != null ? totals.Lines.ToArray() : null,
                IsFreeOfCharge = totals.Total == 0m
            });
        }

        public void UpdateSeats(IEnumerable<OrderItem> items, IPricingService pricingService) {
            var all = ConvertItems(items);
            var totals = pricingService.CalculateTotal(this._conferenceId, all.AsReadOnly());

            this.Update(new OrderUpdated() {Seats = all});
            this.Update(new OrderTotalsCalculated {
                Total = totals.Total,
                Lines = totals.Lines != null ? totals.Lines.ToArray() : null,
                IsFreeOfCharge = totals.Total == 0m
            });
        }

        public void MarkAsReserved(IPricingService pricingService, DateTime expirationDate,
            IEnumerable<SeatQuantity> reservedSeats) {
            if(_isConfirmed) {
                throw new InvalidOperationException("Cannot modify a confirmed order.");
            }

            var reserved = reservedSeats.ToList();

            if(this._seats.Any(item => item.Quantity != 0
                && !reserved.Any(seat => seat.SeatType == item.SeatType && seat.Quantity == item.Quantity))) {
                var totals = pricingService.CalculateTotal(this._conferenceId, reserved.AsReadOnly());
                this.Update(new OrderPartiallyReserved{ReservationExpiration = expirationDate, Seats = reserved.ToArray()});
                this.Update(new OrderTotalsCalculated {
                    Total = totals.Total,
                    Lines = totals.Lines != null ? totals.Lines.ToArray():null,
                    IsFreeOfCharge = totals.Total == 0m
                });
            }
            else {
                this.Update(new OrderReservationCompleted() {
                    ReservationExpiration = expirationDate,
                    Seats = reserved.ToArray()
                });
            }
        }

        public void Expire() {
            if(_isConfirmed) {
                throw new InvalidOperationException("Cannot expire a confirmed order.");
            }
            this.Update(new OrderExpired());
        }

        public void Confirm() {
            this.Update(new OrderConfirmed());
        }

        public void AssignRegistrant(string firstName, string lastName, string email) {
            this.Update(new OrderRegistrantAssigned {
                FirstName = firstName,
                LastName = lastName,
                Email = email
            });
        }

        public SeatAssignments CreateSeatAssignments() {
            if(!this._isConfirmed) {
                throw new InvalidOperationException("Cannot create seat assignments for an order that isn't confirmed yet.");
            }

            return new SeatAssignments(Id, _seats.AsReadOnly());
        }

        private static List<SeatQuantity> ConvertItems(IEnumerable<OrderItem> items) {
            return items.Select(x => new SeatQuantity(x.SeatType, x.Quantity)).ToList();
        } 

        private void OnOrderPlaced(OrderPlaced e) {
            this._conferenceId = e.ConferenceId;
            this._seats = e.Seats.ToList();
        }

        private void OnOrderUpdated(OrderUpdated e) {
            this._seats = e.Seats.ToList();
        }

        private void OnOrderPartiallyReserved(OrderPartiallyReserved e) {
            this._seats = e.Seats.ToList();
        }

        private void OnOrderReservationCompleted(OrderReservationCompleted e) {
            this._seats = e.Seats.ToList();
        }

        private void OnOrderExpired(OrderExpired e) {
            
        }

        private void OnOrderConfirmed(OrderConfirmed e) {
            this._isConfirmed = true;
        }

        private void OnOrderRegistrantAssigned(OrderRegistrantAssigned e) {
            
        }

        private void OnOrderTotalsCalculated(OrderTotalsCalculated e) {
            
        }
    }
}
