using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Activities;

namespace Workflow.UnitTests
{

    public sealed class FailOnceActivity : CodeActivity
    {
        public FailOnceActivity()
        {

        }
        // Define an activity input argument of type string
        public InArgument<string> Text { get; set; }
        public static int CallCount { get; set; } = 0;

        
        // If your activity returns a value, derive from CodeActivity<TResult>
        // and return the value from the Execute method.
        protected override void Execute(CodeActivityContext context)
        {
            // Obtain the runtime value of the Text input argument
            string text = context.GetValue(this.Text);

            System.Diagnostics.Debug.WriteLine(text);

            //Record the number of calls.
            CallCount++;

            //Fail only once
            if (CallCount == 1)
            {                   
                throw new System.TimeoutException();                
            }           
        }
    }
}
