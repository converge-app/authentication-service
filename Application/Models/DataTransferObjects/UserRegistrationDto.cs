using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.DataTransferObjects
{
    public class UserRegistrationDto
    {
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Required]
        public string Email { get; set; }

        [Required] public string Password { get; set; }
    }
}