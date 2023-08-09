using dev.gmeister.unsighted.randomeister.unsighted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.Debug;
using static dev.gmeister.unsighted.randomeister.unsighted.Ability;
using static dev.gmeister.unsighted.randomeister.unsighted.AbilityTools;

namespace dev.gmeister.unsighted.randomeister.rando;

public class Randomiser
{

    private readonly Random random;
    private readonly ChestList chestList;
    private readonly List<string> itemPool;

    Dictionary<string, ChestObject> chestTable = new();
    List<ChestObject> chestPool = new();

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
                chestTable.Add($"{chest.roomName}_{chest.chestName}", chest);
                this.chestPool.Add(chest);
            }
        }

        this.chestPool = this.chestPool.OrderBy(chest => this.random.NextDouble()).ToList();

        this.bannedChests = new() { chestTable["AquariumClockRoom_Chest"], chestTable["FactoryClockRoom_Chest"], chestTable["GardenClockRoom_Chest"], chestTable["CraterTowerRoom1_StaminaCogChest"], chestTable["CraterTowerRoom5_StaminaCogChest"], chestTable["ChurchBlockRoom_ChurchChest"], chestTable["ChurchHookshotRoom_ChurchChest"], chestTable["ChurchDarkMonsterRoom_ChurchChest"], chestTable["ChurchMiniboss_ChurchBossChest"], };
        this.unbannedChests = new(chestPool);
        this.unbannedChests.RemoveAll(chest => this.bannedChests.Contains(chest));

        prologueAreas = new(HashSet<Ability>.CreateSetComparer())
        {
            { new(), new() { chestTable["LabSwordRoom_IronEdgeChest"] } },
            { new() { Weapon } , new() { chestTable["DowntownPushPuzzle_Chest"], chestTable["DowntownBallPuzzle_KeyChest"], chestTable["DowntownSecretDeadend_DeadendChest"] } },
            //{ new() { Ability.Weapon, Ability.Gun }, new() { chestTable["DowntownIndustrialZoneEntrance_GhoulBattleChest"] } },
            //{ new() { Ability.Weapon, Ability.Jump }, new() { chestTable["DowntownJumpRoom_AfterBossChest"], chestTable["DowntownLargeRoom_HookshotSideChest"] } },
            //{ new() { Ability.Weapon, Ability.Rock }, new() {  } }
        };

        mainAreas = new(HashSet<Ability>.CreateSetComparer())
        {
            { new() { Weapon }, new() { chestTable["GardenVillage_RooftopChest"], chestTable["GardenDeepForest_DeepForestChest1"], chestTable["GardenWestArea_FirstDustChest"] } },
            { new() { Weapon, Gun }, new() { chestTable["DowntownGardenEntrance_DarkChest1"], chestTable["DowntownJumpRoom_JumpyChest"], chestTable["DowntownJumpRoom_JumpBootsChest"] }},
            { new() { Weapon, Jump }, new() { chestTable["GardenDeepForest_DeepForestChest2"], chestTable["DowntownGardenEntrance_DarkChest1"], chestTable["DowntownJumpRoom_AfterBossChest"], chestTable["DowntownJumpRoom_JumpyChest"], chestTable["DowntownJumpRoom_JumpBootsChest"], chestTable["DowntownHoleRoom_OffscreenChest"], chestTable["DowntownPushPuzzle_Chest"], chestTable["DowntownBallPuzzle_KeyChest"], chestTable["DowntownSecretDeadend_DeadendChest"], chestTable["DowntownLargeRoom_LongJumpRooftopChest"], chestTable["DowntownLargeRoom_DarkChest"], chestTable["DowntownTerminal_OffscreenChest"], chestTable["DowntownMiniboss1_BlasterChest"], chestTable["CavesEntrance_CogChest"], chestTable["CavesMinecartPuzzleZero_Chest"], chestTable["CavesPond_BattleChest"], chestTable["CavesPond_DustChest"], chestTable["CavesMinecartBossPuzzle_ShurikenChest"], chestTable["GardenJumpCorridor2_GhoulChest"], chestTable["GardenJumpCorridor2_DustChest"], chestTable["SuburbsRailsChipCopyEntrance_Chest"], chestTable["SuburbsDowntownConnection_GhoulAlleyChest"], chestTable["SuburbsFillRoom1_SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2_SuburbsFillRoom2Chest"], chestTable["MuseumStreet_DustChest"], chestTable["MuseumEntranceF2_TopEntranceChest"], chestTable["MuseumLabyrinth_DustChest"], chestTable["MuseumKeyRoom_MuseumKey1"], chestTable["MuseumKeyRoomF2_Chest"], chestTable["MuseumBasement_UltraSecretBasementChest"], chestTable["MuseumBasement_Chest"], chestTable["MuseumCombatRoom1_SpinnerChest"], chestTable["SewersAquariumConnection_SharkChest"], chestTable["SuburbsRailsMainSwitchRoom_CrystalChest"], chestTable["SuburbsRailsKeyRoom3_RailsKey2"], chestTable["SuburbsRailsKeyRoom3_RailsDust"], chestTable["SuburbsRailsKeyRoom_SuburbsRailsKey1"], chestTable["SuburbsRailsCombatRoom_HookshotChest"], chestTable["SuburbsRailsDowntownElevator_DustChest"] } },
            { new() { Weapon, Water }, new() { chestTable["GardenVillage_WaterfallChest1"], chestTable["GardenVillage_LakeChest"], chestTable["GardenShurikenCorridor1_HiddenGardenChest"], chestTable["GardenShurikenCorridor2_LakeChest"], chestTable["GardenJumpCorridor2_GhoulChest"], chestTable["GardenJumpCorridor2_DustChest"], chestTable["GardenJumpCorridor2_RiverChest"], chestTable["CavesShrineElevator_SecretTorchChest"], chestTable["CavesPond_BattleChest"], chestTable["CavesNestRoom_DustChest"], chestTable["CavesNestRoom_KeyChest"], chestTable["SuburbsDowntownConnection_GhoulAlleyChest"], chestTable["SuburbsFillRoom1_SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2_SuburbsFillRoom2Chest"], chestTable["SewersColosseum_DarkMonsterChest"], chestTable["SewersAquariumConnection_SharkChest"], chestTable["AquariumKeyRoom2_KeyChest"], chestTable["AquariumBlackoutCorridor_Chest"], chestTable["AquariumCentralRoom_LeftRiverCurrentChest"], chestTable["AquariumStreet_RiverEndChest"], chestTable["AquariumStreet_IslandChest"], chestTable["AquariumForest_TinyIslandChest"], chestTable["MuseumStreet_DustChest"], chestTable["MuseumEntranceF2_TopEntranceChest"], chestTable["SuburbsRailsChipCopyEntrance_Chest"], chestTable["DowntownLargeRoom_LongJumpRooftopChest"], chestTable["DowntownLargeRoom_DarkChest"], chestTable["DowntownPushPuzzle_Chest"], chestTable["DowntownGardenEntrance_DarkChest1"], chestTable["DowntownJumpRoom_AfterBossChest"], chestTable["DowntownJumpRoom_JumpyChest"], chestTable["DowntownHoleRoom_OffscreenChest"], chestTable["DowntownBallPuzzle_KeyChest"], chestTable["DowntownSecretDeadend_DeadendChest"], chestTable["DowntownTerminal_OffscreenChest"], chestTable["DowntownMiniboss1_BlasterChest"], chestTable["SewersMainWaterway_RiverEndChest"], chestTable["SuburbsRailsDowntownElevator_DustChest"] } },
            { new() { Weapon, Hook }, new() { chestTable["GardenVillage_WaterfallChest1"], chestTable["GardenVillage_LakeChest"], chestTable["GardenShurikenCorridor1_HiddenGardenChest"], chestTable["GardenShurikenCorridor2_LakeChest"], chestTable["GardenJumpCorridor2_GhoulChest"], chestTable["GardenJumpCorridor2_DustChest"], chestTable["GardenJumpCorridor2_RiverChest"], chestTable["SuburbsDowntownConnection_GhoulAlleyChest"], chestTable["SuburbsFillRoom1_SuburbsFillRoomChest"], chestTable["SuburbsFillRoom2_SuburbsFillRoom2Chest"], chestTable["MuseumStreet_DustChest"], chestTable["MuseumEntranceF2_TopEntranceChest"], chestTable["MuseumBasement_UltraSecretBasementChest"], chestTable["MuseumKeyRoom_MuseumKey1"], chestTable["MuseumKeyRoomF2_Chest"], chestTable["DowntownJumpRoom_AfterBossChest"], chestTable["DowntownJumpRoom_JumpyChest"], chestTable["DowntownLargeRoom_LongJumpRooftopChest"], chestTable["DowntownLargeRoom_DarkChest"], chestTable["DowntownPushPuzzle_Chest"], chestTable["DowntownGardenEntrance_DarkChest1"], chestTable["DowntownHoleRoom_OffscreenChest"], chestTable["DowntownBallPuzzle_KeyChest"], chestTable["DowntownSecretDeadend_DeadendChest"], chestTable["DowntownTerminal_OffscreenChest"], chestTable["DowntownMiniboss1_BlasterChest"], chestTable["SewersMainWaterway_RiverEndChest"], chestTable["SewersCentralRoom_EasyChest2"], chestTable["SewersCrossroads_AquariumLakeChest"], chestTable["SewersCrossroads_TopLakeChest"], chestTable["SewersNorthPassage_RiverEndChest"], chestTable["SuburbsRailsChipCopyEntrance_Chest"], chestTable["SuburbsRailsDowntownElevator_DustChest"], chestTable["SuburbsRailsKeyRoom2_RailsWindKeyChest"], chestTable["SuburbsRailsFirstHookshotRoom_RailsHookshotChest"], chestTable["SuburbsRailsFirstHookshotRoom_RailsChip2"], chestTable["SuburbsRailsCombatRoom_HookshotChest"], chestTable["SuburbsRailsDarkMonsterRoom_CorruptedRoadChipChest"], chestTable["SuburbsRailsChurch_SuburbsRailsKey3"], chestTable["DowntownChurch_ChurchChest"], chestTable["ToyRoom_ToyChest"], chestTable["IndustriesSouth_IndustriesSouthChest"] } },
            { new(requiredAbilities), new(unbannedChests) },
        };
    }

    public Dictionary<string, string> Randomise()
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

        Dictionary<string, string> result = new();
        foreach (KeyValuePair<ChestObject, string> chestItems in results) result.Add(Chests.GetChestID(chestItems.Key), chestItems.Value);

        return result;
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
