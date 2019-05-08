using log4net;
using System;
using System.Activities;
using System.Activities.DurableInstancing;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Workflow.Core.Persistance
{
    public class PersistanceHelper
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(PersistanceHelper));
        /// <summary>
        /// Creates an static instance of the application. The Version should be set in a configuration or constant.
        /// </summary>
        private static PersistanceHelper _thisInstance = new PersistanceHelper("No-Version");
        private InstanceStore _store;

        /// <summary>
        /// What to do when an activity is recovered from running ReconstituteRunnableInstances and then is idle. 
        /// </summary>
        public static PersistableIdleAction IdleAction { get; set; } = PersistableIdleAction.Persist;

        /// <summary>
        /// SQL Persistance Database store. The connection in the store is also used in the Data context to get extra information from the database. This operation also sets the instance store owner.
        ///    
        /// Database scripts needed:
        ///    https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/how-to-enable-sql-persistence-for-workflows-and-workflow-services
        ///    C:\Windows\Microsoft.NET\Framework\v4.0.30319\SQL\en
        ///    SqlWorkflowInstanceStoreSchema.sql
        ///    SqlWorkflowInstanceStoreLogic.sql
        /// </summary>
        public static InstanceStore Store {
            get {
                return _thisInstance._store;
            }
            set {
                _thisInstance._store = value;

                //Set the owner
                _thisInstance._ownerHandle = CreateInstanceStoreOwner(
                    _thisInstance._store, 
                    _thisInstance._wfHostTypeName);
            }
        }

        // Create a unique name that is used to associate instances in the instance store hosts that can load them. This is needed to prevent a Workflow host from loading
        // instances that have different implementations. The unique name should change whenever the implementation of the workflow changes to prevent workflow load exceptions.
        // For the purposes of the demo we create a unique name every time the program is run.
        private XName _wfHostTypeName;

        // Create an InstanceStore owner that is associated with the workflow type
        private InstanceHandle _ownerHandle;

        /// <summary>
        /// Unique identifier for the Owner
        /// </summary>
        public static XName HostTypeName { get { return _thisInstance._wfHostTypeName; } }

        // A well known property that is needed by WorkflowApplication and the InstanceStore
        public static XName WorkflowHostTypePropertyName { get; private set; } = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties").GetName("WorkflowHostType");
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="applicationInstanceVersion">A unique name to this application. In most cases this does not apply, but if you have multiple instances running this needs to change.</param>
        private PersistanceHelper(string applicationInstanceVersion)
        {
            _wfHostTypeName = XName.Get(
                applicationInstanceVersion, 
                //This represents the ownership on the machine
                Environment.MachineName);            
        }

        /// <summary>
        /// Configure a Default Owner for the instance store so instances can be re-loaded from WorkflowApplication
        /// </summary>
        /// <param name="store">Database Store</param>
        /// <param name="wfHostTypeName">Unique host name</param>
        /// <returns></returns>
        private static InstanceHandle CreateInstanceStoreOwner(InstanceStore store, XName wfHostTypeName)
        {
            InstanceHandle ownerHandle = store.CreateInstanceHandle();

            CreateWorkflowOwnerCommand ownerCommand = new CreateWorkflowOwnerCommand()
            {
                InstanceOwnerMetadata =
                {
                    {
                        WorkflowHostTypePropertyName,
                        new InstanceValue(wfHostTypeName)
                    }
                }
            };
            _log.Debug("Owner: " + ownerCommand.InstanceOwnerMetadata.Values.First().ToString());
            store.DefaultInstanceOwner = store.Execute(ownerHandle, ownerCommand, TimeSpan.FromSeconds(30)).InstanceOwner;

            return ownerHandle;
        }

        /// <summary>
        /// Find any workflows that are not completed and load them up to start running.  If an activity has found to have another owner, it will wait 30 seconds and try again to reload it. If a lock still exists an exception will bubble up.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">How many to load at once.</param>
        /// <exception cref="System.Runtime.DurableInstancing.InstanceLockedException"></exception>
        public static void ReconstituteRunnableInstances(int maxDegreeOfParallelism = 4)
        {
            var maxParallelization = new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism };

            using (var db = new SqlPersistanceContext())
            {
                //Only get the instances that are not completed.
                var result = from i in db.Instances where ! i.IsCompleted select i;

                //Run each item in parallel they will block automatically.
                Parallel.ForEach(result, maxParallelization,(item) =>
                {
                    bool shouldTryAgain = false;
                    int tryAgainCount = 0;
                    do
                    {
                        using (ApplicationHelper application = new ApplicationHelper(item.GetActivity(), item.GetIdentity()))
                        {
                            application.IdleAction = IdleAction;
                            try
                            {
                                _log.Debug($"Reloading InstanceId={item.InstanceId} IdleAction={IdleAction}");
                                application.ReloadAndRun(item.InstanceId);
                            }
                            catch (InstanceLockedException ex)
                            {
                                var owner = GetOwnerInformation(ex.InstanceOwnerId);
                                //If the lock is less then 30 seconds just sit back and relax and wait
                                if (owner != null && owner.TimeToLockExpire < 30)
                                {
                                    _log.Warn("Sleep thread to wait for the owner to expire then trying again", ex);
                                    System.Threading.Thread.Sleep((int)(owner.TimeToLockExpire + 5d) * 1000);
                                    shouldTryAgain = true;
                                }
                                else
                                    throw ex;                                
                            }
                        }

                        //Make sure it does not try too many times
                        tryAgainCount++;

                    } while (shouldTryAgain && tryAgainCount <=1);
                 });
            }                
        }

        /// <summary>
        /// Gets the owner from the database. If not found null is returned.
        /// </summary>
        /// <param name="id">Instance owner id</param>
        /// <returns></returns>
        public static Models.LockOwner GetOwnerInformation(Guid id)
        {
            using (SqlPersistanceContext db = new SqlPersistanceContext())
            {
                return db.LockOwners.FirstOrDefault(row => row.Id == id);
            }
        }
    }
}
