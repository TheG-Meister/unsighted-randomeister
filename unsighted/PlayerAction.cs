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
    Grenade,
    CreateIceOrRockPlatform,

    Hookshot,
    HookshotWhileHanging,
    LongHookshot,
    DoubleHookshot,
    ShurikenHookshot,

    Spinner,
    SpinnerAttack,
    JumpOffSpinner,
    JumpUpOffSpinner,
    DodgeOffSpinner,
    Grind,
    Skip,

    Dodge,
    Jump,
    JumpUp,
    WallClimb,
    WallJump,
    JumpWhileHanging,

    GrabBox,
    PlaceBox,
    ThrowBox,
    BoxJump,

    Hailee,
    HaileeButton,
    HaileePush,
    HaileeMissile,

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

}
