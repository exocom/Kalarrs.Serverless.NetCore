using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using Kalarrs.Serverless.NetCore.Util.Extensions;
using Microsoft.AspNetCore.Http;

namespace Kalarrs.Serverless.NetCore.Util.RequestDelegates
{
    public static class ApiGateway
    {
        public static RequestDelegate ApiGatewayHandler<T>(IDictionary defatultEnvironmentVariables, Dictionary<string, string> serverlessEnvironmentVariables, HttpConfig httpConfig, MethodBase handlerMethod, IReadOnlyList<ParameterInfo> parameterInfos, T handler)
        {
            return async (context) =>
            {
                if (parameterInfos.Count > 2) throw new Exception("Unrecongnized paramter count for HTTP method. Expected 2 paramters. (object event, ILambdaContext context)");
                // NOTE: KAL-19 unable to check paramter 2 to see if it ILambdaContext. Will allow (object e, object context) which won't work on AWS but will work here.
                // tried https://stackoverflow.com/questions/4963160/how-to-determine-if-a-type-implements-an-interface-with-c-sharp-reflection
                
                APIGatewayProxyResponse response;
                var apiGatewayProxyRequest = await context.ToApiGatewayProxyRequest(httpConfig.Path).ConfigureAwait(false);

                EnvironmentVariable.PrepareEnvironmentVariables(defatultEnvironmentVariables, serverlessEnvironmentVariables,httpConfig.Environment);

                var args = new List<object> { };
                if (parameterInfos.Count > 0) args.Add(apiGatewayProxyRequest);
                if (parameterInfos.Count > 1) args.Add(new TestLambdaContext());
                
                var handlerResponse = handlerMethod.Invoke(handler, parameterInfos.Count == 0 ? null : args.ToArray());
                if (handlerResponse is Task<APIGatewayProxyResponse> task) response = await task.ConfigureAwait(false);
                else if (handlerResponse is APIGatewayProxyResponse proxyResponse) response = proxyResponse;
                else throw new Exception("The Method did not return an APIGatewayProxyResponse.");

                if (response.Headers.Any())
                {
                    foreach (var header in response.Headers)
                    {
                        context.Response.Headers.Add(header.Key, header.Value);
                    }
                }

                context.Response.StatusCode = response.StatusCode;
                if (response.Body != null) await context.Response.WriteAsync(response.Body).ConfigureAwait(false);
            };
        }
    }
}