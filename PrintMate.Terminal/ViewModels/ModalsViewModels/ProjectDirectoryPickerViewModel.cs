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
using DelegateCommand = Prism.Commands.DelegateCommand;
using MessageBox = System.Windows.MessageBox;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{

    public class ProjectDirectoryPickerViewModel : BindableBase
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

        private bool _showFiles = true;
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
        public List<string> AllowedTypes = null;

        public string CurrentDirectory { get; set; } = string.Empty;
        public ObservableCollection<System.Tuple<string, string>> Directories { get; set; } = new();
        public ObservableCollection<DriveInfo> Drives { get; set; }
        protected List<string> History { get; set; }
        public string Result { get; set; }
        public Action CloseAction { get; set; }
        public DelegateCommand LevelUpCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public RelayCommand CloseCommand { get; set; }
        public RelayCommand NextCommand { get; set; }


        public event Action OnClose;
        public event Action<string> OnNext;

        private string _selectedFormat = string.Empty;
        public string SelectedFormat
        {
            get => _selectedFormat;
            set => SetProperty(ref _selectedFormat, value);
        }

        public ProjectDirectoryPickerViewModel()
        {
            LevelUpCommand = new DelegateCommand(LevelUp);
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
            
            CloseCommand = new RelayCommand((e) =>
            {
                OnClose?.Invoke();
            });
            NextCommand = new RelayCommand((e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] START - SelectedFormat={SelectedFormat}");
                //MessageBox.Show("Next command");
                if (SelectedFormat == ".cli")
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] CLI mode - SelectedFilePath={SelectedFilePath}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] Invoking OnNext event...");
                    OnNext?.Invoke(SelectedFilePath);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] OnNext invoked");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] CNC mode - CurrentDirectory={CurrentDirectory}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] Invoking OnNext event...");
                    OnNext?.Invoke(CurrentDirectory);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] OnNext invoked");
                }
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [NextCommand] END");
            });

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

        private void LevelUp()
        {
            if (Drives.FirstOrDefault(p => p.Name.Equals(CurrentDirectory)) != null)
            {
                return;
            }

            ShowDirectory(Directory.GetParent(CurrentDirectory.TrimEnd(Path.DirectorySeparatorChar))!.FullName);
        }

        private async void ShowDirectory(string directory)
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

                if (SelectedFormat == ".cnc")
                {
                    bool noCncFile = true;
                    // Асинхронно проверяем наличие CNC файлов
                    await Task.Run(() =>
                    {
                        foreach (string filePath in Directory.GetFiles(directory))
                        {
                            if (Path.GetExtension(filePath) == ".cnc")
                            {
                                noCncFile = false;
                                break;
                            }
                        }
                    });

                    if (noCncFile)
                    {
                        //Growl.Error("В этой папке нет CNC файлов");
                    }
                    else
                    {
                        //Growl.Info("Проверка сработала");
                        return;
                    }
                }

                Directories.Clear();

                // Асинхронно получаем список директорий и файлов
                var entries = await Task.Run(() =>
                {
                    var result = new List<Tuple<string, string>>();
                    result.Add(new("back", "Назад"));

                    // Добавляем директории
                    result.AddRange(Directory.GetDirectories(CurrentDirectory)
                        .Select(p => Tuple.Create(p, Path.GetFileName(p))));

                    // Добавляем файлы если нужно
                    if (ShowFiles)
                    {
                        result.AddRange(Directory.GetFiles(CurrentDirectory)
                            .Where(p => Path.GetExtension(p) == SelectedFormat)
                            .Select(p => Tuple.Create(p, Path.GetFileName(p))));
                    }

                    return result;
                });

                // Обновляем UI в UI потоке
                foreach (var entry in entries)
                {
                    Directories.Add(entry);
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                //Growl.Error("У вас нет доступа к этой директориии");
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
