// <copyright file="EventLinkChannelNames.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;

    public static class ChannelNames
    {
        private const string ActivityRegistrationChannelName = "ActivityRegistration";
        private const string BroadcastChannelName = "{0}{1}Broadcast";
        private const string ClientControlChannelName = "{0}{1}ClientControl{2}";
        private const string PublishUpdatesChannelName = "{0}{1}ClientUpdates";

        public static string GetBroadcastName(Guid applicationSessionId, string ns)
        {
            return string.Format(BroadcastChannelName, applicationSessionId, ns);
        }

        public static string GetClientName(Guid applicationSessionId, string ns, Guid clientId)
        {
            return string.Format(ClientControlChannelName, applicationSessionId, ns, clientId);
        }

        public static string GetPublishUpdatesName(Guid applicationSessionId, string ns)
        {
            return string.Format(PublishUpdatesChannelName, applicationSessionId, ns);
        }

        public static string GetActivityRegistrationName()
        {
            return ActivityRegistrationChannelName;
        }
    }
}
