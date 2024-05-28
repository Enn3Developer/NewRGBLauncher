using ReactiveUI;

namespace NewRGB.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public virtual void OnClose()
    {
    }
}