using System;
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
        public const double healthValueBattle = 1;
        public const double shieldValuefBattle = 1;
        public const double maxAmmoValue = 100;
        public const double maxPotionsValueLoot = 100;


        public static class Looting
        {
            public const int ShieldLoot = 45;
            public const int AmmoLoot = 800;
            public const int BowLoot = 50000;
        }
    }
}
