﻿using JetBrains.Annotations;

namespace Aiwins.Rocket.Authorization.Permissions {
    public class PermissionGrantInfo {
        public string Name { get; }

        public bool IsGranted { get; }

        public string ProviderName { get; }

        public string ProviderScope { get; }

        public string ProviderKey { get; }

        public PermissionGrantInfo ([NotNull] string name, bool isGranted, [CanBeNull] string providerName = null, [CanBeNull] string providerScope = null, [CanBeNull] string providerKey = null) {
            Check.NotNull (name, nameof (name));

            Name = name;
            IsGranted = isGranted;
            ProviderName = providerName;
            ProviderScope = providerScope;
            ProviderKey = providerKey;
        }
    }
}