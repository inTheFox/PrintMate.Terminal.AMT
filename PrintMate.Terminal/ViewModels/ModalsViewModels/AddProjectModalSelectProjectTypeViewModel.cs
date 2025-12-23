using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class ProjectTypeItem
    { 
        public string Format { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public ProjectTypeItem(string format, string name, string description)
        {
            Format = format;
            Name = name;
            Description = description;
        }
    }
    public class AddProjectModalSelectProjectTypeViewModel : BindableBase
    {
        public ObservableCollection<ProjectTypeItem> ProjectTypes { get; set; }
        private ProjectTypeItem _selectedProjectTypeProjectType;
        public ProjectTypeItem SelectedProjectType
        {
            get => _selectedProjectTypeProjectType;
            set => SetProperty(ref _selectedProjectTypeProjectType, value);
        }

        public AddProjectModalSelectProjectTypeViewModel()
        {
            ProjectTypes = new ObservableCollection<ProjectTypeItem>(new()
            {
                new ProjectTypeItem(".cli", "CLI", "проекты с расширением файлов .cli"),
            });
        }
    }
}
