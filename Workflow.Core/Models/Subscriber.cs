using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    [Serializable]
    [DataContract(Name = "Subscriber", Namespace = "http://schemas.datacontract.org/2004/07/Workflow.Core.Models")]
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
        /// <param name="configuration">Json string configuration</param>
        public Subscriber(string subscriberName, string eventToListenTo, Type workflow, string description = "", bool enabled = true, string configuration = "")
        {
            IsEnabled = enabled;
            Description = description;
            SubscriberName = subscriberName;
            EventToListenToName = eventToListenTo;
            WorkFlowType = new TypeWrapper(workflow);
        }

        /// <summary>
        /// The name of the subscriber
        /// </summary>
        [DataMember]
        public virtual string SubscriberName { get; set; }

        /// <summary>
        /// Any description to identify what this is for.
        /// </summary>
        [DataMember]
        public virtual string Description { get; set; }
 
        [DataMember]
        public TypeWrapper WorkFlowType { get; set; }

        /// <summary>
        /// Event to listen to
        /// </summary>
        [DataMember]
        public virtual string EventToListenToName { get; set; }

        /// <summary>
        /// Allows the subscription to be enabled or disabled.
        /// </summary>
        [DataMember]
        public virtual bool IsEnabled { get; set; }

        /// <summary>
        /// Json string containing a configuration that will get passed to the workflow to change behavior. 
        /// Examples are Urls, emails, directories etc..
        /// If this is a type, the Configuration Type should be set, so validation can be done.
        /// </summary>
        [DataMember]
        public virtual string Configuration { get; set; }

        /// <summary>
        /// Data type for the configuration. This can be used for validation
        /// </summary>
        [DataMember]
        public TypeWrapper ConfigurationType { get; set; }

        /// <summary>
        /// Gets the configuration as the specified type using serilization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Null if Configuration is null or empty</returns>
        public virtual T ConfigurationToType<T>()
        {
            if (string.IsNullOrEmpty(Configuration))
                return default(T);
            else
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Configuration);
        }

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
