﻿using System.Collections.Generic;

namespace Aiwins.Rocket.Cli.ProjectModification
{
    public class MyGetApiResponse
    {
        public string _date { get; set; }

        public List<MyGetPackage> Packages { get; set; }
    }
}