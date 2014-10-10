using System.Data.Entity;
using Infrastructure.Sql.Processes;

namespace Registration.Database {
    public class RegistrationProcessManagerDbContext: DbContext {
        public const string SchemaName = "ConferenceRegistrationProcesses";

        public RegistrationProcessManagerDbContext(string nameOrConnectionString): base(nameOrConnectionString) {
            
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegistrationProcessManager>().ToTable("RegistrationProcess", SchemaName);
            modelBuilder.Entity<UndispatchedMessages>().ToTable("UndispatchedMessages", SchemaName);
        }

        public DbSet<RegistrationProcessManager> RegistrationProcesses { get; set; } 
        public DbSet<UndispatchedMessages> UndispatchedMessages { get; set; } 
    }
}