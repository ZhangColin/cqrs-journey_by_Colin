using Infrastructure.Messaging.Handling;
using NUnit.Framework;

namespace Infrastructure.Tests.Messaging.Handling {
    [TestFixture]
    public class GivenEmptyDispatcher {
        private EventDispatcher _sut;
        public GivenEmptyDispatcher() {
            this._sut = new EventDispatcher();
        }

        [Test]
        public void When_dispatching_an_event_then_does_nothing() {
            var @event = new EventC();
            this._sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }
}