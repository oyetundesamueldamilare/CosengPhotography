using System;
using System.Collections.Generic;
using System.Text;

namespace CosengPhotography.Shared.Dtos
{
    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; // For future JWT validation expansion
    }
}
