#region

using Microsoft.Extensions.Logging;

#endregion

namespace SpyderLib.Control;

#pragma warning disable CS8600
/// <summary>
/// </summary>
public interface ISpyderControl
{
    /// <summary>
    /// </summary>
    Task? ExecuteTask { get; }

    /// <summary>
    /// </summary>
    ILoggerFactory Factory { get; set; }

    #region Methods

    /// <summary>
    /// </summary>
    /// <param name="url"></param>
    /// <param name="outputFileName"></param>
    /// <returns></returns>
    Task BeginSpyder(string url, string outputFileName);





    /// <summary>
    /// </summary>
    void Dispose();





    Task ScrapeSingleSite(string address = "");

    #endregion
}