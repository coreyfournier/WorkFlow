using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflow.Core.Utilities
{
    public class PropertyAssistant
    {
        public const string TransactionIdName = "TransactionId";
        /// <summary>
        /// Gets the transaction id from the DataEventArgs argument in the main activity. This then sets the TransactionId for log4net in the thread context so it can be used in logging to correlate events.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static void SetTransactionId(ActivityContext context)
        {
            Models.DataEventArgs args = null;
            foreach (PropertyDescriptor prop in context.DataContext.GetProperties())
            {
                if (prop.Name == ApplicationHelper.DataEventArgumentName)
                {
                    args = prop.GetValue(context.DataContext) as Models.DataEventArgs;
                    break;
                }
            }

            
            if (args == null)
                log4net.LogicalThreadContext.Properties[TransactionIdName] = null;
            else
                log4net.LogicalThreadContext.Properties[TransactionIdName] = args.SourceTransactionId;
        }
    }
}
