using Application.Models.DataTransferObjects;
using Application.Models.Entities;
using AutoMapper;

namespace Application.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
            CreateMap<UserUpdateDto, User>();
            CreateMap<UserRegisteredDto, User>();
            CreateMap<User, UserRegisteredDto>();
            CreateMap<UserRegistrationDto, User>();
            CreateMap<UserAuthenticationDto, User>();
            CreateMap<User, UserAuthenticatedDto>();
        }
    }
}