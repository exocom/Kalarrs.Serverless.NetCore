using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kalarrs.Lambda.ScheduledEvents;
using Amazon.Lambda.TestUtilities;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Kalarrs.Serverless.NetCore.Util.RequestDelegates
{
    public static class Schedule
    {
        private static readonly ScheduledEvent DefaultScheduleEvent = new ScheduledEvent()
        {
            Account = "123456789012",
            Region = "us-east-1",
            DetailType = "Scheduled Event",
            Source = "aws.events",
            Time = DateTime.UtcNow,
            Id = "cdc73f9d-aea9-11e3-9d5a-835b769c0d9c",
            Resources = new List<string>() {"arn:aws:events:us-east-1:123456789012:rule/my-schedule"}
        };
        
        public static RequestDelegate ScheduleHandler<T>(IDictionary defatultEnvironmentVariables, Dictionary<string, string> serverlessEnvironmentVariables, HttpConfig httpConfig, MethodBase handlerMethod, IReadOnlyList<ParameterInfo> parameterInfos, T handler)
        {
            return async (context) =>
            {
                if (parameterInfos.Count > 2) throw new Exception("Unrecongnized paramter count for Schedule method. Expected 2 paramters max. (object event, ILambdaContext context)");
                // NOTE: KAL-19 unable to check paramter 2 to see if it ILambdaContext. Will allow (object e, object context) which won't work on AWS but will work here.
                // tried https://stackoverflow.com/questions/4963160/how-to-determine-if-a-type-implements-an-interface-with-c-sharp-reflection

                EnvironmentVariable.PrepareEnvironmentVariables(defatultEnvironmentVariables, serverlessEnvironmentVariables, httpConfig.Environment);
                
                var args = new List<object>();
                if (parameterInfos.Count > 0)
                {
                    var parameter0Type = parameterInfos[0].ParameterType;
                    object @event = DefaultScheduleEvent;
                    if (httpConfig.RequestBody != null)
                        @event = JsonConvert.DeserializeObject(httpConfig.RequestBody.ToString(), parameter0Type);
                    else if (context.Request.Body != null)
                    {
                        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                        {
                            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                            @event = JsonConvert.DeserializeObject(body, parameter0Type);
                        }
                    }
                    args.Add(@event);
                }
                if (parameterInfos.Count > 1) args.Add(new TestLambdaContext());
                
                var handlerResponse = handlerMethod.Invoke(handler, parameterInfos.Count == 0 ? null : args.ToArray());
                    
                object response = null;
                if (handlerResponse is Task<string> task) response = await task.ConfigureAwait(false);
                else if (handlerResponse != null) response = handlerResponse;

                context.Response.StatusCode = 200;
                if (response != null) await context.Response.WriteAsync(JsonConvert.SerializeObject(response)).ConfigureAwait(false);
            };
        }
    }
}