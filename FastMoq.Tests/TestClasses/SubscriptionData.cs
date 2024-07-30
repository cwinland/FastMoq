using System;
using System.Collections.Generic;

namespace FastMoq.Tests.TestClasses
{
    public class SubscriptionData
    {
        internal SubscriptionData()
        {
        }

        internal SubscriptionData(string subscriptionId, string displayName, Guid? tenantId, string authorizationSource, IReadOnlyList<Guid> managedByTenants, IReadOnlyDictionary<string, string> tags)
        {
            SubscriptionId = subscriptionId;
            DisplayName = displayName;
            TenantId = tenantId;
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
        /// <summary> The subscription state. Possible values are Enabled, Warned, PastDue, Disabled, and Deleted. </summary>
        /// <summary> The authorization source of the request. Valid values are one or more combinations of Legacy, RoleBased, Bypassed, Direct and Management. For example, 'Legacy, RoleBased'. </summary>
        public string AuthorizationSource { get; }
        /// <summary> An array containing the tenants managing the subscription. </summary>
        public IReadOnlyList<Guid> ManagedByTenants { get; }
        /// <summary> The tags attached to the subscription. </summary>
        public IReadOnlyDictionary<string, string> Tags { get; }
    }
}
