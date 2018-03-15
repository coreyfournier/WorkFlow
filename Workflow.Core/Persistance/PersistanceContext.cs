using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Persistance
{
    /// <summary>
    /// Database operations on the persistance store database set in the PersistanceHelper
    /// This is necessary to restore persisted workflows after the application has been terminated.
    /// </summary>
    public class SqlPersistanceContext : DbContext
    {
        public SqlPersistanceContext() : base(GetConnectionFromStore())
        {
            //Stop EntityFramework from managing the database.
            Database.SetInitializer<SqlPersistanceContext>(null);
        }
        public DbSet<Models.InstanceView> Instances { get; set; }
        public DbSet<Models.LockOwner> LockOwners { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            //Force the schema that is setup by default in the scripts
            modelBuilder.HasDefaultSchema("System.Activities.DurableInstancing");
        }

        /// <summary>
        /// Gets the connection
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private static string GetConnectionFromStore()
        {
            SqlWorkflowInstanceStore store = PersistanceHelper.Store as SqlWorkflowInstanceStore;

            if (store == null)
                throw new InvalidOperationException("The persistance store is set to a non sql store. These operations are not valid unless it's using SqlWorkflowInstanceStore");

            return store.ConnectionString;
        }
    }
}
