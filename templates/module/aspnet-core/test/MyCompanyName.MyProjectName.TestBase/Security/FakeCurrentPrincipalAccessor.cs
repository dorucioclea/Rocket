﻿using System.Collections.Generic;
using System.Security.Claims;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Security.Claims;

namespace MyCompanyName.MyProjectName.Security
{
    [Dependency(ReplaceServices = true)]
    public class FakeCurrentPrincipalAccessor : ICurrentPrincipalAccessor, ISingletonDependency
    {
        public ClaimsPrincipal Principal => GetPrincipal();
        private ClaimsPrincipal _principal;

        private ClaimsPrincipal GetPrincipal()
        {
            if (_principal == null)
            {
                lock (this)
                {
                    if (_principal == null)
                    {
                        _principal = new ClaimsPrincipal(
                            new ClaimsIdentity(
                                new List<Claim>
                                {
                                    new Claim(RocketClaimTypes.UserId,"2e701e62-0953-4dd3-910b-dc6cc93ccb0d"),
                                    new Claim(RocketClaimTypes.UserName,"18638215946"),
                                    new Claim(RocketClaimTypes.Name,"测试账号"),
                                    new Claim(RocketClaimTypes.PhoneNumber,"18638215945")
                                }
                            )
                        );
                    }
                }
            }

            return _principal;
        }
    }
}
