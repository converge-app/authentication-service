using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Application.Models.Entities
{
    public class AuthUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Email { get; set; }
        public string CurrentPassword { get; set; }
        public List<string> Passwords { get; set; } = new List<string>();
    }
}