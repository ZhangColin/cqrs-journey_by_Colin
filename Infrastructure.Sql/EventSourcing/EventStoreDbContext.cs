using System.Data.Entity;

namespace Infrastructure.Sql.EventSourcing {
    public class EventStoreDbContext: DbContext {
        public const string SchemaName = "Events";

        public EventStoreDbContext(string nameOrConnectionString): base(nameOrConnectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Event>().HasKey(x => new {x.AggregateId, x.AggregateType, x.Version}).ToTable("Events",
                SchemaName);
        }
    }
}