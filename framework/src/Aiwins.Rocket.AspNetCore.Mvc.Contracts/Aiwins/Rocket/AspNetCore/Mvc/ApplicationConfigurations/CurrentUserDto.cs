﻿using System;

namespace Aiwins.Rocket.AspNetCore.Mvc.ApplicationConfigurations {
    [Serializable]
    public class CurrentUserDto {
        public bool IsAuthenticated { get; set; }
        public Guid? Id { get; set; }
        public Guid? TenantId { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}