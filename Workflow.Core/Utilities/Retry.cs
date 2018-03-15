using log4net;
using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Workflow.Core.Utilities
{
    /// <summary>
    /// Allows an activity to be retried after an exception.
    /// This will not work correctly if it is inside of a loop as the variables don't persist and will cause this to go into an infinate loop.
    /// Derrived from:
    /// https://www.neovolve.com/2011/02/21/wf-retry-activity/
    /// https://blogs.msdn.microsoft.com/rjacobs/2011/08/14/how-to-create-a-custom-activity-designer-with-windows-workflow-foundation-wf4/
    /// http://www.dotnetconsult.co.uk/weblog2/2010/02/09/NativeActivityNdashATrickyBeast.aspx
    /// Many changes were necessary to get it working.
    /// </summary>
    public class Retry : NativeActivity, IActivityTemplateFactory
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(Retry));
        private static readonly TimeSpan _defaultRetryInterval = new TimeSpan(0, 0, 0, 30);
        private readonly Variable<Int32> _attemptCount = new Variable<Int32>("AttemptCount", 0);
        private readonly Variable<TimeSpan> _delayDuration = new Variable<TimeSpan>("DelayDuration", _defaultRetryInterval);
        private readonly Delay _internalDelay;
        private static TimeSpan? _delayOverrideForUnitTests = null;

        /// <summary>
        /// Allows an override to be set so unit testing does not wait around to complete a run. This will apply to all retries while the application is still running.
        /// </summary>
        public static TimeSpan OverrideForUnitTests {
            set
            {
                _delayOverrideForUnitTests = value;
                _log.Info("Delay override has been set for all Retries to " + value.TotalSeconds + " seconds. This is intended to be only done for unit testing.");
            }            
        }

        [Browsable(false)]
        public ActivityAction Body
        {
            get;
            set;
        }

        [DefaultValue(new Type[] { typeof(TimeoutException), typeof(System.Net.WebException) })]
        [RequiredArgument]
        public InArgument<Type[]> ExceptionType
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<Int32> MaxAttempts
        {
            get;
            set;
        }

        [RequiredArgument]
        public InArgument<TimeSpan> RetryInterval
        {
            get;
            set;
        }

        public Retry()
        {
            _internalDelay = new Delay
            {
                Duration = new InArgument<TimeSpan>(_delayDuration)
            };
            //Body must be set to an activity or you get a null reference exception
            Body = new ActivityAction() { DisplayName = "Activity to retry on error" };
            MaxAttempts = 5;
            ExceptionType = new Type[] { typeof(TimeoutException), typeof(System.Net.WebException) };
            RetryInterval = _defaultRetryInterval;
        }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            metadata.AddDelegate(Body);
            //Add the delay in so it can try again and persist.
            metadata.AddImplementationChild(_internalDelay);
            metadata.AddImplementationVariable(_attemptCount);
            metadata.AddImplementationVariable(_delayDuration);

            RuntimeArgument maxAttemptsArgument = new RuntimeArgument("MaxAttempts", typeof(Int32), ArgumentDirection.In, true);
            RuntimeArgument retryIntervalArgument = new RuntimeArgument("RetryInterval", typeof(TimeSpan), ArgumentDirection.In, true);
            RuntimeArgument exceptionTypeArgument = new RuntimeArgument("ExceptionType", typeof(Type[]), ArgumentDirection.In, true);

            metadata.Bind(MaxAttempts, maxAttemptsArgument);
            metadata.Bind(RetryInterval, retryIntervalArgument);
            metadata.Bind(ExceptionType, exceptionTypeArgument);

            metadata.SetArgumentsCollection(new Collection<RuntimeArgument> { maxAttemptsArgument, retryIntervalArgument, exceptionTypeArgument });
        }

        protected override void Execute(NativeActivityContext context)
        {
            PropertyAssistant.SetTransactionId(context);
            ExecuteAttempt(context);
        }

        private static Boolean ShouldRetryAction(Type[] exceptionType, Exception thrownException)
        {
            if (exceptionType == null)
                return false;
            else
            {
                foreach (var exeption in exceptionType)
                {
                    if (exeption.IsAssignableFrom(thrownException.GetType()))
                        return true;
                }
            }

            return false;
        }

        private void ActionFailed(NativeActivityFaultContext faultContext, Exception propagatedexception, ActivityInstance propagatedfrom)
        {
            Int32 currentAttemptCount = _attemptCount.Get(faultContext);
            Int32 maxAttempts = MaxAttempts.Get(faultContext);
            Type[] exceptionType = ExceptionType.Get(faultContext);

            //Increment and track the count
            currentAttemptCount++;
            _attemptCount.Set(faultContext, currentAttemptCount);            

            if (currentAttemptCount >= maxAttempts)
            {
                // There are no further attempts to make
                return;
            }

            if (ShouldRetryAction(exceptionType, propagatedexception) == false)
            {
                _log.Error("Will only retry exception of type '" + exceptionType.ToCSV() + "'. Unhandled type of '" + propagatedexception.GetType().FullName + "' was found.", propagatedexception);
                return;
            }

            faultContext.CancelChild(propagatedfrom);
            faultContext.HandleFault();

            TimeSpan retryInterval = _delayOverrideForUnitTests == null ? RetryInterval.Get(faultContext) : _delayOverrideForUnitTests.Value;

            _log.Debug("Retrying in " + retryInterval.TotalSeconds + " seconds due to " + propagatedexception.GetType().FullName + ". " + currentAttemptCount + " of " + maxAttempts);

            if (retryInterval == TimeSpan.Zero)
            {
                ExecuteAttempt(faultContext);
            }
            else
            {                
                // We are going to wait before trying again
                _delayDuration.Set(faultContext, retryInterval);
                faultContext.ScheduleActivity(
                    _internalDelay, 
                    DelayCompleted);
            }
        }

        private void DelayCompleted(NativeActivityContext context, ActivityInstance completedinstance)
        {
            ExecuteAttempt(context);
        }

        private void ExecuteAttempt(NativeActivityContext context)
        {
            if (Body == null)
                return;

            context.ScheduleAction(Body, null, ActionFailed);
        }

        [DebuggerNonUserCode]
        public Activity Create(DependencyObject target)
        {
            return new Retry
            {
                Body = { Handler = new Sequence() }
            };
        }
    }
}
