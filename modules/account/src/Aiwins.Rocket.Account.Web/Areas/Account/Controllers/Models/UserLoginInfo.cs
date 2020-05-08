﻿using System;
using System.ComponentModel.DataAnnotations;
using Aiwins.Rocket.Auditing;

namespace Aiwins.Rocket.Account.Web.Areas.Account.Controllers.Models {
    public class UserLoginInfo {
        [Required]
        [StringLength (255)]
        public string UserNameOrEmailAddress { get; set; }

        [Required]
        [StringLength (32)]
        [DataType (DataType.Password)]
        [DisableAuditing]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public Guid? TenantId { get; set; }
    }
}