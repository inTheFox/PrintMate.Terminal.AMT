using PrintMate.Terminal.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrintMate.Terminal.ViewModels.Configure.ConfigureParametersViewModels
{
    public class PackageInfoModel
    {
        public string Name { get; set; }
        public string Developer { get; set; }

        public PackageInfoModel()
        {
            
        }

        public PackageInfoModel(string name, string developer)
        {
            Name = name;
        }
    }

    public class ConfigureParametersAdditionalSoftwareViewModel : BindableBase
    {
        public ConfigureParametersAdditionalSoftwareViewModel()
        {
            Init();
        }

        private ObservableCollection<PackageInfoModel> _packages;

        public ObservableCollection<PackageInfoModel> Packages
        {
            get => _packages;
            set => SetProperty(ref _packages, value);
        }

        private void Init()
        {
            Packages = new ObservableCollection<PackageInfoModel>(GetNuGetPackages().Select(p=>new PackageInfoModel(p, "")));
        }

        public static List<string> GetNuGetPackages()
        {
            var depsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PrintMate.Terminal.deps.json");

            if (!File.Exists(depsFilePath))
                return new List<string> { "deps.json not found" };

            var json = File.ReadAllText(depsFilePath);
            var doc = JsonDocument.Parse(json);

            var packages = new List<string>();

            // "libraries" содержит все зависимости, включая NuGet-пакеты
            if (doc.RootElement.TryGetProperty("libraries", out var libraries))
            {
                foreach (var lib in libraries.EnumerateObject())
                {
                    // Формат ключа: "PackageName/Version"
                    // Только пакеты из NuGet содержат '/' и обычно не начинаются с "./" или имени проекта
                    var nameWithVersion = lib.Name;
                    if (nameWithVersion.Contains("Microsoft")) continue;
                    if (nameWithVersion.Contains("System")) continue;


                    // Фильтр: исключаем локальные проекты (обычно не содержат '/')
                    if (nameWithVersion.Contains('/'))
                    {
                        packages.Add(nameWithVersion); // Например: "Newtonsoft.Json/13.0.3"
                    }
                }
            }

            return packages.Distinct().OrderBy(p => p).ToList();
        }
    }
}
