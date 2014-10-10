using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using Infrastructure.Messaging;
using Infrastructure.Processes;
using Infrastructure.Utils;
using Payments.Contracts.Events;
using Registration.Commands;
using Registration.Contracts.Events;
using Registration.Events;

namespace Registration {
    public class RegistrationProcessManager: IProcessManager {
        private static readonly TimeSpan BufferTimeBeforeReleasingSeatsAfterExpiration = TimeSpan.FromMinutes(14);
        public enum ProcessState {
            NotStarted = 0,
            AwaitingReservationConfirmation = 1,
            ReservationConfirmationReceived = 2,
            PaymentConfirmationReceived = 3
        }

        private readonly List<Envelope<ICommand>> _commands = new List<Envelope<ICommand>>();

        public RegistrationProcessManager() {
            this.Id = GuidUtil.NewSequentialId();
        }

        public Guid Id { get; private set; }
        public bool Completed { get; private set; }
        public Guid ConferenceId { get; set; }
        public Guid OrderId { get; set; }
        public Guid ReservationId { get; set; }
        public Guid SeatReservationCommandId { get; internal set; }

        public DateTime? ReservationAutoExpiration { get; internal set; }
        public Guid ExpirationCommandId { get; set; }

        public int StateValue { get; private set; }

        [NotMapped]
        public ProcessState State {
            get { return (ProcessState)this.StateValue; }
            set { this.StateValue = (int)value; }
        }

        [ConcurrencyCheck]
        [Timestamp]
        public byte[] TimeStamp { get; private set; }

        public IEnumerable<Envelope<ICommand>> Commands {
            get { return _commands; }
        }

        public void Handle(OrderPlaced message) {
            if(this.State == ProcessState.NotStarted) {
                this.ConferenceId = message.ConferenceId;
                this.OrderId = message.SourceId;

                this.ReservationId = message.SourceId;
                this.ReservationAutoExpiration = message.ReservationAutoExpiration;
                var expirationWindow = message.ReservationAutoExpiration.Subtract(DateTime.UtcNow);

                if(expirationWindow>TimeSpan.Zero) {
                    this.State = ProcessState.AwaitingReservationConfirmation;
                    var seatReservationCommand = new MakeSeatReservation {
                        ConferenceId = this.ConferenceId,
                        ReservationId = this.ReservationId,
                        Seats = message.Seats.ToList()
                    };

                    this.SeatReservationCommandId = seatReservationCommand.Id;

                    this.AddCommand(new Envelope<ICommand>(seatReservationCommand) {
                        TimeToLive = expirationWindow.Add(TimeSpan.FromMinutes(1))
                    });

                    var expirationCommand = new ExpireRegistrationProcess() {ProcessId = this.Id};
                    this.ExpirationCommandId = expirationCommand.Id;
                    this.AddCommand(new Envelope<ICommand>(expirationCommand) {
                        Delay = expirationWindow.Add(BufferTimeBeforeReleasingSeatsAfterExpiration)
                    });
                }
                else {
                    this.AddCommand(new RejectOrder {OrderId = this.OrderId});
                    this.Completed = true;
                }
            }
            else {
                if(message.ConferenceId!=this.ConferenceId) {
                    throw new InvalidOperationException();
                }
            }
        }

        public void Handle(OrderUpdated message) {
            if(this.State==ProcessState.AwaitingReservationConfirmation || 
                this.State==ProcessState.ReservationConfirmationReceived) {
                this.State = ProcessState.AwaitingReservationConfirmation;

                var seatReservationCommand = new MakeSeatReservation {
                    ConferenceId = this.ConferenceId,
                    ReservationId = this.ReservationId,
                    Seats = message.Seats.ToList()
                };

                this.SeatReservationCommandId = seatReservationCommand.Id;
                this.AddCommand(seatReservationCommand);
            }
            else {
                throw new InvalidOperationException("The order cannot be updated at this stage.");
            }
        }

        public void Handle(Envelope<SeatsReserved> envelope) {
            if(this.State==ProcessState.AwaitingReservationConfirmation) {
                if(envelope.CorrelationId!=null) {
                    if(string.CompareOrdinal(this.SeatReservationCommandId.ToString(), envelope.CorrelationId)!=0) {
                        Trace.TraceWarning(
                            "Seat reservation response for reservation id {0} does not match the expected correlation id.",
                            envelope.Body.ReservationId);
                        return;
                    }
                }

                this.State = ProcessState.ReservationConfirmationReceived;

                this.AddCommand(new MarkSeatsAsReserved {
                    OrderId = this.OrderId,
                    Seats = envelope.Body.ReservationDetails.ToList(),
                    Expiration = this.ReservationAutoExpiration.Value
                });
            }
            else if (string.CompareOrdinal(this.SeatReservationCommandId.ToString(), envelope.CorrelationId) == 0) {
                Trace.TraceInformation("Seat reservation response for request {1} for reservation id {0} was already handled. Skipping event.", envelope.Body.ReservationId, envelope.CorrelationId);
            }
            else {
                throw new InvalidOperationException("Cannot handle seat reservation at this stage.");
            }
        }

        public void Handle(PaymentCompleted @event) {
            if(this.State == ProcessState.ReservationConfirmationReceived) {
                this.State = ProcessState.PaymentConfirmationReceived;
                this.AddCommand(new ConfirmOrder{OrderId = this.OrderId});
            }
            else {
                throw new InvalidOperationException("Cannot handle payment confirmation at this stage.");
            }
        }

        public void Handle(OrderConfirmed @event) {
            if(this.State == ProcessState.ReservationConfirmationReceived 
                || this.State == ProcessState.PaymentConfirmationReceived) {
                this.ExpirationCommandId = Guid.Empty;
                this.Completed = true;

                this.AddCommand(new CommitSeatReservation {
                    ReservationId = this.ReservationId,
                    ConferenceId = this.ConferenceId
                });
            }
            else {
                throw new InvalidOperationException("Cannot handle order confirmation at this stage.");
            }
        }

        public void Handle(ExpireRegistrationProcess command) {
            if(this.ExpirationCommandId == command.Id) {
                this.Completed = true;

                this.AddCommand(new RejectOrder {OrderId = this.OrderId});
                this.AddCommand(new CancelSeatReservation {
                    ConferenceId = this.ConferenceId,
                    ReservationId = this.ReservationId
                });

                // TODO cancel payment if any
            }

            // else ignore the message as it is no longer relevant (but not invalid)
        }

        private void AddCommand<T>(T command) where T: ICommand {
            this._commands.Add(Envelope.Create<ICommand>(command));
        }

        private void AddCommand(Envelope<ICommand> envelope) {
            this._commands.Add(envelope);
        }
    }
}