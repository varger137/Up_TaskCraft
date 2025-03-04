namespace TaskCraft.Entities{
public class Chat
{
    public Guid Id { get; set; }
    public string Name { get; set; }
     public Guid OwnerId { get; set; }
    public List<Message> ?Messages { get; set; } = new List<Message>();
    public Guid ProjectId { get; set; }
    public Project Project { get; set; }
    public DateTime CreatedAt { get; set; }

    }
}