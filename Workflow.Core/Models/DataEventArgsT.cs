using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    /// <summary>
    /// Allows the data to be set to an explicit type with out having to be XML first. This is intended to only be used for testing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public class DataEventArgs<T> : DataEventArgs
    {
        public DataEventArgs(T data) : base(data, SeralizeAs.Xml)
        {
            
        }

        public DataEventArgs() : base() { }

        
    }
}
