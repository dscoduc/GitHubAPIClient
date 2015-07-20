using GitHubAPIClient;
using System;
using System.IO;

namespace GitHubAPIClient_SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (GitHubClient.RateLimitExceeded())
            {
                Console.WriteLine("Rate limit has been exceeded - terminating...");
            }
            else
            {
                Console.WriteLine(GitHubClient.GetReadme_Content());

                string testFileNamePath = "c:\\temp\\test-signed.ps1";
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
    }
}
