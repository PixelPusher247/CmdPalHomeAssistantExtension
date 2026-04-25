namespace HomeAssistantExtension.Models;

/// <summary>
/// Represents a toggleable Home Assistant entity.
/// </summary>
public class EntityItem
{
    public string EntityId { get; init; } = string.Empty;
    public string FriendlyName { get; init; } = string.Empty;
    public string Domain => EntityId.Contains('.') ? EntityId.Split('.')[0] : "unknown";
    public string State { get; set; } = "unknown";
    public int? Position { get; set; }
    public string? AreaName { get; set; }
    public string? Icon { get; init; }

    public string StateLabel => Domain switch
    {
        "cover" => Position.HasValue ? $"{Position}%" : char.ToUpperInvariant(State[0]) + State[1..],
        _ => IsOn ? "On" : "Off",
    };

    /// <summary>
    /// Whether this entity is considered "on".
    /// </summary>
    public bool IsOn => State.ToLowerInvariant() switch
    {
        "on" => true,
        "home" => true,
        "playing" => true,
        "open" => true,
        "unlocked" => true,
        "heat" => true,
        "cool" => true,
        "auto" => true,
        _ => false,
    };

    public string TurnOnService => Domain switch
    {
        "cover" => "open_cover",
        "lock" => "unlock",
        _ => "turn_on",
    };

    public string TurnOffService => Domain switch
    {
        "cover" => "close_cover",
        "lock" => "lock",
        _ => "turn_off",
    };

    public string ToggleService => Domain switch
    {
        "lock" => IsOn ? "lock" : "unlock",
        "script" => "turn_on",
        _ => "toggle",
    };

    public EntityItem WithState(string newState) => new()
    {
        EntityId = EntityId,
        FriendlyName = FriendlyName,
        State = newState,
        Position = Position,
        AreaName = AreaName,
        Icon = Icon,
    };

    public override string ToString() => $"{FriendlyName} ({EntityId}) [{State}]";
}
