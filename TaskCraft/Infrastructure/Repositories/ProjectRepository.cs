using TaskCraft.DataBase;
using Microsoft.EntityFrameworkCore;
using TaskCraft.Entities;
using TaskCraft.DTOs;
using AutoMapper;
using System;
using System.Threading.Tasks;

namespace TaskCraft.Repositories
{
    public class ProjectRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public ProjectRepository(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<GetProjectDTO?> GetProjectById(Guid id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Chats)
                .Include(p => p.Users)
                .FirstOrDefaultAsync(p => p.Id == id);

            return project != null ? _mapper.Map<GetProjectDTO>(project) : null;
        }

        public async Task<List<GetProjectDTO>> GetAllProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks)
                .Include(p => p.Chats)
                .Include(p => p.Users)
                .ToListAsync();
                
            return _mapper.Map<List<GetProjectDTO>>(projects);
        }

        public async Task<List<GetProjectDTO>> GetUserProjects(Guid userId)
        {
            var projects = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Tasks)
                .Include(p => p.Chats)
                .Include(p => p.Users)
                .Where(p => p.Users.Any(u => u.Id == userId)) 
                .ToListAsync();
                
            return _mapper.Map<List<GetProjectDTO>>(projects);
        }

        public async Task AddProject(CreateProjectDTO projectDto, Guid ownerId,Guid userId)
        {
            var project = _mapper.Map<Project>(projectDto);
                var user = await _context.Users
                .Include(u => u.Projects)
                .FirstOrDefaultAsync(u => u.Id == userId);

            project.Id = Guid.NewGuid();
            project.OwnerId = ownerId;
            project.CreatedAt = DateTime.UtcNow;

            var owner = await _context.Users.FindAsync(ownerId);
            if (owner != null)
            {
                owner.Projects.Add(project);
                project.Users.Add(user);
            }


            

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateProject(Guid projectId, UpdateProjectDTO projectDto)
        {
            var project = await _context.Projects
                .Include(p => p.Users)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) return false;

            _mapper.Map(projectDto, project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AddUserToProject(Guid projectId, Guid userId)
        {
            var project = await _context.Projects
                .Include(p => p.Users)
                .FirstOrDefaultAsync(p => p.Id == projectId);
    
            var user = await _context.Users
                .Include(u => u.Projects)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (project == null || user == null)
            {
                return false;
            }


            if (!project.Users.Any(u => u.Id == userId))
            {

            project.Users.Add(user);
            }
            


            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsUserInProject(Guid projectId, Guid userId)
        {

            return await _context.Projects
                .Where(p => p.Id == projectId)
                .AnyAsync(p => p.Users.Any(u => u.Id == userId));
        }

        public async Task<bool> DeleteProject(Guid id)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return false;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}