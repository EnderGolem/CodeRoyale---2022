﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiCup22.Custom
{
    public static class Koefficient
    {

        public const double eps = 0.000001;
        //GeneralBrain
        public const double healthValueBattle = 2000;
        public const double shieldValuefBattle = 3000;
        public const double WeaponValueBattle = 1000;
        public const double maxAmmoValue = 150;
        public const double maxPotionsValueLoot = 70;
        public const double PunishmentForLeavingBattle = 500;

        public const double PunishmentForLeavingStaySafe = 500;
        public const double StayAwayMaxHealthValue = 1800;
        public const double StayAwayBaseValue = 1100;
        public const double StayAwayZoneMaxValue = 2500;

        public const double StayAwayFromCenter = 0.4; //При 0 будет стоять в центре при 1 прямо на зоне

        public const double bowValue = 3000;

        public static class Looting
        {
            public const int ShieldLoot = 45;
            public const int AmmoLoot = 800;
            public const int BowLoot = 50000;
            public const int StaffLoot = 10000;
            public const int WandLoot = 1;
        }
    }
}
