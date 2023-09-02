#region

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

#endregion

namespace SpyderLib.Models;

/// <inheritdoc />
internal class ScrapedUri : INotifyPropertyChanged
{
    #region Events, Enums

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    private bool _crawled;
    private Uri? _urii;
    private string? _url;





    internal ScrapedUri(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(url));
        this.Url = url ?? "";
        this.Urii = new(uriString: this.Url);
    }





    [DataMember]
    internal bool Crawled
    {
        get => _crawled;
        set
        {
            if (value == _crawled)
            {
                return;
            }

            _crawled = value;
            OnPropertyChanged();
        }
    }

    [DataMember]
    internal Uri? Urii
    {
        get => _urii;
        set
        {
            if (Equals(objA: value, objB: _urii))
            {
                return;
            }

            _urii = value;
            OnPropertyChanged();
        }
    }

    [DataMember]
    internal string? Url
    {
        get => _url;
        set
        {
            if (value == _url)
            {
                return;
            }

            _url = value;
            OnPropertyChanged();
        }
    }

    #region Methods

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new(propertyName: propertyName));
    }





    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(x: field, y: value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName: propertyName);
        return true;
    }

    #endregion
}