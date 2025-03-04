using TaskCraft.Entities;

namespace TaskCraft.DTOs{
public class GetProjectDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GetChatDTO>? Chats { get; set; }
    public List<string>? ChatNames { get; set; }
    public List<GetUserDTO> Users { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerNickName { get; set; }
    public List<GetTaskDTO>? Tasks { get; set; }
}

public class CreateProjectDTO
{
    public string Name { get; set; }
    public string ?Description { get; set; }
    
}

public class GetInListProjectDTO
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid Id { get; set; }
}

public class UpdateProjectDTO{
    public string Name { get; set;}
    public string Description { get; set; }
    
}
}