using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Workflow.Core;
using Workflow.Core.Persistance;

namespace Workflow.ScrachPad
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            /*
             Database scripts
            https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/how-to-enable-sql-persistence-for-workflows-and-workflow-services
            C:\Windows\Microsoft.NET\Framework\v4.0.30319\SQL\en
            SqlWorkflowInstanceStoreSchema.sql
            SqlWorkflowInstanceStoreLogic.sql
             */

            //Store connection string
            PersistanceHelper.Store = new SqlWorkflowInstanceStore(@"Data Source=LAFDEV3\SQL2014;Initial Catalog=Corey-WfPersistenceStore;Integrated Security=True;Async=true")
            {
                InstanceCompletionAction = InstanceCompletionAction.DeleteNothing,
                RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 5),
                InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry,
            };

            //Reload any instances that persisted since you last ran the application.
            PersistanceHelper.ReconstituteRunnableInstances();

            ExampleActivity activity = new ExampleActivity();

            //Defines the application completely so it can be resumed.
            ApplicationHelper app = new ApplicationHelper(
                activity,
                activity.GetIdentity(), 
                new Core.Models.DataEventArgs() {
                    EventDate  = DateTime.Now,
                    SourceApiName = "Program-Application",
                    SourceFriendlyName = "Program Application",
                    SourceTransactionId = Guid.NewGuid().ToString(),
                    SourceType = new Core.Models.TypeWrapper(typeof(ExampleActivity))                    
             });

            app.Run();

            //Wait here so the application can run in the background
            Console.ReadLine();
        }
    }
}
