namespace Core.Globals;

/// <summary>
/// Specifies the display color for text.
/// </summary>
public enum ColorName : byte
{
    Black,
    Blue,
    Green,
    Cyan,
    Red,
    Magenta,
    Brown,
    Gray,
    DarkGray,
    BrightBlue,
    BrightGreen,
    BrightCyan,
    BrightRed,
    Pink,
    Yellow,
    White
}

/// <summary>
/// Defines character sex.
/// </summary>
public enum Sex : byte
{
    Male,
    Female
}

/// <summary>
/// Represents the high-level logical type of a map tile.
/// </summary>
public enum TileType : byte
{
    None,
    Blocked,
    Warp,
    Item,
    NpcAvoid,
    Resource,
    NpcSpawn,
    Shop,
    Bank,
    Heal,
    Trap,
    Animation,
    NoCrossing,
    Key,
    KeyOpen,
    Door,
    WalkThrough,
    Arena,
    Roof
}

/// <summary>
/// Defines special attributes or data flags for a map tile, often used for map serialization.
/// Note the explicit integer values, which differ from the logical TileType enum.
/// </summary>
public enum XwTileType : byte
{
    None = 0,
    Block = 1,
    Door = 2,
    Item = 3,
    NpcAvoid = 4,
    Key = 5,
    KeyOpen = 6,
    Damage = 7,
    Heal = 8,
    Arena = 9,
    Warp = 10,
    Sign = 11,
    NpcSpawn = 12,
    Shop = 13,
    DirectionBlock = 15,
    NoCrossing = 19,
    WalkThrough = 20,
    Roof = 21
}

/// <summary>
/// Specifies the primary category of an item.
/// </summary>
public enum ItemCategory : byte
{
    Equipment,
    Consumable,
    Event,
    Currency,
    Skill,
    Projectile,
}

/// <summary>
/// Specifies the effect of a consumable item.
/// </summary>
public enum ConsumableEffect : byte
{
    RestoresHealth,
    RestoresMana,
    RestoresStamina,
    GrantsExperience
}

/// <Summary> Equipment used by Players </Summary>
public enum Equipment : byte
{
    Weapon,
    Armor,
    Helmet,
    Shield
}

/// <summary>
/// Represents cardinal and ordinal directions.
/// </summary>
public enum Direction : byte
{
    Down,
    Right,
    Left,
    Up,
    DownRight,
    DownLeft,
    UpRight,
    UpLeft
}

/// <summary>
/// Defines the movement state of a character.
/// </summary>
public enum MovementState : byte
{
    Standing,
    Walking,
    Running
}

/// <summary>
/// Defines user access levels or permissions.
/// </summary>
public enum AccessLevel : byte
{
    None,
    Player,
    Moderator,
    Mapper,
    Developer,
    Owner
}

/// <summary>
/// Defines the behavior patterns for Npcs.
/// </summary>
public enum NpcBehavior : byte
{
    AttackOnSight,
    AttackWhenAttacked,
    Friendly,
    ShopKeeper,
    Guard,
    QuestGiver
}

/// <summary>
/// Specifies the valid targets for an action or skill.
/// </summary>
public enum TargetType : byte
{
    None,
    Player,
    Npc,
    Event,
    Self
}

/// <summary>
/// Defines how an action message is displayed on screen.
/// </summary>
public enum ActionMessageType : byte
{
    Static,
    Scroll,
    Screen
}

/// <summary>
/// Represents core character stats.
/// </summary>
public enum Stat : byte
{
    Strength,
    Vitality,
    Luck,
    Intelligence,
    Spirit
}

/// <summary>
/// Represents character vitals (resource pools).
/// </summary>
public enum Vital : byte
{
    Health,
    Mana,
    Stamina
}

/// <summary>
/// Represents layers in a map render.
/// </summary>
public enum MapLayer : byte
{
    Ground,
    Mask,
    MaskAnimation,
    Cover,
    CoverAnimation,
    Fringe,
    FringeAnimation,
    Roof,
    RoofAnimation
}

public enum SdMapLayer : byte
{
    Ground,
    Mask,
    Mask2,
    Fringe,
    Fringe2
}

/// <summary>
/// Defines resource gathering skills.
/// </summary>
public enum ResourceSkill : byte
{
    Herbalism,
    Woodcutting,
    Mining,
    Fishing
}

/// <summary>
/// Defines commands available in the event system.
/// </summary>
public enum EventCommand
{
    // Message
    AddText,
    ShowText,
    ShowChoices,
    ShowChatBubble,

    // Game Progression
    ModifyVariable,
    ModifySwitch,
    ModifySelfSwitch,

    // Flow Control
    ConditionalBranch,
    ExitEventProcess,
    Label,
    GoToLabel,
    Wait,
    WaitMovementCompletion,

    // Player
    ChangeItems,
    ChangeGold,
    RestoreHealth,
    RestoreStamina,
    RestoreMana,
    GiveExperience,
    LevelUp,
    ChangeLevel,
    ChangeSkills,
    ChangeJob,
    ChangeSprite,
    ChangeSex,
    SetPlayerKillable,
    HoldPlayer,
    ReleasePlayer,

    // Movement
    WarpPlayer,
    SetMoveRoute,

    // Character
    PlayAnimation,

    // Audio & Screen Effects
    PlayBgm,
    FadeOutBgm,
    PlaySound,
    StopSound,
    FadeIn,
    FadeOut,
    FlashScreen,
    SetFog,
    SetWeather,
    SetScreenTint,

    // Pictures
    ShowPicture,
    HidePicture,

    // System
    OpenBank,
    OpenShop,
    SetAccessLevel,
    SpawnNpc,
    Key,
}

/// <summary>
/// Defines the trigger type for a common event.
/// </summary>
public enum CommonEventTrigger
{
    Switch,
    Variable,
    Key,
    Script
}

/// <summary>
/// Specifies the different data editors in the toolset.
/// </summary>
public enum EditorType
{
    None,
    Item,
    Map,
    Npc,
    Skill,
    Shop,
    Resource,
    Animation,
    Pet,
    Quest,
    Job,
    Projectile,
    Moral,
    Script
}

/// <summary>
/// Defines the data type of a draggable UI part.
/// </summary>
public enum DraggablePartType
{
    None,
    Item,
    Skill
}

/// <summary>
/// Defines the origin container of a draggable UI part.
/// </summary>
public enum PartOrigin
{
    None,
    Inventory,
    SkillTree,
    Hotbar,
    Bank
}

/// <summary>
/// Defines the main game menus or scenes.
/// </summary>
public enum Menu
{
    MainMenu,
    Login,
    Register,
    Credits,
    JobSelection,
    NewCharacter,
    CharacterSelect
}

/// <summary>
/// Predefined system dialogue messages.
/// </summary>
public enum SystemMessage
{
    Connection,
    Banned,
    Kicked,
    ClientOutdated,
    ServerMaintenance,
    NameTaken,
    NameLengthInvalid,
    NameContainsIllegalChars,
    DatabaseError,
    WrongPassword,
    AccountActivationRequired,
    MaxCharactersReached,
    ConfirmCharacterDeletion,
    CreateAccount,
    MultipleAccountsNotAllowed,
    Login,
    Crashed,
    Disconnected
}