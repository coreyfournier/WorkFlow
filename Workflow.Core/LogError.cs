using System;
using System.Activities;
using log4net;
using Workflow.Core.Utilities;

namespace Workflow.Core
{
    /// <summary>
    /// Workflow activity to log an error via log4net.
    /// </summary>
    public sealed class LogError : CodeActivity
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(LogError));

        // Define an activity input argument of type string
        public InArgument<string> Text { get; set; }

        public InArgument<Exception> Error { get; set; }


        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            PropertyAssistant.SetTransactionId(context);

            if(string.IsNullOrEmpty(context.GetValue(this.Text)))
                _log.Error(context.GetValue(this.Error));
            else
                _log.Error(context.GetValue(this.Text), context.GetValue(this.Error));
        }
    }
}
