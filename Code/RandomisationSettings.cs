﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister;

public class RandomisationSettings
{
    public bool useRandomeister;
    public bool randomSeed = true;
    public bool randomiseChests = true;
    public RandomisationData data;

    public RandomisationSettings(bool useRandomeister, RandomisationData data)
    {
        this.useRandomeister = useRandomeister;
        this.data = data;
    }
}
