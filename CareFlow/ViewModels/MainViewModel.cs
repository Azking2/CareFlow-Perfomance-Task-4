using System.Collections.ObjectModel;
using CareFlow.Data;
using CareFlow.Models;
using CareFlow.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using BookingManager = CareFlow.Services.BookingService;

namespace CareFlow.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly BookingManager _bookingService;
    private readonly IDbContextFactory<CareFlowDbContext> _contextFactory;

    public ObservableCollection<Patient> Patients { get; } = [];
    public ObservableCollection<ServiceOptionViewModel> Services { get; } = [];
    public ObservableCollection<BookingListItem> Bookings { get; } = [];

    [ObservableProperty] private Patient? _selectedPatient;
    [ObservableProperty] private DateTimeOffset _bookingDate = DateTimeOffset.Now.Date;
    [ObservableProperty] private BookingListItem? _selectedBooking;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Loading CareFlow...";

    public string SelectedBookingDetails => SelectedBooking is null
        ? "Choose a booking to see its patient, date, and services."
        : $"Patient: {SelectedBooking.PatientName}\nDate: {SelectedBooking.Date:MMMM d, yyyy}\nServices: {SelectedBooking.Services}";

    public MainViewModel(BookingManager bookingService, IDbContextFactory<CareFlowDbContext> contextFactory)
    {
        _bookingService = bookingService;
        _contextFactory = contextFactory;
        _ = InitializeAsync();
    }

    partial void OnSelectedBookingChanged(BookingListItem? value) => OnPropertyChanged(nameof(SelectedBookingDetails));

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await using (var db = await _contextFactory.CreateDbContextAsync())
                await db.Database.MigrateAsync();
            Patients.Clear();
            foreach (var patient in await _bookingService.GetPatientsAsync()) Patients.Add(patient);
            SelectedPatient = Patients.FirstOrDefault();
            await RefreshAvailabilityAsync();
            await RefreshBookingsAsync();
            StatusMessage = "Ready. Choose a patient, date, and one to three services.";
        }
        catch (Exception ex) { StatusMessage = $"Could not start CareFlow: {FriendlyError(ex)}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshAvailabilityAsync()
    {
        try
        {
            var availability = await _bookingService.GetAvailabilityAsync(DateOnly.FromDateTime(BookingDate.LocalDateTime));
            var current = Services.ToDictionary(x => x.Id);
            Services.Clear();
            foreach (var item in availability)
            {
                var option = new ServiceOptionViewModel(item.ServiceId, item.Name, item.DailyCapacity, item.Remaining);
                if (current.TryGetValue(item.ServiceId, out var old)) option.IsSelected = old.IsSelected;
                Services.Add(option);
            }
        }
        catch (Exception ex) { StatusMessage = $"Could not load availability: {FriendlyError(ex)}"; }
    }

    [RelayCommand]
    private async Task CreateBookingAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (SelectedPatient is null) throw new InvalidOperationException("Select a patient first.");
            var date = DateOnly.FromDateTime(BookingDate.LocalDateTime);
            var selectedIds = Services.Where(x => x.IsSelected).Select(x => x.Id).ToList();
            await _bookingService.CreateBookingAsync(SelectedPatient.Id, date, selectedIds);
            StatusMessage = $"Booking confirmed for {SelectedPatient.FullName} on {date:MMM d, yyyy}.";
            foreach (var service in Services) service.IsSelected = false;
            await RefreshAvailabilityAsync();
            await RefreshBookingsAsync();
        }
        catch (Exception ex)
        {
            var message = FriendlyError(ex);
            await RefreshAvailabilityAsync();
            StatusMessage = $"Booking not saved: {message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RefreshBookingsAsync()
    {
        try
        {
            Bookings.Clear();
            foreach (var booking in await _bookingService.GetBookingsAsync()) Bookings.Add(booking);
        }
        catch (Exception ex) { StatusMessage = $"Could not load bookings: {FriendlyError(ex)}"; }
    }

    private static string FriendlyError(Exception ex) => ex is InvalidOperationException
        ? ex.Message
        : "A database error stopped the operation. No booking was confirmed.";
}
