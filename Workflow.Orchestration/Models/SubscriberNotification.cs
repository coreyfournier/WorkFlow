using System;
using Workflow.Core.Models;

namespace Workflow.Orchestration.Models
{
    /// <summary>
    /// Ties the subscriber to the event so they can receive the notification.
    /// </summary>
    [Serializable]
    public class SubscriberNotification
    {
        public DataEventArgs Event { get; set; }
        public DateTime CreatedDate { get; set; }
        public Subscriber Subscriber { get; set; }
        /// <summary>
        /// Converts the entire object to json.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
