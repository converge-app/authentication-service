using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.DataTransferObjects
{
    public class UserRegistrationDto
    {
        [Required] 
        public string Id { get; set; }
        [Required]
        public string Password { get; set; }
    }
}