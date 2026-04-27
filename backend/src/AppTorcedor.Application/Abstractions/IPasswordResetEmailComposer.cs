namespace AppTorcedor.Application.Abstractions;

/// <summary>Monta o e-mail com link para redefinição de senha no frontend.</summary>
public interface IPasswordResetEmailComposer
{
    EmailMessage Compose(string toEmail, string accountEmail, string resetToken);
}
