﻿using Macrocosm.Common.CrossMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Achievements;

namespace Macrocosm.Content.Achievements
{
    public class TravelToMoon : CustomAchievement
    {
        public override float Order => 37f;
        public override AchievementCategory Category => AchievementCategory.Explorer;
    }
}
