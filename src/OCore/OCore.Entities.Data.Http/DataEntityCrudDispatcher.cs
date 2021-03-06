﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using OCore.Http;
using Orleans;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using OCore.Authorization.Abstractions.Request;
using OCore.Authorization;
using OCore.Authorization.Abstractions;

namespace OCore.Entities.Data.Http
{
    public class DataEntityCrudDispatcher : DataEntityDispatcher
    {
        DataEntityGrainInvoker invoker;
        IClusterClient clusterClient;
        IPayloadCompleter payloadCompleter;
        Type grainType;
        HttpMethod httpMethod;

        public DataEntityCrudDispatcher(IEndpointRouteBuilder routeBuilder,
            string prefix,
            string dataEntityName,
            KeyStrategy keyStrategy,
            Type grainType,
            Type dataEntityType,
            IPayloadCompleter payloadCompleter,
            HttpMethod httpMethod) : base(prefix, dataEntityName, keyStrategy)
        {
            this.grainType = grainType;
            MethodInfo methodInfo = null;
            this.httpMethod = httpMethod;
            this.payloadCompleter = payloadCompleter;
            switch (httpMethod)
            {
                case HttpMethod.Post:
                    routeBuilder.MapPost(GetRoutePattern().RawText, Dispatch);
                    methodInfo = typeof(IDataEntity<>).MakeGenericType(dataEntityType).GetMethod("Create");
                    break;
                case HttpMethod.Get:
                    routeBuilder.MapGet(GetRoutePattern().RawText, Dispatch);
                    methodInfo = typeof(IDataEntity<>).MakeGenericType(dataEntityType).GetMethod("Read");
                    break;
                case HttpMethod.Delete:
                    routeBuilder.MapDelete(GetRoutePattern().RawText, Dispatch);
                    methodInfo = typeof(IDataEntity<>).MakeGenericType(dataEntityType).GetMethod("Delete");
                    break;
                case HttpMethod.Put:
                    routeBuilder.MapPut(GetRoutePattern().RawText, Dispatch);
                    methodInfo = typeof(IDataEntity<>).MakeGenericType(dataEntityType).GetMethod("Upsert");
                    break;
            }
            invoker = new DataEntityGrainInvoker(routeBuilder.ServiceProvider, grainType, methodInfo, dataEntityType)
            {
                IsCrudOperation = true,
                HttpMethod = httpMethod,
            };
            clusterClient = routeBuilder.ServiceProvider.GetRequiredService<IClusterClient>();
        }

        public async Task Dispatch(HttpContext httpContext)
        {
            try
            {             
                httpContext.RunAuthorizationFilters(invoker);
                httpContext.RunActionFilters(invoker);

                var payload = Payload.GetOrDefault();
                if (payload != null)
                {
                    await payloadCompleter.Complete(payload, clusterClient);
                }

                var grainId = GetKey(httpContext);
                var grain = clusterClient.GetGrain(grainType, grainId.Key);
                if (grain == null)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                try
                {
                    await invoker.Invoke(grain, httpContext);
                }
                catch (DataCreationException ex)
                {
                    switch (httpMethod)
                    {
                        case HttpMethod.Get:
                        case HttpMethod.Delete:
                        case HttpMethod.Put:
                            throw new StatusCodeException(HttpStatusCode.NotFound, ex.Message, ex);
                        default: 
                            throw;
                    }
                }
            }
            catch (StatusCodeException ex)
            {
                httpContext.Response.StatusCode = (int)ex.StatusCode;
                httpContext.Response.Headers.Clear();
            }
        }

        public static DataEntityCrudDispatcher Register(IEndpointRouteBuilder routeBuilder,
            string prefix,
            string dataEntityName,
            KeyStrategy keyStrategy,
            Type grainType,
            Type dataEntityType,
            IPayloadCompleter payloadCompleter,
            HttpMethod httpMethod)
        {
            return new DataEntityCrudDispatcher(routeBuilder,
                prefix,
                dataEntityName,
                keyStrategy,
                grainType,
                dataEntityType,
                payloadCompleter,
                httpMethod);
        }

    }
}
