using AutoMapper;
using TaskCraft.Entities;
using TaskCraft.DTOs;
using System.Linq;

namespace TaskCraft.Maps;
public class UserProfile : Profile
{
    public UserProfile()
    {

        CreateMap<RegisterUserDTO, User>();
        CreateMap<UpdateUserDTO, User>();


        CreateMap<User, GetUserDTO>()
            .ForMember(dest => dest.Projects, opt => opt.MapFrom(src => src.Projects));

    }
}