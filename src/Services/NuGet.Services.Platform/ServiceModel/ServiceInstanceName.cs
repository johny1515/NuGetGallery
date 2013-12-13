﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NuGet.Services.ServiceModel
{
    [Serializable]
    public class ServiceInstanceName : IEquatable<ServiceInstanceName>
    {
        private const string InstanceNameDataName = "_NuGet_ServiceInstanceName";

        private static readonly Regex ParseRegex = new Regex("^(?<env>.*)_DC(?<dc>.*)_(?<host>.*)_(?<service>.*)_IN(?<instance>.*)$");
        private static readonly string ToStringFormat = "{0}_{1}";
        
        public ServiceHostName Host { get; private set; }
        public string Name { get; private set; }
        public int InstanceId { get; private set; }

        public string FullInstanceName { get { return String.Concat(Name, "_IN", InstanceId); } }

        public ServiceInstanceName(ServiceHostName host, string name, int instanceId)
        {
            Host = host;
            Name = name;
            InstanceId = instanceId;
        }

        public static bool TryParse(string name, out ServiceInstanceName parsed)
        {
            parsed = null;

            var match = ParseRegex.Match(name);
            if (!match.Success)
            {
                return false;
            }
            int dc;
            if (!Int32.TryParse(match.Groups["dc"].Value, out dc))
            {
                return false;
            }
            int instanceId;
            if (!Int32.TryParse(match.Groups["instance"].Value, out instanceId))
            {
                return false;
            }
            parsed = new ServiceInstanceName(
                new ServiceHostName(
                    new DatacenterName(
                        match.Groups["env"].Value,
                        dc),
                    match.Groups["host"].Value),
                match.Groups["service"].Value,
                instanceId);
            return true;
        }

        public static ServiceInstanceName Parse(string name)
        {
            ServiceInstanceName parsed;
            if (!TryParse(name, out parsed))
            {
                throw new FormatException(Strings.ServiceInstanceName_InvalidName);
            }
            return parsed;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,
                ToStringFormat,
                Host,
                FullInstanceName);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ServiceInstanceName);
        }

        public bool Equals(ServiceInstanceName other)
        {
            return other != null &&
                Equals(Host, other.Host) &&
                String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                InstanceId == other.InstanceId;
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Host)
                .Add(Name)
                .Add(InstanceId)
                .CombinedHash;
        }

        public static ServiceInstanceName GetCurrent()
        {
            return (ServiceInstanceName)CallContext.LogicalGetData(InstanceNameDataName);
        }

        public static void SetCurrent(ServiceInstanceName name)
        {
            CallContext.LogicalSetData(InstanceNameDataName, name);
        }

        public static void FreeCurrent()
        {
            CallContext.FreeNamedDataSlot(InstanceNameDataName);
        }
    }
}