using NewRGB.Data;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private uint _minMemory = DataManager.Instance.Settings.MinMemory;
    private uint _maxMemory = DataManager.Instance.Settings.MaxMemory;

    public uint MaxMemoryValue => Settings.MaxMemoryValue;
    public uint MinMemoryValue => Settings.MinMemoryValue;

    public uint MinMemory
    {
        get => _minMemory;
        set => this.RaiseAndSetIfChanged(ref _minMemory, value);
    }


    public uint MaxMemory
    {
        get => _maxMemory;
        set => this.RaiseAndSetIfChanged(ref _maxMemory, value);
    }
}