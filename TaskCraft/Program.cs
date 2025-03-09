using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using TaskCraft.DataBase;
using System.Security.Claims;
using TaskCraft.Repositories;
using TaskCraft.DTOs;
using System.Net.WebSockets;
using Infrastructure.Auth;
using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
#region Builder
var builder = WebApplication.CreateBuilder();

builder.Services.AddCors();
builder.Services.AddAutoMapper(typeof(Program));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ChatRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<TaskRepository>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
        ValidateIssuerSigningKey = true,
    });
#endregion

#region Apps

var app = builder.Build();
app.UseCors(o => o.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();
#endregion


#region UserEndPoints
app.MapPost("/users/register", async (UserRepository userRepository, RegisterUserDTO userDto) =>
{
    if (userDto == null || string.IsNullOrWhiteSpace(userDto.Login) || string.IsNullOrWhiteSpace(userDto.Password))
    {
        return Results.BadRequest("Invalid user data");
    }

    var existingUser = await userRepository.GetUserByLogin(userDto.Login);
    if (existingUser != null)
    {
        return Results.Conflict("User with this login already exists");
    }

    await userRepository.AddUser(userDto);
    return Results.Ok("User registered successfully");
});
app.MapGet("/users/{id}", async (UserRepository userRepository, Guid id) =>
{
    var user = await userRepository.GetUserById(id);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    return Results.Ok(user);
});
app.MapGet("/users", async (UserRepository userRepository) =>
{
    var users = await userRepository.GetAllUsers();
    return Results.Ok(users);
});
app.MapPut("/users/put/{id}", [Authorize] async (HttpContext ctx, UserRepository userRepository, Guid id, UpdateUserDTO userDto) =>
{
    if (userDto == null || string.IsNullOrWhiteSpace(userDto.Login) || string.IsNullOrWhiteSpace(userDto.Password))
    {
        return Results.BadRequest("Invalid user data");
    }

    var existingUser = await userRepository.GetUserById(id);
    if (existingUser == null)
    {
        return Results.NotFound("User not found");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    if (id != userId)
    {
        return Results.Forbid();
    }

    await userRepository.UpdateUser(id, userDto);
    return Results.Ok("User updated successfully");
});
app.MapDelete("/users/delete/{id}", [Authorize] async (HttpContext ctx, UserRepository userRepository, Guid id) =>
{
    var existingUser = await userRepository.GetUserById(id);
    if (existingUser == null)
    {
        return Results.NotFound("User not found");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    if (id != userId)
    {
        return Results.Forbid();
    }

    await userRepository.DeleteUser(id);
    return Results.Ok("User deleted successfully");
});
app.MapPost("/users/login", async (UserRepository userRepository, LoginUserDTO loginDto) =>
{
    if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Login) || string.IsNullOrWhiteSpace(loginDto.Password))
    {
        return Results.BadRequest("Invalid login data");
    }

    var user = await userRepository.AuthenticateUser(loginDto.Login, loginDto.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Login),  
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())  
    };

    var token = AuthOptions.CreateToken(claims.ToDictionary(claim => claim.Type, claim => claim.Value));

    return Results.Ok(new { Token = token });
});
app.MapGet("/users/account", [Authorize] async (UserRepository userRepository, HttpContext ctx) =>
{
    string login = ctx.User.FindFirst(ClaimTypes.Name)?.Value;

    var user = await userRepository.GetUserByLogin(login);
    return Results.Ok(user);
});
#endregion

#region ProjectEndPoints
app.MapGet("/projects", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var projects = await projectRepository.GetUserProjects(userId);
    return Results.Ok(projects);
});
app.MapGet("/projects/{id}", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx, Guid id) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    

    if (!await projectRepository.IsUserInProject(id, userId))
    {
        return Results.Forbid();
    }

    var project = await projectRepository.GetProjectById(id);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }
    return Results.Ok(project);
});
app.MapPost("/projects/create", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx, CreateProjectDTO projectDto) =>
{
    if (projectDto == null || string.IsNullOrWhiteSpace(projectDto.Name))
    {
        return Results.BadRequest("Invalid project data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    await projectRepository.AddProject(projectDto, userId, userId);
    return Results.Ok("Project created successfully");
});
app.MapPut("/projects/put/{id}", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx, Guid id, UpdateProjectDTO projectDto) =>
{
    if (projectDto == null || string.IsNullOrWhiteSpace(projectDto.Name))
    {
        return Results.BadRequest("Invalid project data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(id);
    
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var success = await projectRepository.UpdateProject(id, projectDto);
    return success ? Results.Ok("Project updated successfully") : Results.Problem("Failed to update project");
});
app.MapDelete("/projects/delete/{id}", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx, Guid id) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(id);
    
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var success = await projectRepository.DeleteProject(id);
    return success ? Results.Ok("Project deleted successfully") : Results.Problem("Failed to delete project");
});
app.MapPost("/projects/{projectId}/join", [Authorize] async (ProjectRepository projectRepository, HttpContext ctx, Guid projectId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }


    if (project.Users.Any(u => u.Id == userId))
    {
        return Results.Conflict("User is already in this project");
    }

    await projectRepository.AddUserToProject(projectId, userId);

    return Results.Ok("User successfully joined the project");
});
#endregion

#region ChatEndPoints
app.MapPost("/projects/{projectId}/chats/create", [Authorize] async (ChatRepository chatRepository, ProjectRepository projectRepository, HttpContext ctx, Guid projectId, CreateChatDTO chatDto) =>
{
    if (chatDto == null || string.IsNullOrWhiteSpace(chatDto.Name))
    {
        return Results.BadRequest("Invalid chat data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var chatId = await chatRepository.AddChat(chatDto, userId, projectId);
    return Results.Ok(new { ChatId = chatId, Message = "Chat created successfully" });
});
app.MapGet("/projects/{projectId}/chats/{chatId}", [Authorize] async (
    ChatRepository chatRepository, 
    ProjectRepository projectRepository, 
    Guid projectId, 
    Guid chatId) =>
{
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    var chat = await chatRepository.GetChatById(chatId);
    if (chat == null || chat.ProjectId != projectId)
    {
        return Results.NotFound("Chat not found");
    }

    return Results.Ok(chat);
});
app.MapPut("/projects/{projectId}/chats/put/{chatId}", [Authorize] async (
    ChatRepository chatRepository, 
    ProjectRepository projectRepository, 
    HttpContext ctx, 
    Guid projectId, 
    Guid chatId, 
    UpdateChatDTO chatDto) =>
{
    if (chatDto == null || string.IsNullOrWhiteSpace(chatDto.Name))
    {
        return Results.BadRequest("Invalid chat data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var chat = await chatRepository.GetChatById(chatId);
    if (chat == null || chat.ProjectId != projectId)
    {
        return Results.NotFound("Chat not found");
    }

    var success = await chatRepository.UpdateChat(chatId, chatDto);
    return success ? Results.Ok("Chat updated successfully") : Results.Problem("Failed to update chat");
});
app.MapDelete("/projects/{projectId}/chats/delete/{chatId}", [Authorize] async (
    ChatRepository chatRepository, 
    ProjectRepository projectRepository, 
    HttpContext ctx, 
    Guid projectId, 
    Guid chatId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var chat = await chatRepository.GetChatById(chatId);
    if (chat == null || chat.ProjectId != projectId)
    {
        return Results.NotFound("Chat not found");
    }

    var success = await chatRepository.DeleteChat(chatId);
    return success ? Results.Ok("Chat deleted successfully") : Results.Problem("Failed to delete project");
});


#endregion

#region TaskEndPoints
app.MapPost("/projects/{projectId}/tasks/create", [Authorize] async (
    TaskRepository taskRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId,
    CreateTaskDTO taskDto) =>
{
    if (taskDto == null || string.IsNullOrWhiteSpace(taskDto.Title))
    {
        return Results.BadRequest("Invalid task data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    Guid assignedToId = userId;

    await taskRepository.AddTask(taskDto, projectId, assignedToId);
    return Results.Ok("Task created successfully");
});
app.MapGet("/projects/{projectId}/tasks", [Authorize] async (
    TaskRepository taskRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    var tasks = await taskRepository.GetAllTasks();
    return Results.Ok(tasks);
});
app.MapGet("/projects/{projectId}/tasks/{taskId}", [Authorize] async (
    TaskRepository taskRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId,
    Guid taskId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    var task = await taskRepository.GetTaskById(taskId);
    if (task == null || task.ProjectId != projectId)
    {
        return Results.NotFound("Task not found");
    }

    return Results.Ok(task);
});
app.MapPut("/projects/{projectId}/tasks/put/{taskId}", [Authorize] async (
    TaskRepository taskRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId,
    Guid taskId,
    UpdateTaskDTO taskDto) =>
{
    if (taskDto == null || string.IsNullOrWhiteSpace(taskDto.Title))
    {
        return Results.BadRequest("Invalid task data");
    }

    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var updatedTask = await taskRepository.UpdateTask(taskId, taskDto);
    if (updatedTask == null)
    {
        return Results.NotFound("Task not found");
    }

    return Results.Ok(updatedTask);
});
app.MapDelete("/projects/{projectId}/tasks/delete/{taskId}", [Authorize] async (
    TaskRepository taskRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId,
    Guid taskId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    var project = await projectRepository.GetProjectById(projectId);
    if (project == null)
    {
        return Results.NotFound("Project not found");
    }

    if (project.OwnerId != userId)
    {
        return Results.Forbid();
    }

    var success = await taskRepository.DeleteTask(taskId);
    return success ? Results.Ok("Task deleted successfully") : Results.NotFound("Task not found");
});
#endregion

#region ws
var webSockets = new ConcurrentDictionary<Guid, WebSocket>();

app.Map("/ws/chat/{chatId}", async context =>
{
    var chatId = context.Request.RouteValues["chatId"]?.ToString();
    var token = context.Request.Query["token"].ToString();

    if (string.IsNullOrEmpty(token))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var principal = AuthOptions.ValidateToken(token);
    if (principal == null)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);

    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        webSockets[userId] = webSocket;

        var buffer = new byte[1024 * 4];
        var messageRepository = context.RequestServices.GetRequiredService<MessageRepository>();
        var userRepository = context.RequestServices.GetRequiredService<UserRepository>();

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var messageText = Encoding.UTF8.GetString(buffer, 0, result.Count);

                if (Guid.TryParse(chatId, out var chatGuid))
                {
                    var messageDto = new CreateMessageDTO
                    {
                        Text = messageText,
                        ChatId = chatGuid,
                        UserId = userId
                    };

                    await messageRepository.AddMessageAsync(messageDto);


                    var user = await userRepository.GetUserById(userId);
                    var nickName = user?.NickName ?? "Unknown";


                    var messageObject = new
                    {
                        Text = messageText,
                        NickName = nickName,
                        DateTime = DateTime.UtcNow
                    };

                    var messageJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageObject));


                    foreach (var socket in webSockets.Values)
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            await socket.SendAsync(
                                new ArraySegment<byte>(messageJson),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                        }
                    }
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                webSockets.TryRemove(userId, out _);
            }
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});


app.MapGet("/projects/{projectId}/chats/{chatId}/messages", [Authorize] async (
    MessageRepository messageRepository,
    ProjectRepository projectRepository,
    HttpContext ctx,
    Guid projectId,
    Guid chatId) =>
{
    var userId = Guid.Parse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);




    if (!await projectRepository.IsUserInProject(projectId, userId))
    {

        return Results.Forbid();
    }

    var messages = await messageRepository.GetMessagesByChatIdAsync(chatId);


    return Results.Ok(messages);
});
#endregion

app.Run();
