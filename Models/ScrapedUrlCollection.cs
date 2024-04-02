//

using System.Collections.Generic;
using System.Linq;

namespace KC.Apps.SpyderLib.Models;


public class ScrapedUrlCollection
{

    private HashSet<Uri> _allUrls { get; set; }


    public Uri StartingUrl { get; set; }
    public IEnumerable<Uri> BaseUrls { get; set; }
    public IEnumerable<Uri> OtherUrls { get; set; }




    public void AddUrl(string newUrl)
    {


        if (!string.IsNullOrEmpty(newUrl) && IsValidUrl(newUrl))
        {
            _ = TryAdd(newUrl);
        }
    }




        /// <summary>
        /// Determines whether the given URL is valid.
        /// </summary>
        /// <param name="newUrl">The URL to be validated.</param>
        /// <returns>True if the URL is valid, false otherwise.</returns>
    private static bool IsValidUrl(string newUrl)
    {
        return Uri.IsWellFormedUriString(newUrl, UriKind.Absolute)
               && Uri.TryCreate(newUrl, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }


    public void AddUrls(IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            AddUrl(url);
        }
    }

    private bool TryAdd(string url)
    {

        var success = Uri.TryCreate(url, UriKind.Absolute, out var uriResult);

        if (uriResult != null && success)
        {
            _ = _allUrls.Append(uriResult);
        }
        return success;
    }





    public void AddRange(IEnumerable<string> urls)
    {

    }

    public void AddUrl(Uri newUrl)
    {
        throw new NotImplementedException();
    }
}