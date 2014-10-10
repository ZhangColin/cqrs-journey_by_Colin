using System;
using Infrastructure.Database;

namespace Infrastructure.Sql.IntegrationTests {
    public class TestAggregateRoot: IAggregateRoot {
        public Guid Id { get; set; }
        public string Title { get; set; }

        protected TestAggregateRoot() {}

        public TestAggregateRoot(Guid id) {
            this.Id = id;
        }
    }
}