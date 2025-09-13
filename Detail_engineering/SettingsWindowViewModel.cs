using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Input;
//using System.Windows.Input;

namespace Detail_engineering
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        { _execute = execute; _canExecute = canExecute; }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged { add { } remove { } }
    }

    public class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private string _jsonPath = Settings.Default.JsonPath ?? "";
        public string JsonPath
        {
            get => _jsonPath;
            set { _jsonPath = value; OnPropertyChanged(); }
        }

        private string _baseFolder = Settings.Default.BaseFolder ?? "";
        
        public string BaseFolder
        {
            get => _baseFolder;
            set { _baseFolder = value; OnPropertyChanged(); }
        }

        public ICommand BrowseJsonCommand { get; }
        public ICommand BrowseBaseCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler CloseRequested;

        public SettingsWindowViewModel()
        {
            BrowseJsonCommand = new RelayCommand(_ => BrowseJson());
            BrowseBaseCommand = new RelayCommand(_ => BrowseBase());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));
        }

        private void BrowseJson()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() != true)
                return;
            JsonPath = dlg.FileName;
        }

        private void BrowseBase()
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK) BaseFolder = dlg.SelectedPath;
        }

        private void Save()
        {
            Settings.Default.JsonPath = (JsonPath ?? "").Trim();
            Settings.Default.BaseFolder = (BaseFolder ?? "").Trim();
            Settings.Default.Save();

            // اعمال فوری روی برنامه (مثلاً PathHelper)
            PathHelper.BaseDir = Settings.Default.BaseFolder ?? "";

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
