using System;
using System.Data.Entity;
using System.Linq;

namespace Registration.ReadModel.Implementation {
    public class ConferenceRegistrationDbContext: DbContext {
        public const string SchemaName = "ConferenceRegistration";

        public ConferenceRegistrationDbContext(string nameOrConnectionString): base(nameOrConnectionString) {}

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DraftOrder>().ToTable("OrdersViewV3", SchemaName);
            modelBuilder.Entity<DraftOrder>().HasMany(o => o.Lines).WithRequired();
            modelBuilder.Entity<DraftOrderItem>().ToTable("OrderItemsViewV3", SchemaName);
            modelBuilder.Entity<DraftOrderItem>().HasKey(item => new {item.OrderId, item.SeatType});

            modelBuilder.Entity<PricedOrder>().ToTable("PricedOrdersV3", SchemaName);
            modelBuilder.Entity<PricedOrder>().HasMany(c => c.Lines).WithRequired().HasForeignKey(x => x.OrderId);
            modelBuilder.Entity<PricedOrderLine>().ToTable("PriceOrderLinesV3", SchemaName);
            modelBuilder.Entity<PricedOrderLine>().HasKey(seat => new {seat.OrderId, seat.Position});
            modelBuilder.Entity<PricedOrderLineSeatTypeDescription>().ToTable("PricedOrderLineSeatTypeDescriptionsV3",
                SchemaName);

            modelBuilder.Entity<Conference>().ToTable("ConferencesView", SchemaName);
            modelBuilder.Entity<SeatType>().ToTable("ConferenceSeatTypesView", SchemaName);
        }

        public T Find<T>(Guid id) where T: class {
            return this.Set<T>().Find(id);
        }

        public IQueryable<T> Query<T>() where T: class {
            return this.Set<T>();
        }

        public void Save<T>(T entity) where T: class {
            var entry = this.Entry(entity);

            if(entry.State==EntityState.Detached) {
                this.Set<T>().Add(entity);
            }

            this.SaveChanges();
        }
    }
}