using System.Collections.Generic;

namespace Kalarrs.Serverless.NetCore.Util.ServerlessConfigs
{
    public class Provider
    {
        public string Stage { get; set; }
        public string Region { get; set; }
        public string Name { get; set; }
        public string Runtime { get; set; }
        public Dictionary<string, string> Environment { get; set; }
        public bool? VersionFunctions { get; set; }
    }
}