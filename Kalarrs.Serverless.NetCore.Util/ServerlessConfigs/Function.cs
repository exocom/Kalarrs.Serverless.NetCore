using System.Collections.Generic;

namespace Kalarrs.Serverless.NetCore.Util.ServerlessConfigs
{
    public class Function
    {
        public string Handler { get; set; }
        public List<FunctionEvent> Events { get; set; }
        public Dictionary<string, string> Environment { get; set; }
    }
}