using System.Collections.Generic;
using Opc2Lib;
using Prism.Mvvm;

namespace PrintMate.Terminal.ViewModels;

public class MonitoringGroup : BindableBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ImagePath { get; set; }
    public List<CommandInfo> Commands = new List<CommandInfo>();

    // Parameterless constructor required for JSON deserialization
    public MonitoringGroup()
    {
    }

    public MonitoringGroup(string id, string name, string imagePath, params CommandInfo[] commands)
    {
        Id = id;
        Name = name;
        ImagePath = imagePath;
        Commands.AddRange(commands);
    }
}