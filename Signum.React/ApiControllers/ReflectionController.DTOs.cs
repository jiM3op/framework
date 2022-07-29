using Signum.Entities.Reflection;
using System.Text.Json.Serialization;

namespace Signum.React.ApiControllers;


public class TypeInfoTS
{
    public KindOfType Kind { get; set; }
    public string FullName { get; set; } = null!;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NiceName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NicePluralName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Gender { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public EntityKind? EntityKind { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public EntityData? EntityData { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsLowPopulation { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsSystemVersioned { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? ToStringFunction { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool QueryDefined { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, MemberInfoTS> Members { get; set; } = null!;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, CustomLiteModelTS>? CustomLiteModels { get; set; } = null!;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool HasConstructorOperation { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, OperationInfoTS>? Operations { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool RequiresEntityPack { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();


    public override string ToString() => $"{Kind} {NiceName} {EntityKind} {EntityData}";
}

public class CustomLiteModelTS
{
    public string? ConstructorFunctionString = null!;
    public bool IsDefault;
}

public class MemberInfoTS
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public TypeReferenceTS? Type { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? NiceName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsReadOnly { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool Required { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Unit { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? Format { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsIgnoredEnum { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsVirtualMList { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? MaxLength { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsMultiline { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool PreserveOrder { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool AvoidDuplicates { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public object? Id { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();
}

public class OperationInfoTS
{
    public OperationType OperationType;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? CanBeNew;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? CanBeModified;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? HasCanExecute;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? HasStates;

    [JsonExtensionData]
    public Dictionary<string, object> Extension { get; set; } = new Dictionary<string, object>();

    public OperationInfoTS(OperationInfo oper)
    {
        this.CanBeNew = oper.CanBeNew;
        this.CanBeModified = oper.CanBeModified;
        this.HasCanExecute = oper.HasCanExecute;
        this.HasStates = oper.HasStates;
        this.OperationType = oper.OperationType;
    }
}

public class TypeReferenceTS
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsCollection { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsLite { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsNotNullable { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public bool IsEmbedded { get; set; }
    public string Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string? TypeNiceName { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public TypeReferenceTS() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.


}

public enum KindOfType
{
    Entity,
    Enum,
    Message,
    Query,
    SymbolContainer,
}
