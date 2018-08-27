using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Kalarrs.Serverless.NetCore.Util.ServerlessConfigs;
using Newtonsoft.Json.Linq;

namespace Kalarrs.Serverless.NetCore.Util
{
    public class ServerlessProject
    {
        private readonly string _path;
        private readonly JObject _serverlessJson;
        private readonly ServerlessConfig _serverlessConfig;

        private static readonly Regex RoutePrefixSuffixRegex = new Regex("(^/|/$)");

        private const string DefaultPort = "5000";

        public Dictionary<string, string> EnvironmentVariables => _serverlessConfig?.Provider?.Environment;

        public readonly IDictionary DefaultEnvironmentVariables =
            Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        public string Port => _serverlessConfig?.Custom?.LocalDevPort ?? DefaultPort;
        public bool ScheduleLocalTime => _serverlessConfig.Custom.LocalDevScheduleShowLocalTime ?? true;
        public bool ScheduleGet => _serverlessConfig?.Custom?.LocalDevScheduleMethodGet ?? true;
        public bool SchedulePost => _serverlessConfig?.Custom?.LocalDevScheduleMethodPost ?? true;

        public ServerlessProject(string path = null)
        {
            _path = path ?? Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            Export();
            _serverlessJson = ReadServerlessJson();
            _serverlessConfig = _serverlessJson.ToObject<ServerlessConfig>();
        }

        private JObject ReadServerlessJson()
        {
            var serverlessJson = File.ReadAllText($"{_path}/.serverless/serverless.json");
            return JObject.Parse(serverlessJson);
        }

        public IEnumerable<HttpConfig> GetHttpConfigs()
        {
            var httpConfigs = new List<HttpConfig>();

            var functions = _serverlessConfig?.Functions;
            if (functions == null) return httpConfigs;


            foreach (var funtionKeyValue in functions)
            {
                var function = funtionKeyValue.Value;
                if (function.Handler == null) continue;

                var events = function.Events;
                if (function.Handler == null || events == null) continue;

                foreach (var @event in events)
                {
                    var http = @event.Http;
                    var schedule = @event.Schedule;
                    if (http == null && schedule == null) continue;

                    if (http != null)
                    {
                        httpConfigs.Add(new HttpConfig()
                        {
                            FunctionName = funtionKeyValue.Key,
                            EventType = EventType.Http,
                            Hander = function.Handler,
                            Environment = function.Environment,
                            Method = http.Method,
                            Path = http.Path == null ? null : RoutePrefixSuffixRegex.Replace(http.Path, ""),
                            Cors = http.Cors
                        });
                    }

                    if (schedule != null)
                    {
                        if (ScheduleGet)
                        {
                            httpConfigs.Add(new HttpConfig()
                            {
                                FunctionName = funtionKeyValue.Key,
                                EventType = EventType.Schedule,
                                Hander = function.Handler,
                                Environment = function.Environment,
                                Method = HttpMethod.Get,
                                Path = $"{funtionKeyValue.Key}/{(ScheduleLocalTime ? schedule.Meta.Local : schedule.Meta.Utc)}",
                                Cors = true,
                                RequestBody = schedule.Input
                            });
                        }

                        if (SchedulePost)
                        {
                            httpConfigs.Add(new HttpConfig()
                            {
                                FunctionName = funtionKeyValue.Key,
                                EventType = EventType.Schedule,
                                Hander = function.Handler,
                                Environment = function.Environment,
                                Method = HttpMethod.Post,
                                Path = $"{funtionKeyValue.Key}/{(ScheduleLocalTime ? schedule.Meta.Local : schedule.Meta.Utc)}",
                                Cors = true
                            });
                        }
                    }
                }
            }

            return httpConfigs;
        }

        private void Export()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"sls export\"",
                    WorkingDirectory = _path,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}