using System.Text.Json.Serialization;

namespace Signum.React.ApiControllers;

public class EntityPackTS
{
    public Entity entity { get; set; }
    public Dictionary<string, string> canExecute { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object?> extension { get; set; } = new Dictionary<string, object?>();

    public static Action<EntityPackTS>? AddExtension;

    public EntityPackTS(Entity entity, Dictionary<string, string> canExecute)
    {
        this.entity = entity;
        this.canExecute = canExecute;
    }
}
