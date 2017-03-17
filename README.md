# CNN Scraper
A web scraper that scrapes, indexes, and searches cnn.com and bleacherreport.com

## `FullSearcher`
This folder contains the C# code for the web crawler, admin dashboard, and search page. This runs as a Microsoft Azure cloud service (Although the Cloud Service project has been removed). The web.config and app.config have both been removed and need to be created if you want to run the app locally.

### Web Crawler
The web crawler pulls URL's off of the AzureCloudQueue and first checks to see if they're valid HTML files and then downloads them. After downloading it gathers the page title and all links from the page. The title and URL gets stored in a database, and all the scraped links are added to the AzureCloudQueue.

The crawler is setup so that multiple instances of the cloud service can run simultaneously to speed up processing time.
### Search Page
The search page is the main interface that users see when interacting with the CNN Scraper. This page lets users search for CNN and bleacher report articles or pages. It useses a full listing of wikipedia titles to populate the search suggestions. The list of results come from the titles that were scraped in the web crawler.
### Admin Dashboard
The admin dashboard (which would normally be behind a secure login) provides controls for the web crawler. The main things that the dashboard does is add cnn.com/robots.txt and/or bleacherreport.com/robots.txt to the processing queue. It also allows pausing/resuming of the web crawler. It also displays statistics such as # URL's crawled, errors, and the status of each worker.

## `NBA_PLAYER_SEARCHER`
This folder contains the PHP code for searching for NBA players. This searches exact matches and returns back JSONP that contains the resulting NBA player. This runs on an AWS EC2 instance and interfaces with an RDS instance containing the players.