namespace TaskCraft.Entities{
public class TaskEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string ?Description { get; set; }
    public Guid AssignedToId { get; set; }
    public string Status { get; set; } = "В процессе";
    public DateTime CreatedAt { get; set; }
    public User AssignedUser { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; }
}
}