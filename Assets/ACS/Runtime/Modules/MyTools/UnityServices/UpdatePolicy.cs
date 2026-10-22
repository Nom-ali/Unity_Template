using System;
using System.Globalization;

namespace ACS.UnityServices
{
    public enum UpdateRequirement { None, Optional, Required }

    public sealed class UpdatePolicy
    {
        public string MinimumVersion { get; private set; }
        public string LatestVersion { get; private set; }
        public string StoreUrl { get; private set; }
        public bool Critical { get; private set; }

        public static bool TryCreate(string minimum, string latest, string severity, string url, out UpdatePolicy policy)
        {
            policy = null;
            Version min, max;
            Uri store;
            if (!TryVersion(minimum, out min) || !TryVersion(latest, out max) || max < min ||
                (severity != "normal" && severity != "critical") ||
                !Uri.TryCreate(url, UriKind.Absolute, out store) || store.Scheme != "https") return false;
            policy = new UpdatePolicy { MinimumVersion = minimum, LatestVersion = latest,
                Critical = severity == "critical", StoreUrl = url };
            return true;
        }

        public UpdateRequirement Evaluate(string installed)
        {
            Version current, minimum, latest;
            if (!TryVersion(installed, out current)) return UpdateRequirement.None;
            TryVersion(MinimumVersion, out minimum);
            TryVersion(LatestVersion, out latest);
            if (current < minimum || (Critical && current < latest)) return UpdateRequirement.Required;
            return current < latest ? UpdateRequirement.Optional : UpdateRequirement.None;
        }

        public static bool TryVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(value)) return false;
            var parts = value.Split('.');
            if (parts.Length > 4) return false;
            var numbers = new int[4];
            for (int i = 0; i < parts.Length; i++)
                if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i])) return false;
            version = new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
            return true;
        }
    }
}
