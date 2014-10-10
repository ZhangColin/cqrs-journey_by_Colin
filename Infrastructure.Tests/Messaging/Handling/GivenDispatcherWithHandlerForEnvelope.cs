using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Tests.Messaging.Handling {
    [TestFixture]
    public class GivenDispatcherWithHandlerForEnvelope {
        private EventDispatcher _sut;
        private Mock<IEventHandler> _handlerMock;

        public GivenDispatcherWithHandlerForEnvelope() {
            this._sut = new EventDispatcher();

            this._handlerMock = new Mock<IEventHandler>();
            this._handlerMock.As<IEnvelopedEventHandler<EventA>>();

            this._sut.Register(this._handlerMock.Object);
        }

        [Test]
        public void When_dispatching_an_event_with_registered_handler_Then_invokes_handler() {
            var @event = new EventA();

            this._sut.DispatchMessage(@event, "message", "correlation", "");
            this._handlerMock.As<IEnvelopedEventHandler<EventA>>()
                .Verify(h => h.Handle(It.Is<Envelope<EventA>>(
                    e => e.Body == @event && e.MessageId == "message" && e.CorrelationId == "correlation")),
                    Times.Once);
        }

        [Test]
        public void When_dispatching_an_event_with_no_registered_handler_then_does_noting() {
            var @event = new EventC();
            this._sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }
}