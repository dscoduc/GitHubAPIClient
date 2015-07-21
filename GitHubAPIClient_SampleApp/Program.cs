using GitHubAPIClient;
using log4net;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace GitHubAPIClient_SampleApp
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            log.Info("Application is starting with the following application settings");

            // Write all app settings except auth_key to debug log
            foreach (string key in ConfigurationManager.AppSettings)
            {
                // skip auth token from writing to log
                if (key != "auth_token")
                    log.InfoFormat("{0}: '{1}'", key, ConfigurationManager.AppSettings[key]);
            }

            //TODO: Remove before flight
            // string filename = "c:\\temp\\test-signed.ps1";

            string filename = string.Empty;

            try
            {
                // get filename from startup arg
                if (args.Length > 0)
                    filename = args[0].ToString();
                
                if (GitHubClient.RateLimitExceeded())
                {
                    Console.WriteLine("Rate limit has been exceeded - terminating...");
                }
                else
                {
                    Console.WriteLine(GitHubClient.GetReadme_Content());

                    string testFileNamePath = filename; // ;
                    string testFileName = Path.GetFileName(testFileNamePath);

                    ///Update an existing file
                    string updateResult = (GitHubClient.UploadContent(testFileNamePath)) ? "File uploaded" : "File not uploaded";
                    Console.WriteLine(updateResult);

                    // Wait for user input - keep the program running
                    Console.WriteLine(Environment.NewLine + "Press any key to delete file"); Console.ReadKey();
                    string deleteResult = (GitHubClient.DeleteContent(testFileName)) ? "File deleted" : "File not deleted";
                    Console.WriteLine(deleteResult);

                    // Wait for user input - keep the program running
                    Console.WriteLine(Environment.NewLine + "Press any key to quit");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(Environment.NewLine + "Press any key to quit"); Console.ReadKey();
                return;
            }

            log.Debug("Application is exiting");
        }
    }
}
