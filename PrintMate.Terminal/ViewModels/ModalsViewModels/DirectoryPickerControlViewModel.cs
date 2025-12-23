using HandyControl.Tools.Extension;
using PrintMate.Terminal.Interfaces;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Controls;
using HandyControl.Tools.Command;
using Newtonsoft.Json;
using PrintMate.Terminal.Services;
using DelegateCommand = Prism.Commands.DelegateCommand;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class SpecialFolder
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    public class DirectoryPickerControlViewModel : BindableBase, IDialogResultable<string>, IViewModelForm
    {
        private ObservableCollection<TabItemViewModel> _tabItems;
        public ObservableCollection<TabItemViewModel> TabItems
        {
            get { return _tabItems; }
            set { SetProperty(ref _tabItems, value); }
        }

        private DriveInfo _selectedDrive;
        public DriveInfo SelectedDrive
        {
            get => _selectedDrive;
            set
            {
                OnSelectDrive(value);
                SetProperty(ref _selectedDrive, value);
            }
        }

        private int _drivesCount = 0;
        public int DrivesCount
        {
            get => _drivesCount;
            set => SetProperty(ref _drivesCount, value);
        }

        private bool _showFiles = false;
        public bool ShowFiles
        {
            get => _showFiles;
            set => SetProperty(ref _showFiles, value);
        }

        public int _selectedDirectory;
        public int SelectedDirectory
        {
            get => _selectedDirectory;
            set
            {
                _selectedDirectory = value;

                if (value >= 0)
                {
                    var directory = Directories.ElementAtOrDefault(value);
                    if (directory == null) return;

                    if (ShowFiles && File.Exists(directory.Item1))
                    {
                        SelectedFilePath = directory.Item1;
                        return;
                    }

                    ShowDirectory(directory.Item1);
                    return;
                }
            }
        }

        public string SelectedFilePath;
        private List<string> _allowedTypes = new List<string>();

        public List<string> AllowedTypes
        {
            get => _allowedTypes;
            set => SetProperty(ref _allowedTypes, value);
        }

        public string? ModalId = null;

        public string CurrentDirectory { get; set; } = string.Empty;
        public ObservableCollection<System.Tuple<string, string>> Directories { get; set; } = new();
        public ObservableCollection<DriveInfo> Drives { get; set; }
        protected List<string> History { get; set; }
        public string Result { get; set; }
        public Action CloseAction { get; set; }
        public DelegateCommand LevelUpCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }
        public RelayCommand SelectCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }

        public DirectoryPickerControlViewModel()
        {
            LevelUpCommand = new DelegateCommand(LevelUp);
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
            SelectCommand = new RelayCommand(SelectCommandCallback);

            Drives = new (DriveInfo.GetDrives());
            DrivesCount = Drives.Count;
            History = new List<string>();
            TabItems = new (Drives.Select(p => new TabItemViewModel(p.Name)));
            ShowDirectory(Drives.FirstOrDefault()!.Name);

            SpecialFolders = new ObservableCollection<SpecialFolder>
            {
                new SpecialFolder
                {
                    Name = "Рабочий стол",
                    Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                },
                new SpecialFolder
                {
                    Name = "Документы",
                    Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                },
                new SpecialFolder
                {
                    Name = "Загрузки",
                    Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                },
            };
        }

        private async void SelectCommandCallback(object obj)
        {
            await ModalService.Instance.CloseAsync(ModalId, true);
        }

        private void LevelUp()
        {
            if (Drives.FirstOrDefault(p => p.Name.Equals(CurrentDirectory)) != null)
            {
                return;
            }

            ShowDirectory(Directory.GetParent(CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName);
        }

        private void ShowDirectory(string directory)
        {
            if (directory == "back")
            {
                LevelUp();
                return;
            }

            try
            {
                CurrentDirectory = directory;

                //Growl.Info(CurrentDirectory);

                //if (AllowedTypes != null && AllowedTypes.Contains(".cnc"))
                //{
                //    bool noCncFile = true;
                //    foreach (string filePath in Directory.GetFiles(directory))
                //    {
                //        if (Path.GetExtension(filePath) == ".cnc")
                //        {
                //            noCncFile = false;
                //            break;
                //        }
                //    }
                    
                //    if (noCncFile)
                //    {
                //        Growl.Error("В этой папке нет CNC файлов");
                //    }
                //    else
                //    {
                //        Growl.Info("Проверка сработала");
                //        return;
                //    }
                //}

                Directories.Clear();


                Directories.Add(new("back", "Назад"));
                Directories.AddRange(Directory.GetDirectories(CurrentDirectory)
                    .Select(p => Tuple.Create(p, Path.GetFileName(p))));
                if (ShowFiles)
                {
                    if (AllowedTypes != null && AllowedTypes.Count > 0)
                    {
                        //MessageBox.Show(string.Join(",", AllowedTypes));

                        Directories.AddRange(Directory.GetFiles(CurrentDirectory)
                            .Where(p => AllowedTypes.Contains(Path.GetExtension(p)))
                            .Select(p => Tuple.Create(p, Path.GetFileName(p))));
                        return;
                    }
                    else
                    {
                        Directories.AddRange(Directory.GetFiles(CurrentDirectory)
                            .Select(p => Tuple.Create(p, Path.GetFileName(p))));
                    }
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                Growl.Error("У вас нет доступа к этой директориии");
            }

            SelectedDirectory = -1;
        }

        private ObservableCollection<SpecialFolder> _specialFolders;
        public ObservableCollection<SpecialFolder> SpecialFolders
        {
            get => _specialFolders;
            set => SetProperty(ref _specialFolders, value);
        }

        private SpecialFolder _selectedSpecialFolder;
        public SpecialFolder SelectedSpecialFolder
        {
            get => _selectedSpecialFolder;
            set
            {
                SetProperty(ref _selectedSpecialFolder, value);
                ShowDirectory(value.Path);
            }
        }

        private void Save()
        {
            Result = CurrentDirectory;
            CloseAction?.Invoke();
        }

        private void Cancel()
        {
            Result = string.Empty;
            CloseAction?.Invoke();
        }

        private void OnSelectDrive(DriveInfo selectedDrive)
        {
            var drive = Drives.FirstOrDefault(p => p.Name == selectedDrive.Name);
            if (drive == null) return;
            ShowDirectory(drive.Name);
        }

    }
}
