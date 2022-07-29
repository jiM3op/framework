using Signum.Entities.Authorization;

namespace Signum.React.Authorization;

#pragma warning disable IDE1006 // Naming Styles
public class LoginRequest
{
    public string userName { get; set; }
    public string password { get; set; }
    public bool? rememberMe { get; set; }
}

public class LoginResponse
{
    public string authenticationType { get; set; }
    public string token { get; set; }
    public UserEntity userEntity { get; set; }
}

public class ChangePasswordRequest
{
    public string oldPassword { get; set; }
    public string newPassword { get; set; }
}

public class ResetPasswordRequest
{
    public string code { get; set; }
    public string newPassword { get; set; }
}

public class ForgotPasswordRequest
{
    public string eMail { get; set; }
}
#pragma warning restore IDE1006 // Naming Styles
