using System.ComponentModel.DataAnnotations;
public class ResetPasswordViewModel
{
    public string Token { get; set; }

    [Required(ErrorMessage = "A nova palavra-passe é obrigatória.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "A palavra-passe deve ter pelo menos 6 caracteres.")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirme a sua palavra-passe.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "As palavras-passe não coincidem.")]
    public string ConfirmPassword { get; set; }
}
