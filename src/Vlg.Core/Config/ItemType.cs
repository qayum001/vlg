namespace Vlg.Core.Config;

/// <summary>domain.md item types.</summary>
public enum ItemType
{
    /// <summary>Goes into a skill socket with a matching tag.</summary>
    Modifier,
    /// <summary>Fires on its own, like a skill.</summary>
    Active,
    /// <summary>Pressed by the player; short effect, cooldown from the press.</summary>
    Usable,
    /// <summary>Global stat or effect; copies stack.</summary>
    Passive,
}
