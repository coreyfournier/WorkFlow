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
        /// Constructor that allows the data and seralization to get set upon creation
        /// </summary>
        /// <param name="data"></param>
        /// <param name="seralization"></param>
        public DataEventArgs(Object data, SeralizeAs seralization)
        {
            SetData(data, seralization);
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
        /// Data as a string. Seralization set by 'Seralization' To get the native type use DataToType(). It must be a string so WCF does not complain about it's inability to find the assembly. Only the source and destination needs to know how to use the XML.
        /// </summary>
        [DataMember]
        public string Data { get { return _data; } set { _data = value; } }

        /// <summary>
        /// Sets how the data is seralized
        /// </summary>
        [DataMember]
        public SeralizeAs Seralization { get; set; }

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
            if (Seralization == SeralizeAs.Xml)
                return Seralizer.StringToObject<T>(Data);
            else if (Seralization == SeralizeAs.Json)
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Data);
            else
                throw new InvalidOperationException("Unknown encoding type selected");
        }

        /// <summary>
        /// Takes the data and seralizes it and sets the seralization
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="seralization"></param>
        public void SetData<T>(T data, SeralizeAs seralization) where T : class
        {
            Data = Encode(data, seralization);
            Seralization = seralization;
        }

        /// <summary>
        /// Centralized location for seralization, but does not set any members on the class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="seralization"></param>
        /// <returns></returns>
        public static string Encode<T>(T data, SeralizeAs seralization) where T: class
        {
            if (seralization == SeralizeAs.Xml)
                return Seralizer.ObjectToXmlString<T>(data);
            else if (seralization == SeralizeAs.Json)
                return Newtonsoft.Json.JsonConvert.SerializeObject(data);
            else
                throw new InvalidOperationException("Unknown seralization selected");
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

    /// <summary>
    /// Specifies how the data is encoded
    /// </summary>
    [Flags]
    public enum SeralizeAs 
    {        
        Xml = 0,
        /// <summary>
        /// Use Json to reduce the size of the seralized data.
        /// </summary>
        Json = 1
    }
}
