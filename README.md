# SpyderLibrary

The Spyder Library is a portable lightweight network crawler and parser. Spyder can search the page for specific html
tags and records the URL to process later. Can be implemented as a service and use the built in console menu or drop it
in an app of your choice. Very flexible and can respect the robots rule file and employs a throttling mechanism to
ensure polite crawling protocols. Methods are optimized for multi-threaded speed and safety. 

Scraper One is a cross-plaform gui crawler using the Spyder Library as it's engine.

**Key features:**

- Operation is controlled by set of options passed in
- Set depth level of crawler
- Can filter out urls based on exclusion patterns
- Output captured urls to file
- process input file urls
- Search for html tags in pages
- Can run as service in a host
- Cab be dropped in a console app
- Can be used to download binary fields
- Multi-threaded for speed

<code>SpyderOptions options = new<br>
{<br>
   StartingUrl="https://www.google.com",<br>
   ScraperDepthLevel = 2<br>
};<br>

SpyderControl control = new(options);<br>
control.Initialize();<br>
   await control.BeginCrawling();<br>
<code/>
