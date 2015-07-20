using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;

namespace GitHubAPIClient
{
    public static class GitHubClient
    {
        #region private declarations
        private static string repository_owner = ConfigurationManager.AppSettings["repository_owner"].ToString();
        private static string repository_name = ConfigurationManager.AppSettings["repository_name"].ToString();
        private static string userAgent = ConfigurationManager.AppSettings["auth_username"].ToString();
        private static string auth_token = ConfigurationManager.AppSettings["auth_token"].ToString();
        private static string committer_name = ConfigurationManager.AppSettings["committer_name"].ToString();
        private static string committer_email = ConfigurationManager.AppSettings["committer_email"].ToString();
        private static string commit_message = ConfigurationManager.AppSettings["commit_message"].ToString();
        private static DateTimeZone timeZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        #endregion // private declarations

        #region public functions
        /// <summary>
        /// Ex. 
        ///     GitUserData user = GitHubAPIClient.GetUser();
        ///     Console.WriteLine("Name: {0}{1}Email: {2}", user.name, Environment.NewLine, user.email);
        /// </summary>
        /// <returns>Returns the authenticated user object</returns>
        public static GitUserData GetUser()
        {
            try
            {
                HttpWebRequest request = buildWebRequest("https://api.github.com/user");

                string jsonResult = getResponseStream(request);
                GitUserData userData = JsonConvert.DeserializeObject<GitUserData>(jsonResult);
                return userData;
            }
            catch (Exception ex)
            {
                //log.debug(ex);
                throw;
            }
        }

        /// <summary>
        /// Ex.
        ///     GitREADME readme = GitHubAPIClient.GetReadme();
        ///     Console.WriteLine(readme.name);
        /// </summary>
        /// <returns>Returns the Repository default README object</returns>
        public static GitREADME GetReadme()
        {
            try
            {
                // GET /repos/:owner/:repo/readme
                string url = string.Format("https://api.github.com/repos/{0}/{1}/readme", repository_owner, repository_name);
                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                // Submit request 
                string jsonResult = getResponseStream(request);

                // convert json to object
                GitREADME readme = JsonConvert.DeserializeObject<GitREADME>(jsonResult);

                //log.Debug("Retrieved README");
                return readme;
            }
            catch (WebException wex)
            {
                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    //log.Debug(wex.Message);
                    return null;
                }
                else
                {
                    //log.Error(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                //log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Ex.
        ///     Console.WriteLine(GitHubAPIClient.GetReadme().DecodedContent);
        /// </summary>
        /// <returns>Returns the text found in the Repository default README file</returns>
        public static string GetReadme_Content()
        {
            return GetReadme().DecodedContent;
        }

        /// <summary>
        /// Ex.
        ///     GitRateLimit rateLimit = GitHubAPIClient.GetRateLimit();
        ///     Console.WriteLine("Rate Limit: {0}{1}Rate Remaining: {2}", rateLimit.rate.limit, Environment.NewLine, rateLimit.rate.remaining);
        /// </summary>
        /// <returns>Returns the RateLimit object</returns>
        public static GitRateLimit GetRateLimit()
        {
            HttpWebRequest request = buildWebRequest("https://api.github.com/rate_limit");

            string jsonResult = getResponseStream(request);
            GitRateLimit rateLimit = JsonConvert.DeserializeObject<GitRateLimit>(jsonResult);
            return rateLimit;
        }

        /// <summary>
        /// Ex.
        ///     if (GitHubAPIClient.RateLimitExceeded())
        ///         Console.WriteLine("Rate limit hass exceeded allowed connections");
        /// </summary>
        /// <returns>Have you exceeded your allowed connections?</returns>
        public static bool RateLimitExceeded()
        {
            Rate rate = GetRateLimit().rate;
            return rate.remaining < 1;
        }

        /// <summary>
        /// Ex. 
        ///     GitContent content = GitHubAPIClient.GetContent("hello.txt");
        ///     Console.WriteLine(content.name); 
        /// </summary>
        /// <param name="ContentPath">The path of the file in the repository (ex. 'hello.txt' or 'folder/hello.txt')</param>
        /// <returns>The file object</returns>
        public static GitContent GetContent(string ContentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(ContentPath)) { throw new ArgumentNullException(); }

                // GET /repos/:owner/:repo/contents/:path
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", repository_owner, repository_name, ContentPath);

                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                // Submit request 
                string jsonResult = getResponseStream(request);

                // convert json to object
                GitContent content = JsonConvert.DeserializeObject<GitContent>(jsonResult);

                //log.DebugFormat("Retrieved content [{0}]", ContentPath);
                return content;
            }
            catch (WebException wex)
            {
                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    //log.Debug(wex.Message);
                    return null;
                }
                else
                {
                    //log.Error(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                // log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Ex. 
        ///     List<GitContent> contents = GitHubAPIClient.GetContents();
        ///     foreach (GitContent entry in contents)
        ///         Console.WriteLine("{0} [{1}] [{2}]", entry.name, entry.FileSize, entry.download_url);
        /// </summary>
        /// <returns>An array of all objects in the Repository</returns>
        public static List<GitContent> GetContents()
        {
            List<GitContent> contents = new List<GitContent>();
            try
            {
                // GET /repos/:owner/:repo/contents
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents", repository_owner, repository_name);

                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                // Submit request 
                string jsonResult = getResponseStream(request);

                //    // No obvious way to tell difference between a json result with a 
                //    // single entry result (non-array) or a json with multiple entries (array).  
                //    // This hack handles it for now...
                if (jsonResult.StartsWith("["))
                {
                    contents = JsonConvert.DeserializeObject<List<GitContent>>(jsonResult);
                }
                else
                {
                    GitContent content = JsonConvert.DeserializeObject<GitContent>(jsonResult);
                    contents.Add(content);
                }

                //log.DebugFormat("Retrieved {0} items from {1}", contents.count, ContentPath);
                return contents;
            }
            catch (WebException wex)
            {

                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    //log.Debug(wex.Message);
                    return null;
                }
                else
                {
                    //log.Error(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                // log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Ex. 
        ///     bool result = GitHubAPIClient.UploadContent("c:\\temp\\myfile.ps1");
        /// </summary>
        /// <param name="SourceFile">The full file path to upload (ex. c:\temp\hello.txt)</param>
        /// <returns>Did upload succeeded?</returns>
        public static bool UploadContent(string SourceFile)
        {
            return UploadContent(SourceFile, string.Empty);
        }

        /// <summary>
        /// Ex. 
        ///     bool result = GitHubAPIClient.UploadContent("c:\\temp\\myfile.ps1", "myfile.ps1");
        /// </summary>
        /// <param name="SourceFile">The full file path to upload (ex. c:\temp\hello.txt)</param>
        /// <param name="ContentPath">The path of the file in the repository (ex. 'hello.txt' or 'folder/hello.txt')</param>
        /// <returns>Did the upload succeed?</returns>
        public static bool UploadContent(string SourceFile, string ContentPath)
        {
            if (string.IsNullOrEmpty(SourceFile)) { throw new ArgumentNullException(); }

            // if ContentPath isn't specified then assume root and use the source file name
            if (string.IsNullOrEmpty(ContentPath)) { ContentPath = Path.GetFileName(SourceFile); }

            // See if we can find a file already in the hub
            GitContent content = GitHubClient.GetContent(ContentPath);
            if (content != null)
            {
                //log.DebugFormat("Updating existing file [{0}]", content.path);
                return UpdateFile(SourceFile, content);
            }

            //log.DebugFormat("Creating new file [{0}]", ContentPath);
            return CreateFile(SourceFile, ContentPath);

        }

        /// <summary>
        /// Ex.
        ///     GitHubAPIClient.DeleteContent("Hello.ps1")
        /// </summary>
        /// <param name="ContentPath"></param>
        /// <returns></returns>
        public static bool DeleteContent(string ContentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(ContentPath)) { throw new ArgumentNullException("Content path is required"); }

                // See if we can find a file already in the hub
                GitContent content = GitHubClient.GetContent(ContentPath);
                if (content == null)
                {
                    //log.DebugFormat("File not found [{0}]", ContentPath);
                    return false;            
                }

                // DELETE /repos/:owner/:repo/contents/:path
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", repository_owner, repository_name, content.path);

                #region Build Request
                HttpWebRequest request = buildWebRequest(method.DELETE, url);
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    GitDeleteFile deleteFile = new GitDeleteFile();
                    deleteFile.message = commit_message;
                    deleteFile.author = new GitAuthor("Chris B", "chris@dscoduc.com");  //TODO: Replace author info with authenticated user info
                    deleteFile.committer = new GitCommitter(committer_name, committer_email);
                    deleteFile.sha = content.sha;

                    // create json from object
                    string json = JsonConvert.SerializeObject(deleteFile, Formatting.None,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                #endregion // Build Request

                string jsonResult = getResponseStream(request);
                
                //log.DebugFormat("File deleted [{0}]", ContentPath);
                return true;

            }
            catch (FileNotFoundException fex)
            {
                //log.Debug(fex.message);
                return false;
            }
            catch (Exception ex)
            {
                //log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// 
        /// Ex. GitHubAPIClient.CreateFile("c:\\temp\\myfile.ps1", "myfile.ps1");
        /// </summary>
        /// <param name="SourceFile">The full file path to upload (ex. c:\temp\hello.txt)</param>
        /// <param name="ContentPath">The path of the file in the repository (ex. 'hello.txt' or 'folder/hello.txt')</param>
        /// <returns>Did the creation succeed?</returns>
        public static bool CreateFile(string SourceFile, string ContentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(SourceFile) || string.IsNullOrEmpty(ContentPath)) { throw new ArgumentNullException(); }

                // PUT /repos/:owner/:repo/contents/:path
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", repository_owner, repository_name, ContentPath);

                #region Build Request
                HttpWebRequest request = buildWebRequest(method.PUT, url);
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    GitCreateFile uploadFile = new GitCreateFile();
                    uploadFile.message = commit_message;
                    uploadFile.author = new GitAuthor("Chris B", "chris@dscoduc.com"); //TODO: Replace author info with authenticated user info 
                    uploadFile.committer = new GitCommitter(committer_name, committer_email);
                    uploadFile.content = Utils.EncodeFile(SourceFile);

                    // create json from object
                    string json = JsonConvert.SerializeObject(uploadFile, Formatting.None, 
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                #endregion // Build Request

                string jsonResult = getResponseStream(request);

                //log.DebugFormat("File created [{0}]", ContentPath);
                return true;
            }
            catch (Exception ex)
            {
                //log.debug(ex);
                return false;
            }
        }

        /// <summary>
        /// 
        /// Ex. GitHubAPIClient.UploadFile("c:\\temp\\myfile.ps1", "myfile.ps1");
        /// </summary>
        /// <param name="SourceFile">The full file path to upload (ex. c:\temp\hello.txt)</param>
        /// <param name="ContentPath">The path of the file in the repository (ex. 'hello.txt' or 'folder/hello.txt')</param>
        /// <returns>Did the update succeed?</returns>
        public static bool UpdateFile(string SourceFile, string ContentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(SourceFile) || string.IsNullOrEmpty(ContentPath)) { throw new ArgumentNullException(); }

                GitContent content = GitHubClient.GetContent(ContentPath);
                if (content == null)
                {
                    //log.DebugFormat("Unable to locate file [{0}]", ContentPath);
                    return false;
                }

                return UpdateFile(SourceFile, content);
            }
            catch (Exception ex)
            { 
                //log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SourceFile">The full file path to upload (ex. c:\temp\hello.txt)</param>
        /// <param name="Content">Content object</param>
        /// <returns>True/False</returns>
        public static bool UpdateFile(string SourceFile, GitContent Content)
        {
            try
            {
                if (string.IsNullOrEmpty(SourceFile) || Content == null) { throw new ArgumentNullException(); }

                // PUT /repos/:owner/:repo/contents/:path
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", repository_owner, repository_name, Content.path);

                #region Build Request
                HttpWebRequest request = buildWebRequest(method.PUT, url);
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    GitUpdateFile updateFile = new GitUpdateFile();
                    updateFile.message = commit_message;
                    updateFile.author = new GitAuthor("Chris B", "chris@dscoduc.com");  //TODO: Replace author info with authenticated user info
                    updateFile.committer = new GitCommitter(committer_name, committer_email);
                    updateFile.content = Utils.EncodeFile(SourceFile);
                    updateFile.sha = Content.sha;

                    // create json from object
                    string json = JsonConvert.SerializeObject(updateFile, Formatting.None, 
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
                #endregion // Build Request

                string jsonResult = getResponseStream(request);

                //log.DebugFormat("File updated [{0}]", ContentPath);
                return true;
            }
            catch (FileNotFoundException fex)
            {
                //log.Debug(fex.message);
                return false;
            }
            catch (Exception ex)
            {
                //log.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// Ex.
        ///     Console.WriteLine(GitHubAPIClient.GetFileContents("hello.txt"));
        /// </summary>
        /// <param name="ContentPath">The path of the file in the repository (ex. 'hello.txt' or 'folder/hello.txt')</param>
        /// <returns>Plain text output of the Base64 encoded contents of the requested file</returns>
        public static string GetFileContents(string ContentPath)
        {
            if (string.IsNullOrEmpty(ContentPath)) { throw new ArgumentNullException(); }

            GitContent content = GitHubClient.GetContent(ContentPath);
            if (content == null)
                return string.Empty;
            
            return GetFileContents(content);
        }

        /// <summary>
        /// Ex.
        ///     Console.Write(GitHubAPIClient.GetFileContents(Content));
        /// </summary>
        /// <param name="Content">Content object</param>
        /// <returns>Plain text output of the Base64 encoded contents of the requested file</returns>
        public static string GetFileContents(GitContent Content)
        {
            if (Content == null) { throw new ArgumentNullException(); }

            return Utils.Base64Decode(Content.content);
        }
        #endregion // public functions

        #region private web functions
        /// <summary>
        /// Builds the web request with pre-set properties needed for GitHub
        /// using the default GET request method
        /// </summary>
        /// <param name="requestURL">The URL to perform the request against</param>
        /// <returns>Prepared request object</returns>
        private static HttpWebRequest buildWebRequest(string requestURL)
        {
            return buildWebRequest(method.GET, requestURL);
        }

        /// <summary>
        /// Builds the web request with pre-set properties needed for GitHub
        /// </summary>
        /// <param name="requestMethod">a http request method type (ex. GET, PUT, POST)</param>
        /// <param name="requestURL">The URL to perform the request against</param>
        /// <returns>Prepared request object</returns>
        private static HttpWebRequest buildWebRequest(method requestMethod, string requestURL = "")
        {
            if (string.IsNullOrEmpty(requestURL)) { throw new ArgumentNullException("Must provide request URL"); }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
            request.Headers.Add("Time-Zone", timeZone.ToString());  // Provide web server timezone
            request.Method = requestMethod.ToString();
            request.ContentType = "text/json";  // everything we're doing here is json based
            request.UserAgent = userAgent;  // Must be authenticated username or repository
            request.Headers.Add("authorization: token " + auth_token);

            return request;
        }

        /// <summary>
        /// Performs the web request
        /// </summary>
        /// <param name="request">a valid HttpWebRequest object</param>
        /// <returns>returns back the response stream</returns>
        private static string getResponseStream(HttpWebRequest request)
        {
            if ((request == null)) { throw new ArgumentNullException("An empty request object was passed"); }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// HTTPWebRequest Methods
        /// </summary>
        private enum method
        {
            GET, POST, PUT, HEAD, DELETE
        }
        #endregion // private web functions

    }
}
