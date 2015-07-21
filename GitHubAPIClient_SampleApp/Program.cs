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
            log.Info("Application is starting");

            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key != "auth_token")
                    log.DebugFormat("{0} : {1}", key, ConfigurationManager.AppSettings[key.ToString()].ToString());
            }

            string filename = "c:\\temp\\test-signed.ps1";
            if(args.Length > 0)
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

            log.Debug("Application is exiting");
        }
    }
}
