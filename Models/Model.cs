#region

using System.ComponentModel;
using System.Runtime.CompilerServices;

#endregion

namespace KC.Apps.SpyderLib.Models;

/// <summary>
///     Represents a class that provides property change notifications.
/// </summary>
/// <remarks>
///     This class provides a base implementation of the <see cref="INotifyPropertyChanged" /> interface and exposes a
///     PropertyChanged event to notify clients when a property value changes.
/// </remarks>
public abstract class Model : INotifyPropertyChanged
{
    #region Interface Members

    /// <summary>
    ///     Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion

    #region Private Methods

    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">Optional parameter. If not given or null, the name of the calling member will be used.</param>
    protected virtual void OnPropertyChanged(
        [CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new(propertyName: propertyName));
        }





    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
    protected virtual void OnPropertyChanged(
        PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            handler?.Invoke(this, e: e);
        }





    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///     The property name of the property that has changed.
    ///     This optional parameter can be skipped because the compiler is able to create it automatically.
    /// </param>
    private void RaisePropertyChanged(
        [CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName: propertyName));
        }





    /// <summary>
    ///     Checks if the given field does not already equal the new value, updates the field with the new value and then
    ///     returns true.
    ///     Raises the <see cref="PropertyChanged" /> event with the name of the changed property.
    ///     Does not do anything and returns false if the field already equals the new value.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the field. This type parameter is contravariant. Specify the type you want the compiler
    ///     to accept.
    /// </typeparam>
    /// <param name="field">The field you want to update and compare to <paramref name="value" />.</param>
    /// <param name="value">The new value you want to check and assign to <paramref name="field" /></param>
    /// <param name="propertyName">Optional parameter. If not given or null, the name of the calling member will be used.</param>
    /// <returns>
    ///     Return true if the field was updated (field and value were not equal), otherwise false (field and value were
    ///     equal).
    /// </returns>
    protected bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(x: field, y: value))
                {
                    return false;
                }

            field = value;
            OnPropertyChanged(propertyName: propertyName);


            return true;
        }





    /// <summary>
    ///     Set the property with the specified value. If the value is not equal with the field then the field is
    ///     set, a PropertyChanged event is raised and it returns true.
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    /// <param name="field">Reference to the backing field of the property.</param>
    /// <param name="value">The new value for the property.</param>
    /// <param name="propertyName">
    ///     The property name. This optional parameter can be skipped
    ///     because the compiler is able to create it automatically.
    /// </param>
    /// <returns>True if the value has changed, false if the old and new value were equal.</returns>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string propertyName = null)
        {
            if (Equals(objA: field, objB: value))
                {
                    return false;
                }

            field = value;
            RaisePropertyChanged(propertyName: propertyName);


            return true;
        }

    #endregion
}