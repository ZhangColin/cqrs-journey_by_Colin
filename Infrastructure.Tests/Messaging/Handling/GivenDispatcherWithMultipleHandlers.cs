using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Tests.Messaging.Handling {
    [TestFixture]
    public class GivenDispatcherWithMultipleHandlers {
        private EventDispatcher _sut;
        private Mock<IEventHandler> _handler1Mock;
        private Mock<IEventHandler> _handler2Mock;

        public GivenDispatcherWithMultipleHandlers() {
            this._sut = new EventDispatcher();

            this._handler1Mock = new Mock<IEventHandler>();
            this._handler1Mock.As<IEnvelopedEventHandler<EventA>>();
            this._handler1Mock.As<IEventHandler<EventB>>();

            this._sut.Register(this._handler1Mock.Object);

            this._handler2Mock = new Mock<IEventHandler>();
            this._handler2Mock.As<IEventHandler<EventA>>();

            this._sut.Register(this._handler2Mock.Object);
        }

        [Test]
        public void When_dispatching_an_event_with_multiple_registered_handlers_then_invokes_handlers() {
            var @event = new EventA();

            this._sut.DispatchMessage(@event, "message", "correlation", "");

            this._handler1Mock.As<IEnvelopedEventHandler<EventA>>().Verify(
                h => h.Handle(It.Is<Envelope<EventA>>(
                    e => e.Body == @event && e.MessageId == "message" && e.CorrelationId == "correlation")),
                Times.Once);
            this._handler2Mock.As<IEventHandler<EventA>>().Verify(h => h.Handle(@event), Times.Once());
        }

        [Test]
        public void When_dispatching_an_event_with_single_registered_handler_then_invokes_handler() {
            var @event = new EventB();
            this._sut.DispatchMessage(@event, "message", "correlation", "");
            this._handler1Mock.As<IEventHandler<EventB>>().Verify(h => h.Handle(@event), Times.Once());
        }

        [Test]
        public void When_dispatching_an_event_with_no_registered_handler_then_does_noting() {
            var @event = new EventC();
            this._sut.DispatchMessage(@event, "message", "correlation", "");
        }
    }
}