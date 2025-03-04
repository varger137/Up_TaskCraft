using AutoMapper;
using TaskCraft.Entities;
using TaskCraft.DTOs;

public class TaskProfile : Profile
{
    public TaskProfile()
    {

        CreateMap<CreateTaskDTO, TaskEntity>();


        CreateMap<UpdateTaskDTO, TaskEntity>();


        CreateMap<TaskEntity, GetTaskDTO>()
            .ForMember(dest => dest.AssignedUserNickName, opt => opt.MapFrom(src => src.AssignedUser.NickName))
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name));
    }
}