namespace SpyderLib.Models;

internal class CrawlerStartEventArgs
{
    internal CrawlerStartEventArgs(string msg, string workingCount, string newLinks)
    {
        this.Message = string.Empty;
        this.NewLinks = string.Empty;
        this.WorkingCount = string.Empty;
        this.Message = msg;
        this.WorkingCount = workingCount;
        this.NewLinks = newLinks;
    }





    internal string Message { get; }

    //        set => this.RaiseAndSetIfChanged(backingField: ref _message, newValue: value);
    internal string NewLinks { get; }

    //       set => this.RaiseAndSetIfChanged(backingField: ref _newLinks, newValue: value);
    internal string WorkingCount { get; }

    //       set => this.RaiseAndSetIfChanged(backingField: ref _workingCount, newValue: value);
}