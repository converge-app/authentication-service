using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.DataTransferObjects
{
    public class UserAuthenticationDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}