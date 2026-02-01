using System.ComponentModel.DataAnnotations;
public class ResetPasswordViewModel
{
    public string Token { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Compare("NewPassword")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }
}
