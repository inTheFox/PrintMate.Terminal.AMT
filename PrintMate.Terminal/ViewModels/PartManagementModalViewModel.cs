using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HandyControl.Tools.Command;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.Interfaces;
using PrintMate.Terminal.Services;
using Prism.Events;
using Prism.Mvvm;
using ProjectParserTest.Parsers.CliParser;
using ProjectParserTest.Parsers.Shared.Enums;
using ProjectParserTest.Parsers.Shared.Models;

namespace PrintMate.Terminal.ViewModels
{
    public class PartManagementModalViewModel : BindableBase, IViewModelForm
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly PrintService _printService;
        private readonly NotificationService _notificationService;
        private Part _part;
        private string _partName;

        // Типы регионов, которые будут отображаться (исключаем Preview и None)
        private static readonly GeometryRegion[] DisplayedRegionTypes = new[]
        {
            GeometryRegion.Infill,
            GeometryRegion.SupportFill,
            GeometryRegion.Support,
            GeometryRegion.Contour,
            GeometryRegion.ContourUpskin,
            GeometryRegion.ContourDownskin,
            GeometryRegion.Upskin,
            GeometryRegion.Downskin,
            GeometryRegion.Edges
        };

        #region Публичные свойства

        public string PartName
        {
            get => _partName;
            set => SetProperty(ref _partName, value);
        }

        /// <summary>
        /// Коллекция параметров для каждого типа региона
        /// </summary>
        public ObservableCollection<RegionTypeParameters> RegionTypeParametersList { get; } = new();

        public Part Part
        {
            get => _part;
            set
            {
                _part = value;
                if (_part != null)
                {
                    PartName = _part.Name;
                    LoadPartParameters();
                }
            }
        }

        #endregion

        #region Команды

        public RelayCommand CloseCommand { get; set; }
        public RelayCommand ApplyChangesCommand { get; set; }
        public RelayCommand DeletePartCommand { get; set; }

        #endregion

        #region Конструктор

        public PartManagementModalViewModel(
            IEventAggregator eventAggregator,
            PrintService printService,
            NotificationService notificationService)
        {
            _eventAggregator = eventAggregator;
            _printService = printService;
            _notificationService = notificationService;

            ApplyChangesCommand = new RelayCommand(ApplyChanges);
            DeletePartCommand = new RelayCommand(DeletePart);
        }

        #endregion

        #region Методы

        /// <summary>
        /// Загружает параметры детали для каждого типа региона
        /// </summary>
        private void LoadPartParameters()
        {
            RegionTypeParametersList.Clear();

            if (_printService.ActiveProject == null || _part == null)
                return;

            // Получаем все регионы этой детали
            var partRegions = _printService.ActiveProject.Layers
                .SelectMany(layer => layer.Regions)
                .Where(region => region.Part != null && region.Part.Id == _part.Id)
                .ToList();

            // Группируем по типу региона для подсчета
            var regionsByType = partRegions
                .GroupBy(r => r.GeometryRegion)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var regionType in DisplayedRegionTypes)
            {
                var hasRegions = regionsByType.ContainsKey(regionType);
                var regions = hasRegions ? regionsByType[regionType] : null;
                var firstRegion = regions?.FirstOrDefault();

                var parameters = new RegionTypeParameters
                {
                    RegionType = regionType,
                    DisplayName = RegionTypeParameters.GetDisplayName(regionType),
                    HasRegions = hasRegions,
                    IsEnabled = hasRegions,
                    LaserPower = firstRegion?.Parameters?.LaserPower ?? 75,
                    LaserSpeed = firstRegion?.Parameters?.LaserSpeed ?? 1000,
                    LaserBeamDiameter = firstRegion?.Parameters?.LaserBeamDiameter ?? 80
                };

                RegionTypeParametersList.Add(parameters);
            }
        }

        /// <summary>
        /// Применяет изменения параметров ко всем регионам детали по типам
        /// </summary>
        private async void ApplyChanges(object parameter)
        {
            try
            {
                if (_printService.ActiveProject == null || _part == null)
                {
                    _notificationService.Error("Ошибка", "Проект не загружен");
                    return;
                }

                // Валидация - проверяем только те регионы, которые есть в детали
                var invalidParams = RegionTypeParametersList
                    .Where(p => p.HasRegions && (p.LaserPower <= 0 || p.LaserSpeed <= 0 || p.LaserBeamDiameter <= 0))
                    .ToList();

                if (invalidParams.Any())
                {
                    var names = string.Join(", ", invalidParams.Select(p => p.DisplayName));
                    _notificationService.Error("Ошибка", $"Все параметры должны быть больше нуля.\nПроблема в: {names}");
                    return;
                }

                // Собираем информацию о изменениях
                var changesInfo = RegionTypeParametersList
                    .Where(p => p.HasRegions)
                    .Select(p => $"• {p.DisplayName}: {p.LaserPower}Вт, {p.LaserSpeed}мм/с, {p.LaserBeamDiameter}мкм")
                    .ToList();

                var result = await CustomMessageBox.ShowQuestionAsync(
                    "Применить изменения",
                    $"Вы действительно хотите изменить параметры для детали '{PartName}'?\n\n" +
                    $"Новые параметры по типам регионов:\n" +
                    string.Join("\n", changesInfo));

                if (result != Models.MessageBoxResult.Yes)
                    return;

                // Применяем изменения ко всем регионам этой детали во всех слоях
                int updatedRegionsCount = 0;
                foreach (var layer in _printService.ActiveProject.Layers)
                {
                    foreach (var region in layer.Regions.Where(r => r.Part != null && r.Part.Id == _part.Id))
                    {
                        if (region.Parameters == null)
                            continue;

                        // Находим параметры для этого типа региона
                        var typeParams = RegionTypeParametersList.FirstOrDefault(p => p.RegionType == region.GeometryRegion);
                        if (typeParams != null && typeParams.HasRegions)
                        {
                            region.Parameters.LaserPower = typeParams.LaserPower;
                            region.Parameters.LaserSpeed = typeParams.LaserSpeed;
                            region.Parameters.LaserBeamDiameter = typeParams.LaserBeamDiameter;
                            updatedRegionsCount++;
                        }
                    }
                }

                _notificationService.Success("Успешно",
                    $"Параметры детали '{PartName}' обновлены.\n" +
                    $"Изменено регионов: {updatedRegionsCount}");

                CloseCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                _notificationService.Error("Ошибка", $"Не удалось применить изменения: {ex.Message}");
                Console.WriteLine($"[PartManagementModal] Error applying changes: {ex}");
            }
        }

        /// <summary>
        /// Удаляет деталь и все её регионы из всех слоёв
        /// </summary>
        private async void DeletePart(object parameter)
        {
            try
            {
                if (_printService.ActiveProject == null || _part == null)
                {
                    _notificationService.Error("Ошибка", "Проект не загружен");
                    return;
                }

                var result = await CustomMessageBox.ShowQuestionAsync(
                    "Удаление детали",
                    $"Вы действительно хотите удалить деталь '{PartName}'?\n\n" +
                    $"Это действие удалит все регионы этой детали из всех слоёв проекта.\n" +
                    $"Это действие необратимо!");

                if (result != Models.MessageBoxResult.Yes)
                    return;

                int removedRegionsCount = 0;
                foreach (var layer in _printService.ActiveProject.Layers)
                {
                    var regionsToRemove = layer.Regions
                        .Where(r => r.Part != null && r.Part.Id == _part.Id)
                        .ToList();

                    foreach (var region in regionsToRemove)
                    {
                        layer.Regions.Remove(region);
                        removedRegionsCount++;
                    }
                }

                var partsList = _printService.ActiveProject.HeaderInfo?.GetParameterValue<List<Part>>(HeaderKeys.Info.Parts);
                if (partsList != null)
                {
                    var partToRemove = partsList.FirstOrDefault(p => p.Id == _part.Id);
                    if (partToRemove != null)
                    {
                        partsList.Remove(partToRemove);
                        var partsParameter = _printService.ActiveProject.HeaderInfo?.GetParameter(HeaderKeys.Info.Parts);
                        if (partsParameter != null)
                        {
                            partsParameter.SetValue(partsList);
                        }
                    }
                }

                _notificationService.Success("Успешно",
                    $"Деталь '{PartName}' удалена.\n" +
                    $"Удалено регионов: {removedRegionsCount}");

                _eventAggregator.GetEvent<OnProjectModifiedEvent>().Publish("PartDeleted");

                CloseCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                _notificationService.Error("Ошибка", $"Не удалось удалить деталь: {ex.Message}");
                Console.WriteLine($"[PartManagementModal] Error deleting part: {ex}");
            }
        }

        #endregion
    }
}
