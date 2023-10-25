namespace KC.Apps.SpyderLib.Interfaces;

public interface IMyClient
{
    Task<string> GetContentFromWebAsync(string address);




    Task<string> GetStringAsync(string requestUri);
}