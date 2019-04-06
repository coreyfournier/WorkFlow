using log4net;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Workflow.Core.Models;
using Workflow.Core.Persistance;

namespace Workflow.Core
{
    public sealed class ApplicationHelper : IDisposable
    {
        /// <summary>
        /// Expected argument name of all activies.
        /// </summary>
        public const string DataEventArgumentName = "DataEventArgument";

        /// <summary>
        /// Expected argument name for the subscriber information
        /// </summary>
        public const string SubscriberArgumentName = "SubscriberArgument";

        static readonly ILog _log = LogManager.GetLogger(typeof(ApplicationHelper));
        /// <summary>
        /// Wait handle used for workflows that are reconstitued.
        /// </summary>
        AutoResetEvent _reloadWaitHandler = new AutoResetEvent(false);
        readonly WorkflowApplication _application;        

        /// <summary>
        /// Public access to the underlying application.
        /// </summary>
        public WorkflowApplication Application {
            get { return _application; }
        }

        /// <summary>
        /// When the activity goes idle, it will be unloaded from the application. Defaults to false
        /// </summary>
        public bool UnloadOnIdle{ get; set; }

        /// <summary>
        /// Time out to start the process. 30 seconds.
        /// </summary>
        public static readonly TimeSpan _timeOut = new TimeSpan(0, 0, 30);

        public delegate void ActivityCompleted(WorkflowApplicationCompletedEventArgs args);
        public delegate void ActivityAborted(WorkflowApplicationAbortedEventArgs args);
        public delegate void ActivityUnhandledException(WorkflowApplicationUnhandledExceptionEventArgs args);
        public delegate void ActivityWillBePersisted(WorkflowApplicationIdleEventArgs args);
        public delegate void ActivityStarted(Guid instanceId, Activity activity);

        /// <summary>
        /// Called after the activity is loaded in the WorkFlowApplication and Run is called. If the activity was persisted it will be after Load is called.
        /// </summary>
        public event ActivityStarted ActivityStartedEvent;

        /// <summary>
        /// Event when the activity is ready to be persisted. The persistance will occur some time after this is called, but not before.
        /// </summary>
        public event ActivityWillBePersisted ActivityWillBePersistedEvent;

        /// <summary>
        /// Any activity that throws an exception
        /// </summary>
        public event ActivityUnhandledException ActivityUnhandledExceptionEvent;

        /// <summary>
        /// Activity has been aborted
        /// </summary>
        public event ActivityAborted ActivityAbortedEvent;

        /// <summary>
        /// Event to get notified when the activity has completed.
        /// </summary>
        public event ActivityCompleted ActivityCompletedEvent;

        /// <summary>
        /// Creates the application instance with event handlers. Workflow must accept 'DataEventArgument' as a parameter of type DataEventArgs
        /// Relies on: PersistanceHelper.WorkflowHostTypePropertyName, PersistanceHelper.HostTypeName, PersistanceHelper.Store
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="identity"></param>
        /// <param name="args"></param>
        /// <remarks>DO NOT get the ID of the workflow instance before it's used otherwise on unpersisted workflows they will not work.</remarks>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public ApplicationHelper(Activity activity, WorkflowIdentity identity, DataEventArgs args = null)  : 
            this(activity, identity, 
                //Supply null arguments if they are passed in as null. Other wise default
                args == null ? null : new Dictionary <string, object> { { DataEventArgumentName, args  }})
        {
            
        }

        /// <summary>
        /// Creates the application instance with event handlers. Workflow must accept 'DataEventArgument' as a parameter of type DataEventArgs and 'SubscriberArgument' of type Subscriber
        /// Relies on: PersistanceHelper.WorkflowHostTypePropertyName, PersistanceHelper.HostTypeName, PersistanceHelper.Store
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="identity"></param>
        /// <param name="args"></param>
        /// <param name="subscriber">Subscriber information that includes configuration information</param>
        /// <remarks>DO NOT get the ID of the workflow instance before it's used otherwise on unpersisted workflows they will not work.</remarks>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public ApplicationHelper(Activity activity, WorkflowIdentity identity, DataEventArgs args, Subscriber subscriber) :
            this(activity, identity,
                //Supply null arguments if they are passed in as null. Other wise default
                args == null ? null : new Dictionary<string, object> {
                    { DataEventArgumentName, args },
                    { SubscriberArgumentName, subscriber }
                })
        {

        }


        /// <summary>
        /// Creates the application instance with event handlers. Not using the DataEventArgument will prevent transaction id's from propagating through log4net.
        /// Relies on: PersistanceHelper.WorkflowHostTypePropertyName, PersistanceHelper.HostTypeName, PersistanceHelper.Store
        /// </summary>
        /// <param name="activity"></param>
        /// <param name="identity"></param>
        /// <param name="extensibleArguments">Optional if reloading the workflow. Only required on inital trigger. KVP of argument name and value.</param>
        /// <remarks>DO NOT get the ID of the workflow instance before it's used otherwise on unpersisted workflows they will not work.</remarks>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public ApplicationHelper(Activity activity, WorkflowIdentity identity, Dictionary<string, object> extensibleArguments)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (PersistanceHelper.Store == null)
                throw new InvalidOperationException("PersistanceHelper.Store is not set. ");
            
            //Default to false
            UnloadOnIdle = false;

            if (extensibleArguments == null)
            {
                //This should only be used when the workflow is reconstitued.
                _application = new WorkflowApplication(activity, identity);
            }
            else
            {
                _application = new WorkflowApplication(
                    activity,
                    extensibleArguments,
                    identity);
            }

            //Owner of the workflow activity
            Dictionary<XName, object> wfScope = new Dictionary<XName, object>
            {
                 { 
                    PersistanceHelper.WorkflowHostTypePropertyName,
                    PersistanceHelper.HostTypeName
                }
            };

            _application.InstanceStore = PersistanceHelper.Store;
            _application.OnUnhandledException = UnhandledExceptionEvent;
            _application.PersistableIdle = AbleToPersistEvent;
            _application.Completed = CompletedEvent;
            _application.Unloaded = UnloadedEvent;
            _application.Aborted = AbortedEvent;
            _application.Idle = IdleEvent;
            
            // Add the WorkflowHostType value to workflow application so that it stores this data in the instance store when persisted
            _application.AddInitialInstanceValues(wfScope);            
        }

        /// <summary>
        /// This should only be called when restoring an existing workflow. This is a short blocking operation. It only waits a short amount of time. It if exits too early the workflow does not come back.
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="System.Runtime.DurableInstancing.InstanceLockedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public void ReloadAndRun(Guid id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            _log.Info("Attempting to readload activity. Id=" + id.ToString());

            _application.Load(id);
            _application.Run(_timeOut);

            ActivityStartedEvent?.Invoke(id, _application.WorkflowDefinition);

            //Give it time to startup before exiting.
            _reloadWaitHandler.WaitOne(new TimeSpan(0, 0, 5));
        }
        
        /// <summary>
        /// Reloads the workflow starting from a bookmark.
        /// </summary>
        /// <param name="id">Workflow Id</param>
        /// <param name="bookmarkName">The name of the bookmark to be resumed.</param>
        /// <param name="value">An object passed as a parameter to the method that is invoked when the bookmark resumes.</param>
        /// <returns>NotFound | NotReady | Success</returns>
        /// <exception cref="System.Runtime.DurableInstancing.InstanceLockedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public BookmarkResumptionResult ReloadAndRun(Guid id, string bookmarkName, object value)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (string.IsNullOrEmpty(bookmarkName))
                throw new ArgumentNullException(nameof(bookmarkName));

            _log.Info("Attempting to readload bookmark=" + bookmarkName);


            ActivityStartedEvent?.Invoke(id, _application.WorkflowDefinition);
            _application.Run(_timeOut);

            return _application.ResumeBookmark(bookmarkName, value, _timeOut);
        }

        /// <summary>
        /// Runs the workflow.
        /// </summary>
        public void Run()
        {
            _application.Run(_timeOut);

            ActivityStartedEvent?.Invoke(_application.Id, _application.WorkflowDefinition);
        }

        private void IdleEvent(WorkflowApplicationIdleEventArgs args)
        {
            _log.Debug($"Idle InstanceId={args.InstanceId.ToString()} Unloading...");
            
            if (UnloadOnIdle)
                _application.Unload();
            _reloadWaitHandler.Set();                        
        }
        private void AbortedEvent(WorkflowApplicationAbortedEventArgs args)
        {
            _reloadWaitHandler.Set();
            _log.Error("Aborted InstanceId=" + args.InstanceId.ToString(), args.Reason);
            ActivityAbortedEvent?.Invoke(args);
        }
        private void UnloadedEvent(WorkflowApplicationEventArgs args)
        {
            _reloadWaitHandler.Set();
            _log.Debug("Unloaded InstanceId=" + args.InstanceId.ToString());
        }

        private void CompletedEvent(WorkflowApplicationCompletedEventArgs args)
        {
            _reloadWaitHandler.Set();
            _log.Debug("It's now completed InstanceId=" + args.InstanceId.ToString());
            
            //Raise the event if it has been subscribed
            ActivityCompletedEvent?.Invoke(args);
        }

        private PersistableIdleAction AbleToPersistEvent(WorkflowApplicationIdleEventArgs args)
        {
            _reloadWaitHandler.Set();
            _log.Debug("It's able to be persisted InstanceId=" + args.InstanceId.ToString());

            ActivityWillBePersistedEvent?.Invoke(args);

            //As soon as it can persist do this incase the application gets terminated.
            return PersistableIdleAction.Persist;
        }

        private UnhandledExceptionAction UnhandledExceptionEvent(WorkflowApplicationUnhandledExceptionEventArgs args)
        {
            _reloadWaitHandler.Set();            
            _log.Error("An exception occured InstanceId=" + args.InstanceId.ToString() + " Activity=\"" + args.ExceptionSource.DisplayName +"\"", args.UnhandledException);
            ActivityUnhandledExceptionEvent?.Invoke(args);
            //Abort so the workflow does not stop. This will allow it to be persisted and become idle.
            return UnhandledExceptionAction.Abort;
        }

        public void Dispose()
        {
            _log.Debug("Disposed Application Helper");
        }
    }
}
