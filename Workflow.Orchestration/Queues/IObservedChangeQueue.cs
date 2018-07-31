using System;
using System.ServiceModel;
using Workflow.Core.Models;

namespace Workflow.Orchestration.Queues
{
    [ServiceContract]
    public interface IObservedChangeQueue : IDisposable
    {
        /// <summary>
        /// Called to send and receive a new event
        /// </summary>
        /// <param name="event"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [OperationContract(IsOneWay = true)]
        void ChangeObserved(DataEventArgs @event);
    }
}
