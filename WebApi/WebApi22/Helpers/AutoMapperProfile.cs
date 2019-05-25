using AutoMapper;
using WebApi22.Dtos;
using WebApi22.Entities;

namespace WebApi22.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
