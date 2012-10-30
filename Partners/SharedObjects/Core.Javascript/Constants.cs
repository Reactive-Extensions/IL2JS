// <copyright file="Constants.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

using System;
namespace Microsoft.Csa.SharedObjects
{
    public static class Constants
    {
        public const double HeartbeatIntervalSeconds = 15;
        public static Guid ServerId = new Guid("49D8CC73-4CA8-4F41-8D2D-100D86D56EF2");        
        public const string DataCacheName = "SharedObjects";

        public static Guid RootId = new Guid("230B3BBF-EA75-4B51-B97A-43CFD039D10F");

        internal static readonly Guid PresenceCollectionId = new Guid("E3FBD3F9-3517-4463-A3CF-150E1F376764");
        internal static readonly string PresenceCollectionName = "Presence_" + PresenceCollectionId;
        internal const string AnonymousUserSid = "AnonymousUserSid_49D8CC73-4CA8-4F41-8D2D-100D86D56EF2";

        internal const string AdministratorSidConfigSetting = "AdministratorSid";
    }

    public static class WellKnownSid
    {
        public const string EveryoneSid = "133C827D-9B37-4C16-BC38-B8150B961554";
    }

    public static class CollectionPropertyNames
    {
        public const string EvictionPolicy = "EvictionPolicy_71FDC5B4-5ED5-463B-9691-A91A7ED92857";
    }

    public static class Channels
    {
        private const string GlobalListeningChannelName = "GlobalListening";
        private const string ClientControlChannelName = "{0}ClientControl";
        private const string PublishUpdatesChannelName = "{0}ClientUpdates";

        public static string GetClientName(Guid clientId)
        {
            return string.Format(ClientControlChannelName, clientId);
        }

        public static string GetPublishUpdatesName(string ns)
        {
            return string.Format(PublishUpdatesChannelName, ns);
        }

        public static string GetGlobalListeningName()
        {
            return GlobalListeningChannelName;
        }
    }
}
