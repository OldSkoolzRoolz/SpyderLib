using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using CommunityToolkit.Diagnostics;

using KC.Apps.SpyderLib.Properties;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;



namespace KC.Apps.SpyderLib.Services;

/// <summary>
///     Base class for services that implement the INotifyPropertyChanged interface.
/// </summary>
public abstract class ServiceBase : INotifyPropertyChanged
{
    protected ServiceBase() { }






    /// <summary>
    ///     Constructor used by the control service alone, to encourage a single point of failure in parameters.
    /// </summary>
    /// <param name="lifetime"></param>
    protected ServiceBase(IHostApplicationLifetime lifetime)
        {
            Guard.IsNotNull(lifetime);



            this.AppLifetime = lifetime;
        }






    #region Properteez

    public IHostApplicationLifetime AppLifetime { get; }
    protected static ILoggerFactory Factory => AppContext.GetData("factory") as ILoggerFactory;
    public static SpyderOptions Options => (SpyderOptions)AppContext.GetData("options");

    #endregion






    #region Public Methods

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion






    #region Private Methods

    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs" /> instance containing the event data.</param>
    private void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                {
                    handler(this, e);
                }
        }






    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event.
    /// </summary>
    /// <param name="propertyName">
    ///     The property name of the property that has changed.
    ///     This optional parameter can be skipped because the compiler is able to create it automatically.
    /// </param>
    [SuppressMessage("Design", "CA1030:Use events where appropriate")]
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            OnPropertyChanged(new(propertyName));
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
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                {
                    return false;
                }

            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

    #endregion
}