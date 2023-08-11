using dev.gmeister.unsighted.randomeister.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NifmNetworkConnector;
using UnityEngine.Diagnostics;
using static dev.gmeister.unsighted.randomeister.unsighted.Ability;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public class AbilityTools
{

    public static readonly Dictionary<string, List<Ability>> itemAbilities = new ()
    {
        { "JumpBoots", new () { Jump } },
        { "Blaster", new() { Bullet } },
        { "DoctorsGun", new() { Bullet } },
        { "Spinner", new() { Rails, Water, Rock } },
        { "Hookshot1", new() { Hook } },
        { "Hookshot2", new() { Hook, DoubleHook } },
        { "AutomaticBlaster", new() { Bullet } },
        { "Shotgun", new() { Bullet } },
        { "Flamethrower", new() { Bullet, Plant } },
        { "Icethrower", new() { Water } },
        { "GranadeLauncher", new() { Rock, Grenade } },
        { "IceGranade", new() { Water, Rock } },
        { "GranadeShotgun", new() { Rock, Grenade } },
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

}
