using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core
{
    public static class Seralizer
    {
        /// <summary>
        /// Takes the object passed in and converts it to an XML string.
        /// </summary>
        /// <param name="inObject"></param>
        /// <returns></returns>
        public static string ObjectToXmlString<T>(T inObject)
        {
            using (MemoryStream mem = new MemoryStream())
            {
                System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(inObject.GetType());
                System.Text.ASCIIEncoding myEnc = new System.Text.ASCIIEncoding();

                ser.Serialize(mem, inObject);

                return myEnc.GetString(mem.ToArray());
            }
        }

        /// <summary>
        /// Converts the XML into an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configXML"></param>
        /// <returns></returns>
        public static T StringToObject<T>(string configXML)
        {
            T outObject;
            using (TextReader mem = new StringReader(configXML))
            {
                System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

                outObject = (T)ser.Deserialize(mem);
                mem.Dispose();

                return outObject;
            }
        }
    }
}
