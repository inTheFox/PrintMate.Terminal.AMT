using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PrintMate.Terminal.Parsers.Shared.Models;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.Database
{
    public class ProjectsRepository
    {
        private readonly DatabaseContext _dbContext;
        public static string ProjectsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProjectsMemory");

        public ProjectsRepository(DatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ProjectInfo>> GetListAsync() => await _dbContext.Projects.ToListAsync();
        public List<ProjectInfo> GetList() => _dbContext.Projects.ToList();

        public async Task AddAsync(ProjectInfo project)
        {
            try
            {
                await _dbContext.Projects.AddAsync(project);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine("Add success");
                /// TODO: Event
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task RemoveAsync(ProjectInfo project)
        {
            _dbContext.Projects.Remove(project);
            await _dbContext.SaveChangesAsync();
            /// TODO event
        }

        public async Task<ProjectInfo> GetProjectByPath(string path)
        {
            return await _dbContext.Projects.FirstOrDefaultAsync(p => p.Path == path);
        }


        public async Task<ProjectInfo> GetProjectByName(string name)
        {
            return await _dbContext.Projects.FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<ProjectInfo> GetProjectById(int id)
        {
            return await _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
