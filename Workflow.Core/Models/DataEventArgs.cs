using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    /// <summary>
    /// Wrapper that contains all elements that define an event, it's data, and source.
    /// </summary>
    [Serializable, DataContract]
    public class DataEventArgs : EventArgs
    {
        private string _data;

        public DataEventArgs()
        {

        }

        /// <summary>
        /// Contains Full name of the source of the Data and which assembly it came from. This is mostly for debugging purposes.
        /// </summary>
        [DataMember]
        public TypeWrapper SourceType { get; set; }

        /// <summary>
        /// Name of the event that should never change.
        /// </summary>
        [DataMember]
        public string SourceApiName { get; set; }

        /// <summary>
        /// A friendly name of the Event
        /// </summary>
        [DataMember]
        public string SourceFriendlyName { get; set; }

        /// <summary>
        /// When did the event occur.
        /// </summary>
        [DataMember]
        public DateTime EventDate { get; set; }

        /// <summary>
        /// Data as an XML String. To get the native type use DataToType(). It must be a string so WCF does not complain about it's inability to find the assembly. Only the source and destination needs to know how to use the XML.
        /// </summary>
        [DataMember]
        public string Data { get { return _data; } set { _data = value; } }

        /// <summary>
        /// Optional. Id to identify the transaction
        /// </summary>
        [DataMember]
        public string SourceTransactionId { get; set; }

        /// <summary>
        /// Converts the Data XML to the native type. This was necessary because WCF wanted the exact type, but it's only know at the source and destination.
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <returns>Concrete type</returns>        
        public virtual T DataToType<T>() where T : class
        {
            return Seralizer.StringToObject<T>(Data);
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
