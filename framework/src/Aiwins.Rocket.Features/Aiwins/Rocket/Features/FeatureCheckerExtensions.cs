﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aiwins.Rocket.Authorization;
using JetBrains.Annotations;

namespace Aiwins.Rocket.Features {
    public static class FeatureCheckerExtensions {
        public static async Task<T> GetAsync<T> (
            [NotNull] this IFeatureChecker featureChecker, [NotNull] string name,
            T defaultValue = default)
        where T : struct {
            Check.NotNull (featureChecker, nameof (featureChecker));
            Check.NotNull (name, nameof (name));

            var value = await featureChecker.GetOrNullAsync (name);
            return value?.To<T> () ?? defaultValue;
        }

        public static async Task<bool> IsEnabledAsync (this IFeatureChecker featureChecker, bool requiresAll, params string[] featureNames) {
            if (featureNames.IsNullOrEmpty ()) {
                return true;
            }

            if (requiresAll) {
                foreach (var featureName in featureNames) {
                    if (!(await featureChecker.IsEnabledAsync (featureName))) {
                        return false;
                    }
                }

                return true;
            }

            foreach (var featureName in featureNames) {
                if (await featureChecker.IsEnabledAsync (featureName)) {
                    return true;
                }
            }

            return false;
        }

        public static async Task CheckEnabledAsync (this IFeatureChecker featureChecker, string featureName) {
            if (!(await featureChecker.IsEnabledAsync (featureName))) {
                throw new RocketAuthorizationException ("Feature is not enabled: " + featureName);
            }
        }

        public static async Task CheckEnabledAsync (this IFeatureChecker featureChecker, bool requiresAll, params string[] featureNames) {
            if (featureNames.IsNullOrEmpty ()) {
                return;
            }

            if (requiresAll) {
                foreach (var featureName in featureNames) {
                    if (!(await featureChecker.IsEnabledAsync (featureName))) {
                        throw new RocketAuthorizationException (
                            "Required features are not enabled. All of these features must be enabled: " +
                            string.Join (", ", featureNames)
                        );
                    }
                }
            } else {
                foreach (var featureName in featureNames) {
                    if (await featureChecker.IsEnabledAsync (featureName)) {
                        return;
                    }
                }

                throw new RocketAuthorizationException (
                    "Required features are not enabled. At least one of these features must be enabled: " +
                    string.Join (", ", featureNames)
                );
            }
        }
    }
}