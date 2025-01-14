﻿
namespace Signum.Entities.Authorization;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class PasswordExpiresIntervalEntity : Entity
{
    public decimal Days { get; set; }

    public decimal DaysWarning { get; set; }

    public bool Enabled { get; set; }
}

[AutoInit]
public static class PasswordExpiresIntervalOperation
{
    public static ExecuteSymbol<PasswordExpiresIntervalEntity> Save;
}

public class PasswordExpiredException : ApplicationException
{
    public PasswordExpiredException() { }
    public PasswordExpiredException(string message) : base(message) { }
    public PasswordExpiredException(string message, Exception inner) : base(message, inner) { }
    protected PasswordExpiredException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    { }
}

