using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Windows.Input;

namespace PrintMate.Terminal.ViewModels.ModalsViewModels
{
    public class PreparationChecklistViewModel : BindableBase
    {
        #region Checklist Items

        private bool _filterValvesOpen;
        public bool FilterValvesOpen
        {
            get => _filterValvesOpen;
            set
            {
                if (SetProperty(ref _filterValvesOpen, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _powderDispenserValvesOpen;
        public bool PowderDispenserValvesOpen
        {
            get => _powderDispenserValvesOpen;
            set
            {
                if (SetProperty(ref _powderDispenserValvesOpen, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _dischargeContainersValvesOpen;
        public bool DischargeContainersValvesOpen
        {
            get => _dischargeContainersValvesOpen;
            set
            {
                if (SetProperty(ref _dischargeContainersValvesOpen, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _protectiveGlassesCleaned;
        public bool ProtectiveGlassesCleaned
        {
            get => _protectiveGlassesCleaned;
            set
            {
                if (SetProperty(ref _protectiveGlassesCleaned, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _inertGasConnected;
        public bool InertGasConnected
        {
            get => _inertGasConnected;
            set
            {
                if (SetProperty(ref _inertGasConnected, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _chillerOn;
        public bool ChillerOn
        {
            get => _chillerOn;
            set
            {
                if (SetProperty(ref _chillerOn, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _buildPlateInstalled;
        public bool BuildPlateInstalled
        {
            get => _buildPlateInstalled;
            set
            {
                if (SetProperty(ref _buildPlateInstalled, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _recoaterKnifeSet;
        public bool RecoaterKnifeSet
        {
            get => _recoaterKnifeSet;
            set
            {
                if (SetProperty(ref _recoaterKnifeSet, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        private bool _firstLayerApplied;
        public bool FirstLayerApplied
        {
            get => _firstLayerApplied;
            set
            {
                if (SetProperty(ref _firstLayerApplied, value))
                {
                    RaisePropertyChanged(nameof(AllChecked));
                }
            }
        }

        #endregion

        #region AllChecked Property

        /// <summary>
        /// Возвращает true только если все галочки проставлены
        /// </summary>
        public bool AllChecked =>
            FilterValvesOpen &&
            PowderDispenserValvesOpen &&
            DischargeContainersValvesOpen &&
            ProtectiveGlassesCleaned &&
            InertGasConnected &&
            ChillerOn &&
            BuildPlateInstalled &&
            RecoaterKnifeSet &&
            FirstLayerApplied;

        #endregion

        private bool _allCheck;

        public bool AllCheck
        {
            get => _allCheck;
            set
            {
                SetProperty(ref _allCheck, value);
                FilterValvesOpen = value;
                PowderDispenserValvesOpen = value;
                DischargeContainersValvesOpen = value;
                ProtectiveGlassesCleaned = value;
                InertGasConnected = value;
                ChillerOn = value;
                BuildPlateInstalled = value;
                RecoaterKnifeSet = value;
                FirstLayerApplied = value;
            }
        }

        #region Commands

        public ICommand StartCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Result

        private bool _dialogResult;
        public bool DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        #endregion

        #region Constructor

        public PreparationChecklistViewModel()
        {
            StartCommand = new DelegateCommand(OnStart, CanStart).ObservesProperty(() => AllChecked);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        #endregion

        #region Command Handlers

        private bool CanStart()
        {
            return AllChecked;
        }

        private void OnStart()
        {
            DialogResult = true;
            Services.ModalService.Instance?.Close();
        }

        private void OnCancel()
        {
            DialogResult = false;
            Services.ModalService.Instance?.Close();
        }

        #endregion
    }
}
