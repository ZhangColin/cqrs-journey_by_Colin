using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.Processes;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class GivenContextThatStallsOnSaveAndOnFindWhenPublishing {

        private TestCommand _command1;
        private TestCommand _command2;
        private TestCommand _command3;
        private Mock<ICommandBus> _bus1;
        private List<Exception> _exceptions;
        private ManualResetEvent _saveFinished;
        private AutoResetEvent _sendContinueResetEvent1;
        private AutoResetEvent _sendStartedResetEvent1;
        private Mock<ICommandBus> _bus2;
        private AutoResetEvent _sendContinueResetEvent2;
        private AutoResetEvent _sendStartedResetEvent2;
        private ManualResetEvent _findAndSaveFinished;
        
        protected readonly string DbName = typeof(GivenContextThatStallsOnSaveAndOnFindWhenPublishing).Name + "-" + Guid.NewGuid();

        [SetUp]
        public void Setup() {
            using (var context = new TestProcessManagerDbContext(this.DbName)) {
                context.Database.Delete();
                context.Database.Create();
            }

            this._bus1 = new Mock<ICommandBus>();
            this._command1 = new TestCommand();
            this._command2 = new TestCommand();
            this._command3 = new TestCommand();

            var id = Guid.NewGuid();
            this._exceptions = new List<Exception>();

            this._saveFinished = new ManualResetEvent(false);
            this._sendContinueResetEvent1 = new AutoResetEvent(false);
            this._sendStartedResetEvent1 = new AutoResetEvent(false);

            this._bus1.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)))
                .Callback(() => {
                    this._sendStartedResetEvent1.Set();
                    this._sendContinueResetEvent1.WaitOne();
                });

            Task.Factory.StartNew(() => {
                using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(
                    () => new TestProcessManagerDbContext(this.DbName), this._bus1.Object, new JsonTextSerializer())) {
                    var aggregate = new OrmTestProcessManager(id);
                    aggregate.AddEnvelope(new Envelope<ICommand>(this._command1), new Envelope<ICommand>(this._command2),
                        new Envelope<ICommand>(this._command3));
                    context.Save(aggregate);
                }
            }).ContinueWith(t => this._exceptions.Add(t.Exception.InnerException), TaskContinuationOptions.OnlyOnFaulted)
                .ContinueWith(t => this._saveFinished.Set());

            Assert.IsTrue(this._sendStartedResetEvent1.WaitOne(3000));

            this._bus2 = new Mock<ICommandBus>();
            this._sendContinueResetEvent2 = new AutoResetEvent(false);
            this._sendStartedResetEvent2 = new AutoResetEvent(false);
            this._bus2.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)))
                .Callback(() => {
                    this._sendStartedResetEvent2.Set();
                    this._sendContinueResetEvent2.WaitOne();
                });

            this._findAndSaveFinished = new ManualResetEvent(false);

            Task.Factory.StartNew(() => {
                using(var context = new SqlProcessManagerDataContext<OrmTestProcessManager>(
                    () => new TestProcessManagerDbContext(this.DbName), this._bus2.Object, new JsonTextSerializer())) {
                    var entity = context.Find(id);
                    context.Save(entity);
                }
            }).ContinueWith(t => this._exceptions.Add(t.Exception.InnerException), TaskContinuationOptions.OnlyOnFaulted)
                .ContinueWith(t => this._findAndSaveFinished.Set());
        }

        [TearDown]
        public void TearDown() {
            using (var context = new TestProcessManagerDbContext(this.DbName)) {
                context.Database.Delete();
            }
        }

        [Test]
        public void WhenSaveFinishesSendingFirstThenFindIgnoresConcurrencyExceptionAndRefreshesContext() {
            Assert.IsTrue(this._sendStartedResetEvent2.WaitOne(3000));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command1.Id)));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command2.Id)));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command3.Id)), Times.Never);
            
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command1.Id)));
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command2.Id)));
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command3.Id)), Times.Never);

            this._sendContinueResetEvent1.Set();
            Assert.IsTrue(this._saveFinished.WaitOne(3000));

            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id == this._command3.Id)));

            this._sendContinueResetEvent2.Set();
            Assert.IsTrue(this._findAndSaveFinished.WaitOne(3000));

            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));

            Assert.AreEqual(0, this._exceptions.Count);
        }
        
        [Test]
        public void WhenFindFinishesPublishingFirstThenSaveIgnoresConcurrencyException() {
            Assert.IsTrue(this._sendStartedResetEvent2.WaitOne(3000));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command1.Id)));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command2.Id)));
            this._bus1.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command3.Id)), Times.Never);
            
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command1.Id)));
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command2.Id)));
            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id==this._command3.Id)), Times.Never);

            this._sendContinueResetEvent2.Set();
            Assert.IsTrue(this._findAndSaveFinished.WaitOne(3000));

            this._bus2.Verify(x=>x.Send(It.Is<Envelope<ICommand>>(c=>c.Body.Id == this._command3.Id)));

            this._sendContinueResetEvent1.Set();
            Assert.IsTrue(this._saveFinished.WaitOne(3000));

            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));

            Assert.AreEqual(0, this._exceptions.Count);
        }

        [Test]
        public void WhenSaveThrowsSendingFirstThenFindIgnoresConcurrencyExceptionAndRefreshesContext() {
            this._bus1.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)))
                .Throws<TimeoutException>();

            Assert.IsTrue(this._sendStartedResetEvent2.WaitOne(3000));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command1.Id)));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)), Times.Never);

            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command1.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)), Times.Never);

            this._sendContinueResetEvent1.Set();
            Assert.IsTrue(this._saveFinished.WaitOne(3000));
            
            this._sendContinueResetEvent2.Set();
            Assert.IsTrue(this._findAndSaveFinished.WaitOne(3000));

            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));

            Assert.AreEqual(1, this._exceptions.Count);
            Assert.IsAssignableFrom<TimeoutException>(this._exceptions[0]);
        }
        
        [Test]
        public void WhenSaveThrowsSendingAfterFindSentEverythingThenIgnoresConcurrencyExceptionAndSurfacesOriginal() {
            this._bus1.Setup(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)))
                .Throws<TimeoutException>();

            Assert.IsTrue(this._sendStartedResetEvent2.WaitOne(3000));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command1.Id)));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)));
            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)), Times.Never);

            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command1.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command2.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)), Times.Never);

            this._sendContinueResetEvent2.Set();
            Assert.IsTrue(this._findAndSaveFinished.WaitOne(3000));

            this._sendContinueResetEvent1.Set();
            Assert.IsTrue(this._saveFinished.WaitOne(3000));

            this._bus1.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));
            this._bus2.Verify(x => x.Send(It.Is<Envelope<ICommand>>(c => c.Body.Id == this._command3.Id)));

            Assert.AreEqual(1, this._exceptions.Count);
            Assert.IsAssignableFrom<TimeoutException>(this._exceptions[0]);
        }
    }
}