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
        private T _data;
        public DataEventArgs(T data)
        {
            _data = data;
            base.Data = Seralizer.ObjectToXmlString(data);
        }

        public DataEventArgs() : base() { }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException"></exception>
        public override T1 DataToType<T1>()
        {
            if (typeof(T1).FullName != typeof(T).FullName)
                throw new InvalidCastException("The templated type of the class is not the same as the type of DataToType");

            return _data as T1;
        }
    }
}
