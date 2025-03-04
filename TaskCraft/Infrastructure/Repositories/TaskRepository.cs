using TaskCraft.DataBase;
using TaskCraft.DTOs;
using Microsoft.EntityFrameworkCore;
using TaskCraft.Entities;
using AutoMapper;

namespace TaskCraft.Repositories
{
    public class TaskRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;


        public TaskRepository(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }


        public async Task<GetTaskDTO> GetTaskById(Guid id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            return task != null ? _mapper.Map<GetTaskDTO>(task) : null;
        }


        public async Task<List<GetTaskDTO>> GetAllTasks()
        {
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .ToListAsync();

            return _mapper.Map<List<GetTaskDTO>>(tasks);
        }


        public async Task AddTask(CreateTaskDTO taskDto, Guid projectId, Guid assignedToId)
        {
            var task = _mapper.Map<TaskEntity>(taskDto);
            task.ProjectId = projectId;
            task.AssignedToId = assignedToId;  
            task.CreatedAt = DateTime.UtcNow;  
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task<GetTaskDTO> UpdateTask(Guid id, UpdateTaskDTO taskDto)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return null;
            }

            _mapper.Map(taskDto, task);  

            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            return _mapper.Map<GetTaskDTO>(task);
        }

        public async Task<bool> DeleteTask(Guid id)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return false;
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}