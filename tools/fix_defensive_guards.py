import re

path = r'src\CalradiaForge.Mod\CampaignBehaviors\ClanCharacterProgressionBehavior.cs'
with open(path, 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add safe helpers near top of class
helper_code = '''        // Safe accessors for Campaign-level singletons (null when Campaign is inactive or in tests)
        private static bool IsCampaignActive => Campaign.Current != null;
        private static Clan SafePlayerClan => IsCampaignActive ? Clan.PlayerClan : null;
        private static Hero SafeMainHero => IsCampaignActive ? Hero.MainHero : null;
        private static bool IsPlayerClan(Clan clan) => clan != null && SafePlayerClan != null && clan == SafePlayerClan;
        private static bool IsPlayerHero(Hero hero) => hero != null && SafeMainHero != null && hero == SafeMainHero;

        /// <summary>
        /// Total number of currently alive heroes tracked statelessly via vanilla engine collections.
        /// </summary>
        public int ActiveTrackedHeroesCount => (IsCampaignActive && Hero.AllAliveHeroes != null) ? Hero.AllAliveHeroes.Count : 0;'''

code = re.sub(
    r'(\s*/// <summary>\s*/// Total number of currently alive heroes tracked statelessly via vanilla engine collections\.\s*/// </summary>\s*public int ActiveTrackedHeroesCount => Hero\.AllAliveHeroes != null \? Hero\.AllAliveHeroes\.Count : 0;)',
    '\n' + helper_code,
    code
)

# 2. Fix ShouldProcessInCurrentHour
old_slicing = '''        public static bool ShouldProcessInCurrentHour(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            int entityHash = stringId.GetHashCode() & 0x7FFFFFFF;
            int currentHour = (int)CampaignTime.Now.ToHours % 24;
            return (entityHash % 24) == currentHour;
        }'''

new_slicing = '''        public static bool ShouldProcessInCurrentHour(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return false;
            if (!IsCampaignActive) return true;
            int entityHash = stringId.GetHashCode() & 0x7FFFFFFF;
            int currentHour = (int)CampaignTime.Now.ToHours % 24;
            return (entityHash % 24) == currentHour;
        }'''

code = code.replace(old_slicing, new_slicing)

# 3. Replace all clan == Clan.PlayerClan
code = code.replace('hero.Clan != null && hero.Clan == Clan.PlayerClan', 'IsPlayerClan(hero.Clan)')
code = code.replace('victim.Clan != null && victim.Clan == Clan.PlayerClan', 'IsPlayerClan(victim.Clan)')
code = code.replace('prisonerHero.Clan != null && prisonerHero.Clan == Clan.PlayerClan', 'IsPlayerClan(prisonerHero.Clan)')
code = code.replace('prisoner.Clan != null && prisoner.Clan == Clan.PlayerClan', 'IsPlayerClan(prisoner.Clan)')
code = code.replace('clan != null && clan == Clan.PlayerClan', 'IsPlayerClan(clan)')
code = code.replace('clan == Clan.PlayerClan', 'IsPlayerClan(clan)')
code = code.replace('selectedHeir.Clan != null && selectedHeir.Clan == Clan.PlayerClan', 'IsPlayerClan(selectedHeir.Clan)')
code = code.replace('town.OwnerClan == Clan.PlayerClan', 'IsPlayerClan(town.OwnerClan)')
code = code.replace('firstHero.Clan == Clan.PlayerClan', 'IsPlayerClan(firstHero.Clan)')
code = code.replace('secondHero.Clan == Clan.PlayerClan', 'IsPlayerClan(secondHero.Clan)')
code = code.replace('mother.Clan == Clan.PlayerClan', 'IsPlayerClan(mother.Clan)')
code = code.replace('hero.Clan == Clan.PlayerClan', 'IsPlayerClan(hero.Clan)')
code = code.replace('leader.Clan == Clan.PlayerClan', 'IsPlayerClan(leader.Clan)')
code = code.replace('kingdom == Clan.PlayerClan?.Kingdom', 'kingdom != null && SafePlayerClan != null && kingdom == SafePlayerClan.Kingdom')

# 4. Hero.MainHero replacements
code = code.replace('person1 == Hero.MainHero || person2 == Hero.MainHero', 'IsPlayerHero(person1) || IsPlayerHero(person2)')
code = code.replace('Hero target = (person1 == Hero.MainHero) ? person2 : person1;', 'Hero target = IsPlayerHero(person1) ? person2 : person1;')

# 5. OnHourlyTick CampaignTime guard
old_hourly = '''            // Modulo-24 Hash Time-Sliced evaluation of alive heroes
            var aliveHeroes = Hero.AllAliveHeroes;
            if (aliveHeroes == null) return;

            int count = aliveHeroes.Count;
            int currentHour = (int)CampaignTime.Now.ToHours % 24;'''

new_hourly = '''            if (!IsCampaignActive) return;
            // Modulo-24 Hash Time-Sliced evaluation of alive heroes
            var aliveHeroes = Hero.AllAliveHeroes;
            if (aliveHeroes == null) return;

            int count = aliveHeroes.Count;
            int currentHour = (int)CampaignTime.Now.ToHours % 24;'''

code = code.replace(old_hourly, new_hourly)

with open(path, 'w', encoding='utf-8') as f:
    f.write(code)

print("Updated ClanCharacterProgressionBehavior.cs successfully.")
