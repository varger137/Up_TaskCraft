using AutoMapper;
using TaskCraft.Entities;
using TaskCraft.DTOs;
using System.Linq;

public class ProjectProfile : Profile
{
    public ProjectProfile()
    {

        CreateMap<CreateProjectDTO, Project>();


        CreateMap<UpdateProjectDTO, Project>();

        CreateMap<Project, GetProjectDTO>()
            .ForMember(dest => dest.OwnerNickName, opt => opt.MapFrom(src => src.Owner.NickName))
            .ForMember(dest => dest.ChatNames, opt => opt.MapFrom(src => src.Chats.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.Chats, opt => opt.MapFrom(src => src.Chats))
            .ForMember(dest => dest.Tasks, opt => opt.MapFrom(src => src.Tasks));


        CreateMap<Project, GetInListProjectDTO>();
    }
}