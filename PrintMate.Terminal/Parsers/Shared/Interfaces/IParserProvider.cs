using System;
using System.Threading.Tasks;
using ProjectParserTest.Parsers.Shared.Models;

namespace ProjectParserTest.Parsers.Shared.Interfaces
{
    public interface IParserProvider
    {
        public event Action<string> ParseStarted;
        public event Action<Project> ParseCompleted;
        public event Action<string> ParseError;
        public event Action<double> ParseProgressChanged;

        Project Project { get; set; }
        Task<Project> ParseAsync(string path);
        public Task<Layer> GetLayer(int index);
        public Task<Layer> GetLayer();
        Task NextLayer();
        void ClearProject();
    }
}