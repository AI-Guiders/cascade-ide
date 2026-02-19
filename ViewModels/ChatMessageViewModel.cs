using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public sealed partial class ChatMessageViewModel : ObservableObject
{
    public string Role { get; }

    [ObservableProperty]
    private string _content;

    public ChatMessageViewModel(string role, string content)
    {
        Role = role;
        _content = content;
    }
}
