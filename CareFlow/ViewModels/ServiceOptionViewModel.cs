using CommunityToolkit.Mvvm.ComponentModel;

namespace CareFlow.ViewModels;

public partial class ServiceOptionViewModel(int id, string name, int capacity, int remaining) : ObservableObject
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int Capacity { get; } = capacity;
    [ObservableProperty] private int _remaining = remaining;
    [ObservableProperty] private bool _isSelected;
    public string CapacityText => $"{Remaining} of {Capacity} spots left";
    public bool CanSelect => Remaining > 0;
    partial void OnRemainingChanged(int value)
    {
        OnPropertyChanged(nameof(CapacityText));
        OnPropertyChanged(nameof(CanSelect));
        if (value <= 0) IsSelected = false;
    }
}
