using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Workflow.Core.Models;

namespace Workflow.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Converts the specific Activity type to the base activity type instance for execution.
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.TypeLoadException"></exception>
        public static Activity GetActivity(this Subscriber subscriber)
        {
            if (subscriber.WorkFlowName == null)
                throw new ArgumentNullException(nameof(subscriber.WorkFlowName));       

            return (Activity)Activator.CreateInstance(subscriber.WorkFlowType.ToType());
        }

        /// <summary>
        /// Converts the specific Activity type to the base activity type instance for execution.
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Activity GetActivity(this InstanceView instance)
        {
            if (instance.IdentityName == null)
                throw new ArgumentNullException("instance.IdentityName");

            //This may need to be revisted depending on where the workflows are.
            var assembly = GetAssemblyByName(instance.IdentityPackage);            

            return (Activity)assembly.CreateInstance(instance.IdentityName);
        }

        /// <summary>
        /// Gets the version identity for the workflow being used. Version is hard coded to 1.
        /// </summary>
        /// <returns></returns>
        public static WorkflowIdentity GetIdentity(this Subscriber subscriber)
        {
            return GetIdentity(subscriber, new Version(1, 0, 0, 0));
        }

        /// <summary>
        /// Gets the version identity for the workflow being used. 
        /// </summary>
        /// <param name="workflowVersion"></param>
        /// <returns></returns>
        public static WorkflowIdentity GetIdentity(this Subscriber subscriber, Version workflowVersion)
        {
            if (string.IsNullOrEmpty(subscriber.WorkFlowType.AssemblyName))
                throw new ArgumentNullException(nameof(subscriber.WorkFlowType.AssemblyName));

            return new WorkflowIdentity(
                subscriber.WorkFlowName,
                workflowVersion,
                subscriber.WorkFlowType.AssemblyName);
        }

        /// <summary>
        /// Gets the version identity for the workflow being used. Only should only be used with unit testing as it provides no help. Version is hard coded to 1.
        /// </summary>
        /// <returns></returns>
        public static WorkflowIdentity GetIdentity(this Activity activity)
        {
            return GetIdentity(activity, new Version(1, 0, 0, 0));
        }

        /// <summary>
        /// Gets the version identity for the workflow being used. Only should only be used with unit testing as it provides no help. 
        /// </summary>
        /// <returns></returns>
        public static WorkflowIdentity GetIdentity(this Activity activity, Version workflowVersion)
        {
            return new WorkflowIdentity(
                activity.DisplayName,
                //Version is hard coded. This will need to change if you have version tracking.
                workflowVersion,
                "UnitTestApplication");
        }

        public static Assembly GetAssemblyByName(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().
                   SingleOrDefault(assembly => assembly.FullName == fullName);
        }

        /// <summary>
        /// Gets the identity from the reconstituted workflow
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static WorkflowIdentity GetIdentity(this Models.InstanceView instance)
        {
            return new WorkflowIdentity(
                instance.IdentityName, 
                new Version((int)instance.Major, (int)instance.Minor, (int)instance.Build, (int)instance.Revision), 
                instance.IdentityPackage);
        }

        /// <summary>
        /// Takes an int array and converts it to CSV.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string ToCSV(this Type[] array)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].FullName);
                if (i + 1 < array.Length)
                    sb.Append(",");
            }
            return sb.ToString();
        }
    }
}
