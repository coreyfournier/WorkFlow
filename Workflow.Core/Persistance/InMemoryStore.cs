using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Workflow.Core.Persistance.XmlPropertyBag;

namespace Workflow.Core.Persistance
{
    /// <summary>
    /// In memory store adapted from: https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/how-to-create-a-custom-instance-store
    /// This allows persistance to occur for testing. It has not been proven to work with delays.
    /// </summary>
    public class InMemoryStore : InstanceStore
    {
        Guid ownerInstanceID;
        AutoResetEvent _saveWaitHandler = new AutoResetEvent(false);
        NetDataContractSerializer serializer = new NetDataContractSerializer();

        /// <summary>
        /// Holds the instance information.
        /// </summary>
        Dictionary<string, IDictionary<XName, InstanceValue>> _fileStorage = new Dictionary<string, IDictionary<XName, InstanceValue>>();
        public InMemoryStore() : this(Guid.NewGuid())
        {

        }

        public InMemoryStore(Guid id)
        {
            ownerInstanceID = id;
        }

        //Synchronous version of the Begin/EndTryCommand functions  
        protected override bool TryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout)
        {
            return EndTryCommand(BeginTryCommand(context, command, timeout, null, null));
        }

        //The persistence engine will send a variety of commands to the configured InstanceStore,  
        //such as CreateWorkflowOwnerCommand, SaveWorkflowCommand, and LoadWorkflowCommand.  
        //This method is where we will handle those commands  
        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            IDictionary<XName, InstanceValue> data = null;

            //The CreateWorkflowOwner command instructs the instance store to create a new instance owner bound to the instanace handle  
            if (command is CreateWorkflowOwnerCommand)
            {
                context.BindInstanceOwner(ownerInstanceID, ownerInstanceID);
            }
            //The SaveWorkflow command instructs the instance store to modify the instance bound to the instance handle or an instance key  
            else if (command is SaveWorkflowCommand)
            {
                SaveWorkflowCommand saveCommand = (SaveWorkflowCommand)command;
                data = saveCommand.InstanceData;
                
                
                Save(data);
                //_saveWaitHandler.WaitOne(new TimeSpan(0, 0, 1));
                
            }
            //The LoadWorkflow command instructs the instance store to lock and load the instance bound to the identifier in the instance handle  
            else if (command is LoadWorkflowCommand)
            {
                string fileName = GetFileName(this.ownerInstanceID);

                data = LoadInstanceDataFromFile(fileName);
                var nonWriteOnly = data.Where(kvp => (kvp.Value.Options & InstanceValueOptions.WriteOnly) != InstanceValueOptions.WriteOnly).ToDictionary(k=>k.Key,v=>v.Value);


                //load the data into the persistence Context  
                context.LoadedInstance(InstanceState.Initialized, nonWriteOnly, null, null, null);

            }
            else if(command.Name.LocalName == "CreateWorkflowOwner")
            {
                //do nothing
            }
            var result = new CompletedAsyncResult<bool>(true, callback, state);

            return result;
        }

        private string GetFileName(Guid ownerInstanceID)
        {
            return ownerInstanceID.ToString();
        }

        protected override bool EndTryCommand(IAsyncResult result)
        {
            return CompletedAsyncResult<bool>.End(result);
        }

        //Reads data from xml file and creates a dictionary based off of that.  
        IDictionary<XName, InstanceValue> LoadInstanceDataFromFile(string fileName)
        {
            return _fileStorage[fileName];
        }

        //Saves the persistence data to an xml file.  
        void Save(IDictionary<XName, InstanceValue> instanceData)
        {
            string fileName = GetFileName(this.ownerInstanceID);

            SerializePropertyBag(instanceData, new InstanceEncodingOption() { });

            if (_fileStorage.ContainsKey(fileName))
                _fileStorage[fileName] = instanceData;
            else
                _fileStorage.Add(fileName, instanceData);
        }

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="propertyBag"></param>
        void SerializePropertyBag(Stream stream, Dictionary<XName, object> propertyBag)
        {
            using (GZipStream gzip = new GZipStream(stream, CompressionLevel.Fastest, true))
            {
                DefaultSerializePropertyBag(gzip, propertyBag);
            }
        }

        /// <summary>
        /// /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="propertyBag"></param>
        void DefaultSerializePropertyBag(Stream stream, Dictionary<XName, object> propertyBag)
        {
            using (XmlDictionaryWriter dictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
            {
                dictionaryWriter.WriteStartElement("Properties");

                foreach (KeyValuePair<XName, object> property in propertyBag)
                {

                    dictionaryWriter.WriteStartElement("Property");

                    try
                    {

                        serializer.WriteObject(dictionaryWriter, property);
                    }
                    catch (System.Runtime.Serialization.InvalidDataContractException ex)
                    {
                        throw new SerializationFailedException("This can be caused by many issues. Ensure you have a default constructor and check the inner exception for more info. See also: https://stackoverflow.com/questions/10077121/datacontract-exception-cannot-be-serialized", ex);
                    }

                    dictionaryWriter.WriteEndElement();
                }

                dictionaryWriter.WriteEndElement();
            }
        }

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        static class SqlWorkflowInstanceStoreConstants
        {
            public static readonly TimeSpan MaxHostLockRenewalPulseInterval = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan DefaultTaskTimeout = TimeSpan.FromSeconds(30);
            public static readonly TimeSpan LockOwnerTimeoutBuffer = TimeSpan.FromSeconds(30);
            public static readonly XNamespace WorkflowNamespace = XNamespace.Get("urn:schemas-microsoft-com:System.Activities/4.0/properties");
            public static readonly XNamespace DurableInstancingNamespace = XNamespace.Get("urn:schemas-microsoft-com:System.ServiceModel.Activities.DurableInstancing/SqlWorkflowInstanceStore");
            public static readonly XName LastUpdatePropertyName = WorkflowNamespace.GetName("LastUpdate");
            public static readonly XName PendingTimerExpirationPropertyName = WorkflowNamespace.GetName("TimerExpirationTime");
            public static readonly XName BinaryBlockingBookmarksPropertyName = WorkflowNamespace.GetName("Bookmarks");
            public static readonly XName StatusPropertyName = WorkflowNamespace.GetName("Status");
            public static readonly string MachineName = Environment.MachineName;
            public const string DefaultSchema = "[System.Activities.DurableInstancing]";
            public const InstanceCompletionAction DefaultInstanceCompletionAction = InstanceCompletionAction.DeleteAll;
            public const InstanceEncodingOption DefaultInstanceEncodingOption = InstanceEncodingOption.GZip;
            public const InstanceLockedExceptionAction DefaultInstanceLockedExceptionAction = InstanceLockedExceptionAction.NoRetry;
            public const string ExecutingStatusPropertyValue = "Executing";
            public const int DefaultStringBuilderCapacity = 512;
            public const int MaximumStringLengthSupported = 450;
            public const int MaximumPropertiesPerPromotion = 32;
        };

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="encodingOption"></param>
        /// <returns></returns>
        ArraySegment<byte>[] SerializePropertyBag(IDictionary<XName, InstanceValue> properties, InstanceEncodingOption encodingOption)
        {
            ArraySegment<byte>[] dataArrays = new ArraySegment<byte>[4];
            NetDataContractSerializer serializer = new NetDataContractSerializer();

            if (properties.Count > 0)
            {
                XmlPropertyBag primitiveProperties = new XmlPropertyBag();
                XmlPropertyBag primitiveWriteOnlyProperties = new XmlPropertyBag();
                Dictionary<XName, object> complexProperties = new Dictionary<XName, object>();
                Dictionary<XName, object> complexWriteOnlyProperties = new Dictionary<XName, object>();
                Dictionary<XName, object>[] propertyBags = new Dictionary<XName, object>[] { primitiveProperties, complexProperties,
                    primitiveWriteOnlyProperties, complexWriteOnlyProperties };

                foreach (KeyValuePair<XName, InstanceValue> property in properties)
                {
                    bool isComplex = (XmlPropertyBag.GetPrimitiveType(property.Value.Value) == PrimitiveType.Unavailable);
                    bool isWriteOnly = (property.Value.Options & InstanceValueOptions.WriteOnly) == InstanceValueOptions.WriteOnly;
                    int index = (isWriteOnly ? 2 : 0) + (isComplex ? 1 : 0);
                    propertyBags[index].Add(property.Key, property.Value.Value);
                }

                // Remove the properties that are already stored as individual columns from the serialized blob
                primitiveWriteOnlyProperties.Remove(SqlWorkflowInstanceStoreConstants.StatusPropertyName);
                primitiveWriteOnlyProperties.Remove(SqlWorkflowInstanceStoreConstants.LastUpdatePropertyName);
                primitiveWriteOnlyProperties.Remove(SqlWorkflowInstanceStoreConstants.PendingTimerExpirationPropertyName);

                complexWriteOnlyProperties.Remove(SqlWorkflowInstanceStoreConstants.BinaryBlockingBookmarksPropertyName);

                for (int i = 0; i < propertyBags.Length; i++)
                {
                    if (propertyBags[i].Count > 0)
                    {
                        if (propertyBags[i] is XmlPropertyBag)
                        {
                            dataArrays[i] = SerializeValue(propertyBags[i]);
                        }
                        else
                        {
                            dataArrays[i] = SerializePropertyBag(propertyBags[i]);
                        }
                    }
                }
            }

            return dataArrays;
        }

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        ArraySegment<byte> SerializePropertyBag(Dictionary<XName, object> value)
        {
            using (MemoryStream memoryStream = new MemoryStream(4096))
            {
                this.SerializePropertyBag(memoryStream, value);
                
                return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, Convert.ToInt32(memoryStream.Length));
            }
        }

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        ArraySegment<byte> SerializeValue(object value)
        {
            using (MemoryStream memoryStream = new MemoryStream(4096))
            {
                this.SerializeValue(memoryStream, value);

                System.Text.ASCIIEncoding myEnc = new System.Text.ASCIIEncoding();
                System.Diagnostics.Debug.WriteLine(myEnc.GetString(memoryStream.ToArray()));

                return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, Convert.ToInt32(memoryStream.Length));
            }
        }

        /// <summary>
        /// Serialization routines used only to mimic SQL persistance to ensure there are no errors during unit testing of the in memory store.
        /// Obtained from https://referencesource.microsoft.com/#System.Activities.DurableInstancing/System/Activities/DurableInstancing/SerializationUtilities.cs,6929c9045420df05
        /// </summary>
        protected virtual void SerializeValue(Stream stream, object value)
        {
            using (XmlDictionaryWriter dictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream, null, null, false))
            {
                this.serializer.WriteObject(dictionaryWriter, value);
            }
        }
    }    
}
