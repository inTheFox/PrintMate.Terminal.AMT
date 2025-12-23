using Hans.NET.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Windows;

namespace LaserConfigurator.ViewModels
{
    public class EditMarkSpeedProfileViewModel : BindableBase
    {
        private ProcessVariables _profile;
        public ProcessVariables Profile
        {
            get => _profile;
            set => SetProperty(ref _profile, value);
        }

        // Свойства для биндинга
        public int MarkSpeed
        {
            get => Profile?.MarkSpeed ?? 0;
            set { if (Profile != null) Profile.MarkSpeed = value; RaisePropertyChanged(); }
        }

        public int JumpSpeed
        {
            get => Profile?.JumpSpeed ?? 0;
            set { if (Profile != null) Profile.JumpSpeed = value; RaisePropertyChanged(); }
        }

        public int PolygonDelay
        {
            get => Profile?.PolygonDelay ?? 0;
            set { if (Profile != null) Profile.PolygonDelay = value; RaisePropertyChanged(); }
        }

        public int JumpDelay
        {
            get => Profile?.JumpDelay ?? 0;
            set { if (Profile != null) Profile.JumpDelay = value; RaisePropertyChanged(); }
        }

        public int MarkDelay
        {
            get => Profile?.MarkDelay ?? 0;
            set { if (Profile != null) Profile.MarkDelay = value; RaisePropertyChanged(); }
        }

        public double LaserOnDelay
        {
            get => Profile?.LaserOnDelay ?? 0;
            set { if (Profile != null) Profile.LaserOnDelay = value; RaisePropertyChanged(); }
        }

        public double LaserOffDelay
        {
            get => Profile?.LaserOffDelay ?? 0;
            set { if (Profile != null) Profile.LaserOffDelay = value; RaisePropertyChanged(); }
        }

        public double LaserOnDelayForSkyWriting
        {
            get => Profile?.LaserOnDelayForSkyWriting ?? 0;
            set { if (Profile != null) Profile.LaserOnDelayForSkyWriting = value; RaisePropertyChanged(); }
        }

        public double LaserOffDelayForSkyWriting
        {
            get => Profile?.LaserOffDelayForSkyWriting ?? 0;
            set { if (Profile != null) Profile.LaserOffDelayForSkyWriting = value; RaisePropertyChanged(); }
        }

        public double CurBeamDiameterMicron
        {
            get => Profile?.CurBeamDiameterMicron ?? 0;
            set { if (Profile != null) Profile.CurBeamDiameterMicron = value; RaisePropertyChanged(); }
        }

        public double CurPower
        {
            get => Profile?.CurPower ?? 0;
            set { if (Profile != null) Profile.CurPower = value; RaisePropertyChanged(); }
        }

        public double JumpMaxLengthLimitMm
        {
            get => Profile?.JumpMaxLengthLimitMm ?? 0;
            set { if (Profile != null) Profile.JumpMaxLengthLimitMm = value; RaisePropertyChanged(); }
        }

        public int MinJumpDelay
        {
            get => Profile?.MinJumpDelay ?? 0;
            set { if (Profile != null) Profile.MinJumpDelay = value; RaisePropertyChanged(); }
        }

        public bool Swenable
        {
            get => Profile?.Swenable ?? false;
            set { if (Profile != null) Profile.Swenable = value; RaisePropertyChanged(); }
        }

        public double Umax
        {
            get => Profile?.Umax ?? 0;
            set { if (Profile != null) Profile.Umax = value; RaisePropertyChanged(); }
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public Action CloseAction { get; set; }
        public bool DialogResult { get; private set; }

        public EditMarkSpeedProfileViewModel()
        {
            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
        }

        public void SetProfile(ProcessVariables profile)
        {
            Profile = profile;
            RaisePropertyChanged(nameof(MarkSpeed));
            RaisePropertyChanged(nameof(JumpSpeed));
            RaisePropertyChanged(nameof(PolygonDelay));
            RaisePropertyChanged(nameof(JumpDelay));
            RaisePropertyChanged(nameof(MarkDelay));
            RaisePropertyChanged(nameof(LaserOnDelay));
            RaisePropertyChanged(nameof(LaserOffDelay));
            RaisePropertyChanged(nameof(LaserOnDelayForSkyWriting));
            RaisePropertyChanged(nameof(LaserOffDelayForSkyWriting));
            RaisePropertyChanged(nameof(CurBeamDiameterMicron));
            RaisePropertyChanged(nameof(CurPower));
            RaisePropertyChanged(nameof(JumpMaxLengthLimitMm));
            RaisePropertyChanged(nameof(MinJumpDelay));
            RaisePropertyChanged(nameof(Swenable));
            RaisePropertyChanged(nameof(Umax));
        }

        private void Save()
        {
            DialogResult = true;
            CloseAction?.Invoke();
        }

        private void Cancel()
        {
            DialogResult = false;
            CloseAction?.Invoke();
        }
    }
}
