using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PreloaderConfigurator.ViewModels;

/// The ViewModelBase class serves as a base class for view models and provides
/// functionality to notify property value changes, enabling data binding in MVVM applications.
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// Notifies listeners that a property value has changed.
    /// <param name="propertyName">The name of the property that has changed. This parameter is optional and defaults to the caller member name.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// Updates the specified field with a new value and notifies listeners that the associated property value has changed.
    /// <typeparam name="T">The type of the field being updated.</typeparam>
    /// <param name="field">A reference to the field to be updated.</param>
    /// <param name="value">The new value to set for the field.</param>
    /// <param name="propertyName">The name of the property associated with the field, used for notification. This parameter is optional and defaults to the caller member name.</param>
    /// <returns>True if the field was updated (i.e., the new value differs from the current value); otherwise, false.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}