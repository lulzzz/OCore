﻿using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OCore.Authorization.Abstractions.Request
{
    public class Payload
    {
        public bool IsInitialStateSatisfied { get; set; }

        public Permissions InitialPermissions { get; set; }

        public Requirements InitialRequirements { get; set; }

        public bool AllowElevatedRequests { get; set; }

        public bool ElevateRequest { get; set; }

        public bool IsCompleted { get; set; }        

        public Guid Token { get; set; }

        public string ApiKey { get; set; }

        public List<string> ApiKeyApplications { get; set; }

        public Guid? OriginalAccountId { get; set; }

        public Guid? ProjectedAccountId { get; set; }

        public Guid? AccountId => ProjectedAccountId.HasValue ? ProjectedAccountId : OriginalAccountId;

        public bool AccountIdHasBeenProjected { get; set; } 
            
        public string TenantId { get; set; }

        public bool IsRequestElevated { get; set; }

        public List<string> Roles { get; set; }

        public static Payload Get()
        {
            var payload = RequestContext.Get("I") as Payload;
            if (payload == null)
            {
                throw new UnauthorizedAccessException("No identity payload was found in the request context");
            }
            return payload;
        }

        public static Payload GetOrDefault()
        {
            return RequestContext.Get("I") as Payload;
        }
    }
}
