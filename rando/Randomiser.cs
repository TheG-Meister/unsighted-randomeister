using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using static dev.gmeister.unsighted.randomeister.rando.Ability;

namespace dev.gmeister.unsighted.randomeister.rando;

public class Randomiser
{

    private readonly Random random;
    private readonly ChestList chestList;
    private readonly List<string> itemPool;

    Dictionary<string, Dictionary<string, ChestObject>> chestTable = new();
    List<ChestObject> chestPool = new();

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

    List<Ability> requiredAbilities = new() { Weapon, Gun, Shuriken, Plant, Hook, DoubleHook, Rock, Water, Rails, Jump };
    List<ChestObject> bannedChests;
    List<ChestObject> unbannedChests;

    Dictionary<HashSet<Ability>, List<ChestObject>> prologueAreas;
    Dictionary<HashSet<Ability>, List<ChestObject>> mainAreas;

    public Randomiser(Random random, ChestList chestList, List<string> itemPool)
    {
        this.random = new(random.Next());
        this.chestList = Chests.CloneChestList(chestList);
        this.itemPool = new List<string>(itemPool).OrderBy(item => this.random.NextDouble()).ToList();

        foreach (AreaChestList areaChestList in this.chestList.areas)
        {
            foreach (ChestObject chest in areaChestList.chestList)
            {
                if (!chestTable.ContainsKey(chest.roomName)) chestTable.Add(chest.roomName, new());
                chestTable[chest.roomName].Add(chest.chestName, chest);
                this.chestPool.Add(chest);
            }
        }

        this.chestPool = this.chestPool.OrderBy(chest => this.random.NextDouble()).ToList();

        this.bannedChests = new() { chestTable["AquariumClockRoom"]["Chest"], chestTable["FactoryClockRoom"]["Chest"], chestTable["GardenClockRoom"]["Chest"], chestTable["CraterTowerRoom1"]["StaminaCogChest"], chestTable["CraterTowerRoom5"]["StaminaCogChest"], chestTable["ChurchBlockRoom"]["ChurchChest"], chestTable["ChurchHookshotRoom"]["ChurchChest"], chestTable["ChurchDarkMonsterRoom"]["ChurchChest"], chestTable["ChurchMiniboss"]["ChurchBossChest"], };
        this.unbannedChests = new(chestPool);
        this.unbannedChests.RemoveAll(chest => this.bannedChests.Contains(chest));

        prologueAreas = new(HashSet<Ability>.CreateSetComparer())
        {
            { new(), new() { chestTable["LabSwordRoom"]["IronEdgeChest"] } },
            { new() { Weapon } , new() { chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"] } },
            //{ new() { Ability.Weapon, Ability.Gun }, new() { chestTable["DowntownIndustrialZoneEntrance"]["GhoulBattleChest"] } },
            //{ new() { Ability.Weapon, Ability.Jump }, new() { chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownLargeRoom"]["HookshotSideChest"] } },
            //{ new() { Ability.Weapon, Ability.Rock }, new() {  } }
        };

        mainAreas = new(HashSet<Ability>.CreateSetComparer())
        {
            { new() { Weapon }, new() { chestTable["GardenVillage"]["RooftopChest"], chestTable["GardenDeepForest"]["DeepForestChest1"], chestTable["GardenWestArea"]["FirstDustChest"] } },
            { new() { Weapon, Gun }, new() { chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"] }},
            { new() { Weapon, Jump }, new() { chestTable["GardenDeepForest"]["DeepForestChest2"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownJumpRoom"]["JumpBootsChest"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["CavesEntrance"]["CogChest"], chestTable["CavesMinecartPuzzleZero"]["Chest"], chestTable["CavesPond"]["BattleChest"], chestTable["CavesPond"]["DustChest"], chestTable["CavesMinecartBossPuzzle"]["ShurikenChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["MuseumLabyrinth"]["DustChest"], chestTable["MuseumKeyRoom"]["MuseumKey1"], chestTable["MuseumKeyRoomF2"]["Chest"], chestTable["MuseumBasement"]["UltraSecretBasementChest"], chestTable["MuseumBasement"]["Chest"], chestTable["MuseumCombatRoom1"]["SpinnerChest"], chestTable["SewersAquariumConnection"]["SharkChest"], chestTable["SuburbsRailsMainSwitchRoom"]["CrystalChest"], chestTable["SuburbsRailsKeyRoom3"]["RailsKey2"], chestTable["SuburbsRailsKeyRoom3"]["RailsDust"], chestTable["SuburbsRailsKeyRoom"]["SuburbsRailsKey1"], chestTable["SuburbsRailsCombatRoom"]["HookshotChest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"] } },
            { new() { Weapon, Water }, new() { chestTable["GardenVillage"]["WaterfallChest1"], chestTable["GardenVillage"]["LakeChest"], chestTable["GardenShurikenCorridor1"]["HiddenGardenChest"], chestTable["GardenShurikenCorridor2"]["LakeChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["GardenJumpCorridor2"]["RiverChest"], chestTable["CavesShrineElevator"]["SecretTorchChest"], chestTable["CavesPond"]["BattleChest"], chestTable["CavesNestRoom"]["DustChest"], chestTable["CavesNestRoom"]["KeyChest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["SewersColosseum"]["DarkMonsterChest"], chestTable["SewersAquariumConnection"]["SharkChest"], chestTable["AquariumKeyRoom2"]["KeyChest"], chestTable["AquariumBlackoutCorridor"]["Chest"], chestTable["AquariumCentralRoom"]["LeftRiverCurrentChest"], chestTable["AquariumStreet"]["RiverEndChest"], chestTable["AquariumStreet"]["IslandChest"], chestTable["AquariumForest"]["TinyIslandChest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["SewersMainWaterway"]["RiverEndChest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"] } },
            { new() { Weapon, Hook }, new() { chestTable["GardenVillage"]["WaterfallChest1"], chestTable["GardenVillage"]["LakeChest"], chestTable["GardenShurikenCorridor1"]["HiddenGardenChest"], chestTable["GardenShurikenCorridor2"]["LakeChest"], chestTable["GardenJumpCorridor2"]["GhoulChest"], chestTable["GardenJumpCorridor2"]["DustChest"], chestTable["GardenJumpCorridor2"]["RiverChest"], chestTable["SuburbsDowntownConnection"]["GhoulAlleyChest"], chestTable["SuburbsFillRoom1"]["SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2"]["SuburbsFillRoom2Chest"], chestTable["MuseumStreet"]["DustChest"], chestTable["MuseumEntranceF2"]["TopEntranceChest"], chestTable["MuseumBasement"]["UltraSecretBasementChest"], chestTable["MuseumKeyRoom"]["MuseumKey1"], chestTable["MuseumKeyRoomF2"]["Chest"], chestTable["DowntownJumpRoom"]["AfterBossChest"], chestTable["DowntownJumpRoom"]["JumpyChest"], chestTable["DowntownLargeRoom"]["LongJumpRooftopChest"], chestTable["DowntownLargeRoom"]["DarkChest"], chestTable["DowntownPushPuzzle"]["Chest"], chestTable["DowntownGardenEntrance"]["DarkChest1"], chestTable["DowntownHoleRoom"]["OffscreenChest"], chestTable["DowntownBallPuzzle"]["KeyChest"], chestTable["DowntownSecretDeadend"]["DeadendChest"], chestTable["DowntownTerminal"]["OffscreenChest"], chestTable["DowntownMiniboss1"]["BlasterChest"], chestTable["SewersMainWaterway"]["RiverEndChest"], chestTable["SewersCentralRoom"]["EasyChest2"], chestTable["SewersCrossroads"]["AquariumLakeChest"], chestTable["SewersCrossroads"]["TopLakeChest"], chestTable["SewersNorthPassage"]["RiverEndChest"], chestTable["SuburbsRailsChipCopyEntrance"]["Chest"], chestTable["SuburbsRailsDowntownElevator"]["DustChest"], chestTable["SuburbsRailsKeyRoom2"]["RailsWindKeyChest"], chestTable["SuburbsRailsFirstHookshotRoom"]["RailsHookshotChest"], chestTable["SuburbsRailsFirstHookshotRoom"]["RailsChip2"], chestTable["SuburbsRailsCombatRoom"]["HookshotChest"], chestTable["SuburbsRailsDarkMonsterRoom"]["CorruptedRoadChipChest"], chestTable["SuburbsRailsChurch"]["SuburbsRailsKey3"], chestTable["DowntownChurch"]["ChurchChest"], chestTable["ToyRoom"]["ToyChest"], chestTable["IndustriesSouth"]["IndustriesSouthChest"] } },
            { new(requiredAbilities), new(unbannedChests) },
        };
    }

    public ChestList Randomise()
    {
        //Logic is currently the following:
        // - Put a weapon in the lab chest
        // - put a gun somewhere easy to access in downtown
        // - put something to cross water, jump boots or a hookshot somewhere easy to access in gardens
        // - loop through the world adding progression items and expanding the chest pool
        // - once the player has all types of progression item, fill all remaining chests randomly

        //As an aside, this logic only works because it's adding any one of the three types of starting item in gardens upons up enough chests alone for us to add all the items we need to beat the game
        //We then frontload all of progression items to save on much more complicated logic
        //That logic may be necessary for more complicated randomisers, such as those integrating a map randomiser, or those with more sophisticated options
        //It may be more helpful for customisers too

        Dictionary<ChestObject, string> results = new()
        {
            { GetAndRemove(chestPool, chestPool.Find(chest => prologueAreas[new()].Contains(chest))), GetAndRemove(itemPool, itemPool.Find(item => itemAbilities.ContainsKey(item) && itemAbilities[item].Contains(Weapon))) },
            { GetAndRemove(chestPool, chestPool.Find(chest => prologueAreas[new() { Weapon }].Contains(chest))), GetAndRemove(itemPool, itemPool.Find(item => itemAbilities.ContainsKey(item) && itemAbilities[item].Contains(Gun))) },
        };

        string startingItem = itemPool.Find(item => itemAbilities.ContainsKey(item) && (itemAbilities[item].Contains(Water) || itemAbilities[item].Contains(Hook) || itemAbilities[item].Contains(Jump)));
        results.Add(GetAndRemove(chestPool, chestPool.Find(chest => mainAreas[new() { Weapon }].Contains(chest))), GetAndRemove(itemPool, startingItem));

        HashSet<Ability> currentAbilities = new() { Weapon };
        foreach (Ability ability in itemAbilities[startingItem]) if (!currentAbilities.Contains(ability)) currentAbilities.Add(ability);

        HashSet<ChestObject> accessibleChests = new();

        foreach (HashSet<Ability> abilities in mainAreas.Keys)
            if (!abilities.Except(currentAbilities).Any())
                foreach (ChestObject chest in mainAreas[abilities])
                    accessibleChests.Add(chest);

        while (chestPool.Count > 0)
        {
            if (itemPool.Count < chestPool.Count) throw new InvalidOperationException("There are less items than chests");

            string? nextItem = null;
            List<ChestObject> accessibleEmptyChests = chestPool.FindAll(chest => accessibleChests.Contains(chest)).ToList();
            if (accessibleEmptyChests.Count < 1) throw new InvalidOperationException("No chests are available");

            HashSet<Ability> neededAbilities = new(requiredAbilities.Except(currentAbilities).ToList());
            bool nextHookshotMakesDouble = !currentAbilities.Contains(DoubleHook) && currentAbilities.Contains(Hook);

            if (neededAbilities.Count < 1) nextItem = itemPool[0];
            else nextItem = itemPool.Find(item => itemAbilities.ContainsKey(item) && (itemAbilities[item].Intersect(neededAbilities).Any() || (nextHookshotMakesDouble && item == "Hookshot1")));

            if (itemAbilities.ContainsKey(nextItem) || (nextHookshotMakesDouble && nextItem == "Hookshot1"))
            {
                bool abilityAdded = false;
                foreach (Ability ability in itemAbilities[nextItem]) if (!currentAbilities.Contains(ability))
                    {
                        currentAbilities.Add(ability);
                        abilityAdded = true;
                    }
                if (nextHookshotMakesDouble && nextItem == "Hookshot1")
                {
                    currentAbilities.Add(DoubleHook);
                    abilityAdded = true;
                }

                if (abilityAdded)
                {
                    if (!requiredAbilities.Except(currentAbilities).Any()) accessibleChests = new(chestPool);
                    else
                    {
                        foreach (HashSet<Ability> abilities in mainAreas.Keys)
                            if (!abilities.Except(currentAbilities).Any())
                                foreach (ChestObject chest in mainAreas[abilities])
                                    accessibleChests.Add(chest);
                    }
                }
            }

            ChestObject nextChest = accessibleEmptyChests[0];
            results.Add(nextChest, nextItem);
            chestPool.Remove(nextChest);
            itemPool.Remove(nextItem);
        }

        foreach (KeyValuePair<ChestObject, string> randomChest in results) randomChest.Key.reward = randomChest.Value;

        return chestList;
    }

    private T GetAndRemove<T>(List<T> list, T element)
    {
        list.Remove(element);
        return element;
    }

    //===================================================================================================
    // Everything below will be used in more advanced logic, and is left commented out until it's needed
    //===================================================================================================

    //Actually switch up
    //If an item expands the chest pool by even one
    //or works towards expanding a chest pool by more than one
    //
    //If we put progression focussed items only in chests, how many items would we need to make all of the world available to us?
    /*
    if (accessibleChests.Count > requiredAbilities.Except(currentAbilities).Count() || (accessibleChests.Count == chestPool.Count && accessibleChests.Count == itemPool.Count)) nextItem = itemPool[0];
    else
    {
        HashSet<Ability> abilitiesNeeded = requiredAbilities.Except(currentAbilities).ToHashSet();

        //Build a list of progression-focussed items. Only include each item once, and only include each new set of permissions once
        //If another item covers any items's permissions, remove it
        List<string> progressionItems = new();

        foreach (string item in itemPool) if (itemAbilities.ContainsKey(item) && !progressionItems.Contains(item))
            {
                List<string> itemsToRemove = new();
                foreach (string otherItem in progressionItems)
                {
                    //this item is already covered if no abilities are gained over otherItem
                    bool covered = !itemAbilities[item].Except(itemAbilities[otherItem]).Any();
                    //otheritem is covered by this item if otheritem gains no abilities over item
                    bool otherCovered = !itemAbilities[otherItem].Except(itemAbilities[item]).Any();

                    if (covered) goto itemRemoval;
                    else if (otherCovered) itemsToRemove.Add(otherItem);
                    //if this item contains all the abilities of the other item, 
                    //if another item contains all this item's abilities, don't add this one
                }

                progressionItems.Add(item);

            itemRemoval:
                progressionItems.RemoveAll(item => itemsToRemove.Contains(item));
            }

        //if any permutation of these items works then use that

        //check how many chests we have available rn
        //go through all the areas
        //check for chests we might be able to get access to
        //if adding all the permissions in there (additionally checking other areas with those permissions) gets enough chests
        //when considering current chests, the abilities needed to get there, the progression items available, etc.
        //then we can check
        //additionally keep a list of the items that are allowed? or best? and then add those from the pool instead

        Dictionary<HashSet<Ability>, List<ChestObject>> addedChests = new(HashSet<Ability>.CreateSetComparer());

        foreach (HashSet<Ability> abilities in mainAreas.Keys) if (abilities.Except(currentAbilities).Any())
            {
                HashSet<Ability> extraAbilities = abilities.Except(currentAbilities).ToHashSet();
                if (!addedChests.ContainsKey(extraAbilities)) addedChests.Add(extraAbilities, new());

                foreach (HashSet<Ability> otherAbilities in mainAreas.Keys)
                    if (!otherAbilities.Except(abilities).Any())
                        foreach (ChestObject chest in mainAreas[otherAbilities])
                            if (!accessibleChests.Contains(chest) && !addedChests[extraAbilities].Contains(chest))
                                addedChests[extraAbilities].Add(chest);
            }

        //any set of permissions the requires more items to be put in chests than we get chests back is worth removing from this list
        //unless for example it's one chest and we only need one item to finish all our abilities
        //there's a set of extra chests that become accessible
        //clearly there's a set of items that can be used to get to those chests
        //there's also a set of currently accessible chests from where we are
        //it's also worth considering that this is cumulative too, so it contains chests accessed by a subset of the abilities

        //avoid killing the randomiser by putting in items that mean we can't get all our permissions at any point in the future, even if we put in the best ones

        //let's say there are 2 empty chests avilable
        //hookshot gets us 1 more chest
        //hookshot spinner gets us 1 more chest
        //hookshot, spinner and gun are in the item pool and they get us loads more chests, or cause us to finish
        //how do we know to put a hookshot or a spinner in a chest (even a gun) but not a random crap item
        //picking an item with no abilities means number of chests goes down by one

        //when we put an item in a chest, we gain abilities, maybe gain more empty accessible chests and lose one accessible chest.
        //from the hookshot condition, 2 empties plus 1 extra is 3, adding a hookshot gets us our permission. so -1 ability, -1 chest. 2 chests and 2 abilities left is good
        //if we put a spinner in, we end up with 1 accessible chest, 1 additional chest available with a hookshot and 2 abilities remaining
        //if we put nothing in, we end up with 1 accessible chest, 1 available chest and 3 abilities needed => bad
        //if we put a gun in, we end up with 1 accessible chest, 1 future chest and 2 abilities needed => fine?

        //so an item is okay to add if availableEmptyChests + futureChests - correctedAbilitiesNeeded - 1 is greater than -1
        //what would happen after this is we end up doing the same calculation with adjusted numbers to work out what our future chests number is
        //we want to find the first item in the pool that meets this condition even if it's a bit slow
        //(might end up iterating over 100 items a few times just to get back to the good stuff)

        //I think that's where we get to looping through the item pool to find viable friends

    }
    */

    //find anything that gives water, hook or jump
    //then just whack the rest of the perms in, updating accessible chest accordingly
    //

}
