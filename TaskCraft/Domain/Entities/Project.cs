namespace TaskCraft.Entities{
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ?Description { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner {get; set; }
    public DateTime CreatedAt { get; set; } 
    public List<Chat> ?Chats { get; set; } = new List<Chat>();
    public List<TaskEntity> ?Tasks { get; set; } = new List<TaskEntity>();
    public List<User> Users { get; set; } = new List<User>();

}
}