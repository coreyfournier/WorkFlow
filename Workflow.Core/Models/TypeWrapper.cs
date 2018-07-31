using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Models
{
    /// <summary>
    /// Allows the type information to be seralized. Stores the full name of the type and the assembly they belong to.
    /// </summary>
    [Serializable]
    public class TypeWrapper
    {
        public TypeWrapper()
        {

        }

        public TypeWrapper(Type type)
        {
            AssemblyName = type.Assembly.FullName;
            FullName = type.FullName;
        }

        /// <summary>
        /// Full name of the type. 
        /// Ex: Workflow.Orchestration.TestActivity
        /// </summary>
        public virtual string FullName { get; set; }

        /// <summary>
        /// Full name of the assembly.
        /// EX: "Workflow.Orchestration, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        /// </summary>
        public virtual string AssemblyName { get; set; }

        /// <summary>
        /// Takes the full name and the assembly and get the Type.
        /// </summary>
        /// <exception cref="System.TypeLoadException"></exception>
        public Type ToType()
        { 
            if (string.IsNullOrEmpty(FullName))
                throw new ArgumentNullException(nameof(FullName));

            var assembly = Extensions.GetAssemblyByName(AssemblyName);
            if (assembly == null)
                throw new System.TypeLoadException("Unable to find '"+ AssemblyName + "' in the App Domain");

            return assembly.GetType(FullName, true, true);
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
