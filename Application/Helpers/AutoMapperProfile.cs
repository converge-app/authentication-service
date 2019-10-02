using Application.Models.DataTransferObjects;
using Application.Models.Entities;
using AutoMapper;

namespace Application.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<AuthUser, UserDto>();
            CreateMap<UserDto, AuthUser>();
            CreateMap<UserUpdateDto, AuthUser>();
            CreateMap<UserRegisteredDto, AuthUser>();
            CreateMap<AuthUser, UserRegisteredDto>();
            CreateMap<UserRegistrationDto, AuthUser>();
            CreateMap<UserAuthenticationDto, AuthUser>();
            CreateMap<AuthUser, UserAuthenticatedDto>();
        }
    }
}