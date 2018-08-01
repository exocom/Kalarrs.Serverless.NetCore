using Newtonsoft.Json.Linq;

namespace Kalarrs.Serverless.NetCore.Util.ServerlessConfigs
{
    public class ScheduleEvent
    {
        public string Rate { get; set; }
        public bool Enabled { get; set; }
        public ScheduleEventMeta Meta { get; set; }
        public JObject Input { get; set; }
    }
}