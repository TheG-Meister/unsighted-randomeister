using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dev.gmeister.unsighted.randomeister.unsighted;

public enum PlayerAction
{
    Walk,
    Run,
    StaminaRecharge,

    Attack,
    DashAttack,
    JumpAttack,
    SpinAttack,
    Parry,

    ShurikenThrow,
    ShootBullet,
    Spray,
    PlayerGrenade,
    PlayerIceGrenade,
    Grenade,
    ScrapRobotGrenade,
    CreateIceOrRockPlatform,
    ExplodeRock,

    Hookshot,
    HookshotWhileHanging,
    LongHookshot,
    DoubleHookshot,
    ShurikenHookshot,
    HookshotStraightIntoMyPantsDaddy,

    Spinner,
    SpinnerAttack,
    JumpOffSpinner,
    JumpUpOffSpinner,
    DodgeOffSpinner,
    Grind,
    Skip,
    BreakRockWithSpinner,

    Dodge,
    RunningDodge,
    Jump,
    RunningJump,
    ChangeDirectionDuringJump,
    JumpUp,
    WallClimb,
    WallJump,
    JumpWhileClimbing,
    JumpWhileHanging,

    GrabBox,
    PlaceBox,
    ThrowBox,
    BoxJump,

    Hailee,
    HaileeButton,
    HaileePush,
    HaileeMissile,
    BreakRockWithMissile,
    BreakSafeWithMissile,

    Telehook,
    Wierdshot,
    HitSwitchWithBox,
    ClimbSlash,
    Respawn,

    StandOnMaterialCrystal,
    StandOnMinecart,
    StandOnBarrier,
    StandOnRockBlock,
    StandOnUnclimbableGround,

    CoyoteJump,

    PushContainerWithMeteorWeapon,
    BreakRockWithMeteorWeapon,
    BreakSafeWithMeteorWeapon,
}
