using System;
using System.Linq;
using NUnit.Framework;
using Payments.Contracts.Events;

namespace Payments.Tests {
    [TestFixture]
    public class ThirdPartyProcessorPaymentFixture {
        private static readonly Guid PaymentId = Guid.NewGuid();
        private static readonly Guid SourceId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();

        private ThirdPartyProcessorPayment _sut;

        [SetUp]
        public void Setup() {
            _sut = new ThirdPartyProcessorPayment(PaymentId, SourceId, "payment", 300,
                new[] {new ThirdPartyProcessorPaymentItem("item1", 100), new ThirdPartyProcessorPaymentItem("item2", 200)});
        }

        [Test]
        public void When_initiating_payment_then_status_is_initiated() {
            Assert.AreEqual(PaymentId, _sut.Id);
            Assert.AreEqual(ThirdPartyProcessorPayment.States.Initiated, _sut.State);
        }

        [Test]
        public void WhenInitiatingPaymentThenRaisesIntegrationEvent() {
            Assert.AreEqual(1, _sut.Events.Count());
            Assert.AreEqual(PaymentId, ((PaymentInitiated)_sut.Events.Single()).SourceId);
            Assert.AreEqual(SourceId, ((PaymentInitiated)_sut.Events.Single()).PaymentSourceId);
        }

        [Test]
        public void WhenCompletingPaymentThenChangesStatus() {
            this._sut.Complete();
            Assert.AreEqual(ThirdPartyProcessorPayment.States.Completed, _sut.State);
        }

        [Test]
        public void WhenCompletingPaymentThenNotifiesEvent() {
            this._sut.Complete();

            var @event = (PaymentCompleted)_sut.Events.Last();
            Assert.AreEqual(PaymentId, @event.SourceId);
            Assert.AreEqual(SourceId, @event.PaymentSourceId);
        }

        [Test]
        public void WhenRejectingPaymentThenChangesStatus() {
            this._sut.Cancel();
            Assert.AreEqual(ThirdPartyProcessorPayment.States.Rejected, _sut.State);
        }

        [Test]
        public void WhenRejectingPaymentThenNotifiesEvent() {
            this._sut.Cancel();

            var @event = (PaymentRejected)_sut.Events.Last();
            Assert.AreEqual(PaymentId, @event.SourceId);
            Assert.AreEqual(SourceId, @event.PaymentSourceId);
        }
    }
}