namespace TaskCraft.Entities{
public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Password { get; set; }
    public string NickName { get; set; }
    public List<Project> ?Projects { get; set; } = new List<Project>();
    public List<TaskEntity> ?Tasks{ get; set; } = new List<TaskEntity>();
    public List<Project> OwnedProjects { get; set; } = new List<Project>();
}
}