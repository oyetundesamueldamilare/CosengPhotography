
namespace CosengPhotography.Shared.Dtos
{
    public class FrontendAuthResult
    {

        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // For future JWT validation expansion
    }
}
