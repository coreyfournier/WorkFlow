using log4net;
using System.Activities;
using Workflow.Core.Utilities;

namespace Workflow.Core
{
    public class DebugWriteLine : CodeActivity
    {
        static readonly ILog _log = LogManager.GetLogger(typeof(DebugWriteLine));

        public DebugWriteLine()
        {
            base.DisplayName = "Debug Write Line";
        }

        [RequiredArgument]
        public InArgument<string> Text { get; set; }
        protected override void Execute(CodeActivityContext context)
        {
            PropertyAssistant.SetTransactionId(context);
            _log.Debug(Text.Get(context));
        }
    }
}
