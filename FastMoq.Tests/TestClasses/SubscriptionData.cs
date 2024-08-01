using System;
using System.Collections.Generic;

namespace FastMoq.Tests.TestClasses
{
    public class SubscriptionData
    {
        internal SubscriptionData()
        {
        }

        internal SubscriptionData(string subscriptionId, string displayName, Guid? tenantId, Guid tenantId2, string authorizationSource, IReadOnlyList<Guid> managedByTenants, IReadOnlyDictionary<string, string> tags)
        {
            SubscriptionId = subscriptionId;
            DisplayName = displayName;
            TenantId = tenantId;
            TenantId2 = tenantId2;
            AuthorizationSource = authorizationSource;
            ManagedByTenants = managedByTenants;
            Tags = tags;
        }
        /// <summary> The subscription ID. </summary>
        public string SubscriptionId { get; }
        /// <summary> The subscription display name. </summary>
        public string DisplayName { get; set; }
        /// <summary> The subscription tenant ID. </summary>
        public Guid? TenantId { get; }
        public Guid TenantId2 { get; }
        public string AuthorizationSource { get; }
        public IReadOnlyList<Guid> ManagedByTenants { get; }
        public IReadOnlyDictionary<string, string> Tags { get; }
    }
}
