using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Infrastructure.MessageLog;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Infrastructure.Sql.MessageLog;
using Moq;
using NUnit.Framework;

namespace Infrastructure.Sql.IntegrationTests {
    [TestFixture]
    public class SqlEventLogFixture {
        private string _dbName = "SqlEventLogFixture_" + Guid.NewGuid();
        private SqlMessageLog _messageLog;
        private Mock<IMetadataProvider> _metadata;

        private EventA _eventA;
        private EventB _eventB;
        private EventC _eventC;

        [SetUp]
        public void Setup() {
            using(var context = new MessageLogDbContext(_dbName)) {
                if(context.Database.Exists()) {
                    context.Database.Delete();
                }

                context.Database.Create();
            }

            _eventA = new EventA();
            _eventB = new EventB();
            _eventC = new EventC();

            var metadata = Mock.Of<IMetadataProvider>(x =>
                x.GetMetadata(_eventA) == new Dictionary<string, string> {
                    {StandardMetadata.SourceId, _eventA.SourceId.ToString()},
                    {StandardMetadata.SourceType, "SourceA"},
                    {StandardMetadata.Kind, StandardMetadata.EventKind},
                    {StandardMetadata.AssemblyName, "A"},
                    {StandardMetadata.Namespace, "Namespace"},
                    {StandardMetadata.FullName, "Namespace.EventA"},
                    {StandardMetadata.TypeName, "EventA"}
                } &&
                x.GetMetadata(_eventB) == new Dictionary<string, string> {
                    {StandardMetadata.SourceId, _eventA.SourceId.ToString()},
                    {StandardMetadata.SourceType, "SourceB"},
                    {StandardMetadata.Kind, StandardMetadata.EventKind},
                    {StandardMetadata.AssemblyName, "B"},
                    {StandardMetadata.Namespace, "Namespace"},
                    {StandardMetadata.FullName, "Namespace.EventB"},
                    {StandardMetadata.TypeName, "EventB"}
                } &&
                x.GetMetadata(_eventC) == new Dictionary<string, string> {
                    {StandardMetadata.SourceId, _eventA.SourceId.ToString()},
                    {StandardMetadata.SourceType, "SourceC"},
                    {StandardMetadata.Kind, StandardMetadata.EventKind},
                    {StandardMetadata.AssemblyName, "B"},
                    {StandardMetadata.Namespace, "AnotherNamespace"},
                    {StandardMetadata.FullName, "AnotherNamespace.EventC"},
                    {StandardMetadata.TypeName, "EventC"}
                });

            this._metadata = Mock.Get(metadata);

            this._messageLog = new SqlMessageLog(_dbName, new JsonTextSerializer(), metadata);
            this._messageLog.Save(_eventA);
            this._messageLog.Save(_eventB);
            this._messageLog.Save(_eventC);
        }

        [TearDown]
        public void TearDown() {
            using (var context = new MessageLogDbContext(_dbName)) {
                if (context.Database.Exists()) {
                    context.Database.Delete();
                }
            }
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanReadAll() {
            var events = this._messageLog.ReadAll().ToList();
            Assert.AreEqual(3, events.Count);
        }
        
        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByAssembly() {
            var events = this._messageLog.Query(new QueryCriteria() {AssemblyNames = {"A"}}).ToList();
            Assert.AreEqual(1, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByAssemblies() {
            var events = this._messageLog.Query(new QueryCriteria() {AssemblyNames = {"A", "B"}}).ToList();
            Assert.AreEqual(3, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByNamespace() {
            var events = this._messageLog.Query(new QueryCriteria() {Namespaces = {"Namespace"}}).ToList();
            
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x=>x.SourceId==_eventA.SourceId));    
            Assert.IsTrue(events.Any(x=>x.SourceId==_eventB.SourceId));    
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByNamespaces() {
            var events = this._messageLog.Query(new QueryCriteria() {Namespaces = {"Namespace", "AnotherNamespace"}}).ToList();
            Assert.AreEqual(3, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByNamespaceAndAssembly() {
            var events = this._messageLog.Query(new QueryCriteria() {AssemblyNames = {"B"}, Namespaces = {"AnotherNamespace"}}).ToList();
            
            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x=>x.SourceId==_eventC.SourceId));   
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByNamespaceAndAssembly2() {
            var events = this._messageLog.Query(new QueryCriteria() {AssemblyNames = {"A"}, Namespaces = {"AnotherNamespace"}}).ToList();
            
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByFullName() {
            var events = this._messageLog.Query(new QueryCriteria() { FullNames = { "Namespace.EventA" } }).ToList();

            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByFullNames() {
            var events = this._messageLog.Query(new QueryCriteria() { FullNames = { "Namespace.EventA", "AnotherNamespace.EventC" } }).ToList();
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
            Assert.IsTrue(events.Any(x => x.SourceId == _eventC.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByTypeName() {
            var events = this._messageLog.Query(new QueryCriteria() { TypeNames = { "EventA" } }).ToList();

            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByTypeNames() {
            var events = this._messageLog.Query(new QueryCriteria() { TypeNames = { "EventA", "EventC" } }).ToList();
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
            Assert.IsTrue(events.Any(x => x.SourceId == _eventC.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterByTypeNameAndAssembly() {
            var events = this._messageLog.Query(new QueryCriteria() { AssemblyNames = {"B"}, TypeNames = { "EventB", "EventC" } }).ToList();

            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventB.SourceId));
            Assert.IsTrue(events.Any(x => x.SourceId == _eventC.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterBySourceId() {
            var events = this._messageLog.Query(new QueryCriteria() { SourceIds = { _eventA.SourceId.ToString() } }).ToList();

            Assert.AreEqual(1, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterBySourceIds() {
            var events = this._messageLog.Query(new QueryCriteria() { SourceIds = { _eventA.SourceId.ToString(), _eventC.SourceId.ToString() } }).ToList();
            Assert.AreEqual(2, events.Count);
            Assert.IsTrue(events.Any(x => x.SourceId == _eventA.SourceId));
            Assert.IsTrue(events.Any(x => x.SourceId == _eventC.SourceId));
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterBySourceType() {
            var events = this._messageLog.Query(new QueryCriteria() { SourceTypes = { "SourceA" } }).ToList();
            Assert.AreEqual(1, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterBySourceTypes() {
            var events = this._messageLog.Query(new QueryCriteria() { SourceTypes = { "SourceA", "SourceB" } }).ToList();
            Assert.AreEqual(2, events.Count);
        }
        
        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterInEndDate() {
            var events = this._messageLog.Query(new QueryCriteria() { EndDate = DateTime.UtcNow}).ToList();
            Assert.AreEqual(3, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanFilterOutEndDate() {
            var events = this._messageLog.Query(new QueryCriteria() { EndDate = DateTime.UtcNow.AddMinutes(-1)}).ToList();
            Assert.AreEqual(0, events.Count);
        }

        [Test]
        public void GivenASqlLogWithThreeEventsThenCanUseFluentCriteriaBuilder() {
            var events = this._messageLog.Query()
                .FromAssembly("A").FromAssembly("B")
                .FromNamespace("Namespace")
                .FromSource("SourceB")
                .WithTypeName("EventB")
                .WithFullName("Namespace.EventB")
                .Until(DateTime.UtcNow).ToList();
            Assert.AreEqual(1, events.Count);
        }

        class EventA: IEvent {
            public EventA() {
                this.SourceId = Guid.NewGuid();
            }

            public Guid SourceId { get; set; }
        } 

        class EventB: IEvent {
            public EventB() {
                this.SourceId = Guid.NewGuid();
            }

            public Guid SourceId { get; set; }
        } 

        class EventC: IEvent {
            public EventC() {
                this.SourceId = Guid.NewGuid();
            }

            public Guid SourceId { get; set; }
        }
    }
}