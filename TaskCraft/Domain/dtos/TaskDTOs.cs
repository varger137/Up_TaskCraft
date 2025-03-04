
namespace TaskCraft.DTOs{
public class CreateTaskDTO
{
    public string Title { get; set; }
    public string Description { get; set; }
}

public class GetTaskDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AssignedUserNickName { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; }

}
public class UpdateTaskDTO{
    public string Title { get; set; }
    public string ?Description { get; set; }
    public string Status { get; set; }

}   
}