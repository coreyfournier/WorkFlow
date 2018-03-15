using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    [Serializable]
    public class Subscriber
    {

        public Subscriber() { }

        /// <summary>
        /// All required parts of a subscriber. Defaults it to enabled.
        /// </summary>
        /// <param name="subscriberName"></param>
        /// <param name="eventToListenTo"></param>
        /// <param name="workflow"></param>
        /// <param name="description"></param>
        /// <param name="enabled"></param>
        public Subscriber(string subscriberName, string eventToListenTo, Type workflow, string description = "", bool enabled = true)
        {
            IsEnabled = enabled;
            Description = description;
            SubscriberName = subscriberName;
            EventToListenToName = eventToListenTo;
            WorkFlowName = workflow.FullName;
            WorkFlowType = new TypeWrapper(workflow);
        }

        /// <summary>
        /// The name of the subscriber
        /// </summary>
        public string SubscriberName { get; set; }

        /// <summary>
        /// Any description to identify what this is for.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Fully qualified workflow name
        /// </summary>
        public string WorkFlowName { get; set; }

        public TypeWrapper WorkFlowType { get; set; }

        /// <summary>
        /// Event to listen to
        /// </summary>
        public string EventToListenToName { get; set; }

        /// <summary>
        /// Allows the subscription to be enabled or disabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Converts the item to JSON.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
