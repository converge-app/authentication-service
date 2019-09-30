using System;

namespace Application.Models.DataTransferObjects
{
    public class UserAuthenticatedDto
    {
        public string Id { get; set; }
        public string Token { get; set; }
    }
}