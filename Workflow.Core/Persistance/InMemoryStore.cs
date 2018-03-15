using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.DurableInstancing;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

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

            if (_fileStorage.ContainsKey(fileName))
                _fileStorage[fileName] = instanceData;
            else
                _fileStorage.Add(fileName, instanceData);
            
        }
    }
}
