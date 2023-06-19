using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static dev.gmeister.unsighted.randomeister.rando.Ability;

namespace dev.gmeister.unsighted.randomeister.rando;

public class Randomiser
{

    private readonly Random random;
    private readonly ChestList chestList;
    private readonly List<string> itemPool;

    public Randomiser(Random random, ChestList chestList, List<string> itemPool)
    {
        this.random = random;
        this.chestList = chestList;
        this.itemPool = itemPool;
    }

    public ChestList Randomise()
    {
        Random random = new(this.random.Next());

        List<string> itemPool = new List<string>(this.itemPool).OrderBy(item => this.random.NextDouble()).ToList();
        ChestList chestList = Chests.CloneChestList(this.chestList);

        Dictionary<string, Dictionary<string, ChestObject>> chestTable = new();
        List<ChestObject> chestPool = new();

        foreach (AreaChestList areaChestList in chestList.areas)
        {
            foreach (ChestObject chest in areaChestList.chestList)
            {
                if (!chestTable.ContainsKey(chest.roomName)) chestTable.Add(chest.roomName, new());
                chestTable[chest.roomName].Add(chest.chestName, chest);
                chestPool.Add(chest);
            }
        }

        Dictionary<string, List<Ability>> itemAbilities = new()
        {
            { "JumpBoots", new() { Jump } },
            { "Blaster", new() { Gun } },
            { "DoctorsGun", new() { Gun } },
            { "Spinner", new() { Rails, Water, Rock } },
            { "Hookshot1", new() { Hook } },
            { "Hookshot2", new() { DoubleHook } },
            { "AutomaticBlaster", new() { Gun } },
            { "Shotgun", new() { Gun } },
            { "Flamethrower", new() { Gun, Plant } },
            { "Icethrower", new() { Water } },
            { "GranadeLauncher", new() { Rock } },
            { "IceGranade", new() { Water, Rock } },
            { "GranadeShotgun", new() { Rock } },
            { "MeteorBlade", new() { Weapon, Sword, CorruptedWeapon } },
            { "IronEdge", new() { Weapon, Sword } },
            { "ThunderEdge", new() { Weapon, Sword } },
            { "Frostbite", new() { Weapon, Sword } },
            { "Flameblade", new() { Weapon, Sword, Plant } },
            { "ElementalBlade", new() { Weapon, Sword, Plant } },
            { "MeteorAxe", new() { Weapon, Axe, CorruptedWeapon } },
            { "WarAxe", new() { Weapon, Axe } },
            { "IceAxe", new() { Weapon, Axe } },
            { "FireAxe", new() { Weapon, Axe, Plant } },
            { "ThunderAxe", new() { Weapon, Axe } },
            { "RaquelAxe", new() { Weapon, Axe } },
            { "IronStar", new() { Shuriken } },
            { "IceStar", new() { Shuriken } },
            { "FireStar", new() { Shuriken, Plant } },
            { "ThunderStar", new() { Shuriken } },
        };

        List<ChestObject> bannedChests = new() { chestTable["AquariumClockRoom"]["Chest"], chestTable["FactoryClockRoom"]["Chest"], chestTable["GardenClockRoom"]["Chest"], chestTable["CraterTowerRoom1"]["StaminaCogChest"], chestTable["CraterTowerRoom5"]["StaminaCogChest"], chestTable["ChurchBlockRoom"]["ChurchChest"], chestTable["ChurchHookshotRoom"]["ChurchChest"], chestTable["ChurchDarkMonsterRoom"]["ChurchChest"], chestTable["ChurchMiniboss"]["ChurchBossChest"] };

        //chestTable[""]

        Dictionary<HashSet<Ability>, List<ChestObject>> prologueAreas = new()
        {
            { new(), new() { chestTable["LabSwordRoom"]["IronEdgeChest"] } },
            { new() { Weapon } , new() { chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"] } },
            //{ new() { Ability.Weapon, Ability.Gun }, new() { chestTable["DowntownIndustrialZoneEntrance"]["GhoulBattleChest"] } },
            //{ new() { Ability.Weapon, Ability.Jump }, new() { chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownLargeRoom"]["HookshotSideChest"] } },
            //{ new() { Ability.Weapon, Ability.Rock }, new() {  } }
        };

        Dictionary<HashSet<Ability>, List<ChestObject>> mainAreas = new()
        {
            { new() { Weapon }, new() { chestTable["GardenVillage"]["RooftopChest"], chestTable["GardenDeepForest"]["DeepForestChest1"], chestTable["GardenWestArea"]["FirstDustChest"] } },
            { new() { Weapon, Gun }, new() { chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"] }},
            { new() { Weapon, Jump }, new() { chestTable["GardenDeepForest"]["DeepForestChest2"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["CavesEntrance"]["CogChest"], chestTable["CavesMinecartPuzzleZero"]["Chest"], chestTable["CavesPond"]["BattleChest"], chestTable["CavesPond"]["DustChest"], chestTable["CavesMinecartBossPuzzle"]["ShurikenChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["MuseumLabyrinth"]["DustChest"], chestTable["MuseumKeyRoom"]["MuseumKey1"], chestTable["MuseumKeyRoomF2"]["Chest"], chestTable["MuseumBasement"]["UltraSecretBasementChest"], chestTable["MuseumBasement"]["Chest"], chestTable["MuseumCombatRoom1"]["SpinnerChest"], chestTable["SewersAquariumConnection"]["SharkChest"], chestTable["SuburbsRailsMainSwitchRoom"]["CrystalChest"], chestTable["SuburbsRailsKeyRoom3"]["RailsKey2"], chestTable["SuburbsRailsKeyRoom3"]["RailsDust"], chestTable["SuburbsRailsKeyRoom"]["SuburbsRailsKey1"], chestTable["SuburbsRailsCombatRoom"]["HookshotChest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"] } },
            { new() { Weapon, Water }, new() { chestTable["GardenVillage"]["WaterfallChest1"], chestTable["GardenVillage"]["LakeChest"], chestTable["GardenShurikenCorridor1"]["HiddenGardenChest"], chestTable["GardenShurikenCorridor2"]["LakeChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["GardenJumpCorridor2"]["RiverChest"], chestTable["CavesShrineElevator"]["SecretTorchChest"], chestTable["CavesPond"]["BattleChest"], chestTable["CavesNestRoom"]["DustChest"], chestTable["CavesNestRoom"]["KeyChest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["SewersColosseum"]["DarkMonsterChest"], chestTable["SewersAquariumConnection"]["SharkChest"], chestTable["AquariumKeyRoom2"]["KeyChest"], chestTable["AquariumBlackoutCorridor"]["Chest"], chestTable["AquariumCentralRoom"]["LeftRiverCurrentChest"], chestTable["AquariumStreet"]["RiverEndChest"], chestTable["AquariumStreet"]["IslandChest"], chestTable["AquariumForest"]["TinyIslandChest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["SewersMainWaterway"]["RiverEndChest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"] } },
            { new() { Weapon, Hook }, new() { chestTable["GardenVillage"]["WaterfallChest1"], chestTable["GardenVillage"]["LakeChest"], chestTable["GardenShurikenCorridor1"]["HiddenGardenChest"], chestTable["GardenShurikenCorridor2"]["LakeChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["GardenJumpCorridor2"]["RiverChest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["MuseumBasement"]["UltraSecretBasementChest"], chestTable["MuseumKeyRoom"]["MuseumKey1"], chestTable["MuseumKeyRoomF2"]["Chest"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["SewersMainWaterway"]["RiverEndChest"], chestTable["SewersCentralRoom"]["EasyChest2"], chestTable["SewersCrossroads"]["AquariumLakeChest"], chestTable["SewersCrossroads"]["TopLakeChest"], chestTable["SewersNorthPassage"]["RiverEndChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"], chestTable["SuburbsRailsKeyRoom2"]["RailsWindKeyChest"], chestTable["SuburbsRailsFirstHookshotRoom"]["RailsHookshotChest"], chestTable["SuburbsRailsFirstHookshotRoom"]["RailsChip2"], chestTable["SuburbsRailsCombatRoom"]["HookshotChest"], chestTable["SuburbsRailsDarkMonsterRoom"]["CorruptedRoadChipChest"], chestTable["SuburbsRailsChurch"]["SuburbsRailsKey3"], chestTable["DowntownChurch"]["ChurchChest"], chestTable["ToyRoom"]["ToyChest"], chestTable["IndustriesSouth"]["IndustriesSouthChest"] } },
            { new() { Weapon, Shuriken }, new() { } },
            { new() { Weapon, Rails }, new() { } }
        };

        List<ChestObject> prologueChests = new() { chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"] };
        List<ChestObject> startChests = new() { chestTable["GardenVillage"]["RooftopChest"], chestTable["GardenDeepForest"]["DeepForestChest1"], chestTable["GardenWestArea"]["FirstDustChest"] };
        List<ChestObject> gunChests = new() { chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"] };

        Dictionary<ChestObject, string> results = new()
        {
            { GetAndRemove(chestPool, chestPool.Find(chest => prologueAreas[new()].Contains(chest))), GetAndRemove(itemPool, itemPool.Find(item => itemAbilities[item].Contains(Weapon))) },
            { GetAndRemove(chestPool, chestPool.Find(chest => prologueAreas[new() { Weapon }].Contains(chest))), GetAndRemove(itemPool, itemPool.Find(item => itemAbilities[item].Contains(Gun))) }
        };

        HashSet<Ability> currentAbilities = new() { Weapon };
        HashSet<ChestObject> accessibleChests = new();

        while (chestPool.Count > 0)
        {
            //use existing permissions to update chest pool
            if (mainAreas.Keys.SelectMany(area => area).Distinct().Except(currentAbilities).Any())
            {
                foreach (HashSet<Ability> abilities in mainAreas.Keys)
                    if (!abilities.Except(currentAbilities).Any())
                    {
                        List<ChestObject> abilityChests = chestPool.FindAll(chest => mainAreas[abilities].Contains(chest));
                        foreach (ChestObject chest in abilityChests) if (!accessibleChests.Contains(chest)) accessibleChests.Add(chest);
                    }
            }
            else accessibleChests = new(chestPool);

            string? nextItem = null;

            List<ChestObject> accessibleEmptyChests = chestPool.FindAll(chest => accessibleChests.Contains(chest)).ToList();
            ChestObject nextChest = GetAndRemove(chestPool, accessibleEmptyChests[0]);

            //if there's more than one chest left, put the next item in
            if (accessibleEmptyChests.Count < 1) throw new InvalidOperationException("No chests are available");
            else if (accessibleEmptyChests.Count > 1) nextItem = GetAndRemove(itemPool, itemPool[0]);
            else
            {
                foreach (string item in itemPool) if (itemAbilities.ContainsKey(item) && itemAbilities[item].Except(currentAbilities).Any())
                    {
                        HashSet<Ability> newAbilities = new(currentAbilities);
                        foreach (Ability ability in itemAbilities[item]) newAbilities.Add(ability);

                        foreach (HashSet<Ability> abilities in mainAreas.Keys) if (!abilities.Except(newAbilities).Any() && mainAreas[abilities].Except(accessibleChests).Any())
                            {
                                nextItem = GetAndRemove(itemPool, item);
                                goto itemFound;
                            }
                    }
                throw new InvalidOperationException("No items left to expand the chest pool");
            }
            itemFound:

            if (nextItem == null) throw new InvalidOperationException("Not enough items in the item pool");

            if (itemAbilities.ContainsKey(nextItem)) foreach (Ability ability in itemAbilities[nextItem]) if (!currentAbilities.Contains(ability)) currentAbilities.Add(ability);

            results.Add(nextChest, nextItem);
        }

        foreach (KeyValuePair<ChestObject, string> randomChest in results) randomChest.Key.reward = randomChest.Value;

        return chestList;
    }

    private T GetAndRemove<T>(List<T> list, T element)
    {
        list.Remove(element);
        return element;
    }

}
