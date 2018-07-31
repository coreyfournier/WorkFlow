using System.ServiceModel;
using Workflow.Core.Models;
using System;
using log4net;

namespace Workflow.Orchestration.Queues
{
    public class ObservedChangeQueue : ClientBase<IObservedChangeQueue>, IObservedChangeQueue
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ObservedChangeQueue));

        public ObservedChangeQueue()
            : base("ObservedChangesEndPoint")
        {

        }

        /// <summary>
        /// Called to send and receive a new event
        /// </summary>
        /// <param name="event"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [OperationBehavior(TransactionAutoComplete = true, TransactionScopeRequired = true)]
        public void ChangeObserved(DataEventArgs @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            if (string.IsNullOrEmpty(@event.SourceApiName))
                throw new ArgumentNullException(nameof(@event.SourceApiName));

            if (string.IsNullOrEmpty(@event.SourceFriendlyName))
                throw new ArgumentNullException(nameof(@event.SourceFriendlyName));

            if (@event.SourceType == null)
                throw new ArgumentNullException(nameof(@event.SourceType));

            _log.Debug("New event observed ApiName=" + @event.SourceApiName);

            try
            {
                this.Channel.ChangeObserved(@event);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw ex;
            }            
        }
    }
}
