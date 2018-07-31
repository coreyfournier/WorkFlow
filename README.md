# Workflow
Windows Workflow Wrappers
## What is this?
The wrappers serve as a means to easily hide the complexity of Windows Workflow 4.5. As I learned the hard way it is not easy to turn on right out of the box. 
* To use features such as persistance inside a service requires to you handle resuming the workflows. This is now acomplished via a single function call ReconstituteRunnableInstances.
* Out of the box there is no way to do unit testing. I have added an In Memory Persistance Store to allow unit testing with no depencency of a database. ***I have not been able to support bookmarking yet***.
* My personal needs of a platform that manages work going in and being distributed and resumable upon error (Orchestration).

## Overview
The project is broken into 2 major and 2 minor parts. 
1. Workflow.Core
The abstraction is here. You can start a workflow, and resume it here. 
2. Workflow.Orchestration
The project that handles Events and Notifications with MSMQ. Workflows are expected to accept DataEventArgs
3. Workflow.ScratchPad
Simple program to test execution of the code in a more real world setting. 
4. Workflow.UnitTests
	Unit tests expected behavior of the code.


## Prerequisites
If you are unable to build the project be sure to install the Office/SharePoint Development tools. See this: http://code-coverage.net/windows-workflow-foundation-build-errors-in-visual-studio-2017/
### Sql database for the persistance store
	https://docs.microsoft.com/en-us/dotnet/framework/windows-workflow-foundation/how-to-enable-sql-persistence-for-workflows-and-workflow-services
	C:\Windows\Microsoft.NET\Framework\v4.0.30319\SQL\en
	SqlWorkflowInstanceStoreSchema.sql
	SqlWorkflowInstanceStoreLogic.sql
### MSMQ or Equivalent queue if using Orchestration
It is expected that using the orchestration you have prior knowledge of WCF and hosting services with in a windows service. 2 Queues must specified in your app.confg with the names: ObservedChangesEndPoint and SubscriberEndPoint
#### Client Section Example
```
	<endpoint address="net.msmq://localhost/private/subscriberqueue" binding="netMsmqBinding" bindingConfiguration="msmqNoSecurity" contract="Workflow.Orchestration.Queues.ISubscriberQueue" name="SubscriberEndPoint" />
	<endpoint address="net.msmq://localhost/private/observedqueue" binding="netMsmqBinding" bindingConfiguration="msmqNoSecurity" contract="Workflow.Orchestration.Queues.IObservedChangeQueue" name="ObservedChangesEndPoint" />
```
#### Services Section Example
```
<service behaviorConfiguration="QueueServiceBehavior" name="***Name of the service hosting***">
        <endpoint address="debug_subscriberqueue" binding="netMsmqBinding" bindingConfiguration="msmqNoSecurity" contract="Workflow.Orchestration.Queues.ISubscriberQueue" name="SubscriberEndPoint" />
        <endpoint address="debug_observedqueue" binding="netMsmqBinding" bindingConfiguration="msmqNoSecurity" contract="Workflow.Orchestration.Queues.IObservedChangeQueue" name="ObservedChangesEndPoint" />
        <host>
          <baseAddresses>
            <add baseAddress="net.msmq://localhost/private" />
          </baseAddresses>
        </host>
      </service>
```
The above example expects the actual queue name to be observedqueue and subscriberqueue. This can be changed to anything.
## Logging
Logging has been added via Log4net.

## Usage
### Example Usage for starting and resuming activities
```
    //Store connection string
    PersistanceHelper.Store = new SqlWorkflowInstanceStore(@"Data Source=LAFDEV3\SQL2014;Initial Catalog=Corey-WfPersistenceStore;Integrated Security=True;Async=true")
    {
        InstanceCompletionAction = InstanceCompletionAction.DeleteNothing,
        RunnableInstancesDetectionPeriod = new TimeSpan(0, 0, 5),
        InstanceLockedExceptionAction = InstanceLockedExceptionAction.AggressiveRetry,
    };

    //Reload any instances that persisted since you last ran the application.
    PersistanceHelper.ReconstituteRunnableInstances();

    ExampleActivity activity = new ExampleActivity();
	
	//Defines the application completely so it can be resumed.
    ApplicationHelper app = new ApplicationHelper(
        activity,
        activity.GetIdentity(), 
        new Core.Models.DataEventArgs() {
            EventDate  = DateTime.Now,
            SourceApiName = "Program-Application",
            SourceFriendlyName = "Program Application",
            SourceTransactionId = Guid.NewGuid().ToString(),
            SourceType = new Core.Models.TypeWrapper(typeof(ExampleActivity))                    
        });

    app.Run();

    //Wait here so the application can run in the background
    Console.ReadLine();
```
### Example using Orchestration
See above prerequisites for more information on the App.config settings.
```
public class WorkflowHostedListner : Workflow.Orchestration.WcfHostedListners 
{
	public WorkflowHostedListner()
	{
		
	}

	protected Subscriber[] GetSubscriberForEvent(string eventApiName)
	{
		var subscribers = new Subscriber[] {
                //This subscriber is set to run when the pfa gets terminated.
                new Subscriber("Name of Subscriber",
                "Event-Name", 
                typeof(/*Activity class*/))
            };

		return subscribers.Where(x=> x.EventToListenToName == eventApiName && x.IsEnabled).ToArray();
	}
}
```

* Push a new change into the Object Change Queue using ChangeObserved.
* The change gets picked up and Subscribers is queried find if there are any subscribers for the event.
* When subscribers are found the Workflow associated to the subscriber is executed with the data passed in.
