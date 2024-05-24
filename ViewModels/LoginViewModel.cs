using System.Reactive;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private bool _visibleMethod = true;
    private bool _visibleOffline;

    public LoginViewModel()
    {
        OfflineAuth = ReactiveCommand.Create(() =>
        {
            VisibleOffline = true;
            VisibleMethod = false;
        });
    }

    public bool VisibleOffline
    {
        get => _visibleOffline;
        private set => this.RaiseAndSetIfChanged(ref _visibleOffline, value);
    }

    public bool VisibleMethod
    {
        get => _visibleMethod;
        private set => this.RaiseAndSetIfChanged(ref _visibleMethod, value);
    }

    public ReactiveCommand<Unit, Unit> OfflineAuth { get; }
}