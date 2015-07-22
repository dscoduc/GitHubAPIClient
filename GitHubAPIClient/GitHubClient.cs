using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Caching;

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
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //private static ObjectCache memoryCache = MemoryCache.Default;
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
                log.Info("Retrieving authenticated user");
                HttpWebRequest request = buildWebRequest("https://api.github.com/user");

                string jsonResult = getResponse(request);

                GitUserData userData = JsonConvert.DeserializeObject<GitUserData>(jsonResult);
                return userData;
            }
            catch (Exception ex)
            {
                log.Debug(ex);
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
                log.Info("Requesting the default README from the Repository");

                // GET /repos/:owner/:repo/readme
                string url = string.Format("https://api.github.com/repos/{0}/{1}/readme", repository_owner, repository_name);

                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                // Submit request 
                string jsonResult = getResponse(request);
                
                // convert json to object
                GitREADME readme = JsonConvert.DeserializeObject<GitREADME>(jsonResult);

                return readme;
            }
            catch (WebException wex)
            {
                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    log.Info(wex.Message);
                    return null;
                }
                else
                {
                    log.Warn(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
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
            try
            {
                log.Info("Requesting the Rate Limit from GitHub");

                HttpWebRequest request = buildWebRequest("https://api.github.com/rate_limit");

                string jsonResult = getResponse(request);

                GitRateLimit rateLimit = JsonConvert.DeserializeObject<GitRateLimit>(jsonResult);
                return rateLimit;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
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

                log.Info("Requesting the content of a file");

                // GET /repos/:owner/:repo/contents/:path
                string url = string.Format("https://api.github.com/repos/{0}/{1}/contents/{2}", repository_owner, repository_name, ContentPath);

                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                // Submit request 
                string jsonResult = getResponse(request);

                // convert json to object
                GitContent content = JsonConvert.DeserializeObject<GitContent>(jsonResult);

                return content;
            }
            catch (WebException wex)
            {
                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    log.Info(wex.Message);
                    return null;
                }
                else
                {
                    log.Warn(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Ex. 
        ///     List<GitContent> contents = GitHubClient.GetContents();
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

                log.Info("Requesting a list of all files in the Repository");

                // Build request
                HttpWebRequest request = buildWebRequest(method.GET, url);

                //// check if it's in cache already
                //object cacheData = Utils.GetCache(url);
                //if (cacheData != null)
                //{
                //    // load from cache
                //    gitResponse = (GitResponse)cacheData;

                //    // grab etag and add into request
                //    request.Headers.Add("If-None-Match", gitResponse.GetETAG);

                //    // perform the request
                //    GitResponse checkResponse = getResponse(request);

                //    // if request has been modified then replace gitResponse with recent response
                //    if (checkResponse.GetStatus != "304 Not Modified")
                //    {
                //        log.Info("Cached data is stale - updating memory cache with latest and greatest");
                        
                //        // looks like our cached data is stale
                //        gitResponse = checkResponse;

                //        // better add it into cache for next time
                //        Utils.AddCache(url, checkResponse);
                //    }
                //}
                //else
                //{
                //    // not in cache, let's load some fresh data...
                //    gitResponse = getResponse(request);
                    
                //    // ...and store it in cache for next time
                //    addCache(url, gitResponse);
                //}

                string jsonResult = getResponse(request);

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

                log.InfoFormat("Returning {0} content items", contents.Count);
                return contents;
            }
            catch (WebException wex)
            {

                if ((wex.Response).Headers["status"] == "404 Not Found")
                {
                    log.Warn(wex.Message);
                    return null;
                }
                else
                {
                    log.Warn(wex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                throw;
            }
        }

        public static void addCache(string cacheKey, object cacheData)
        {
            ObjectCache cache = MemoryCache.Default;
            CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddHours(12.0) };
            cache.Add(cacheKey, cacheData, policy);

        }

        public static object getCache(string cacheKey)
        {
            ObjectCache memoryCache = MemoryCache.Default;
            object cacheData = (object)memoryCache.Get(cacheKey);
            if (cacheData != null)
            {
                log.InfoFormat("Data found for memory cache key [{0}]", cacheKey);
                return cacheData;
            }
            else
            {
                log.InfoFormat("no Cache data found for cache key [{0}]", cacheKey);
                return null;
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

            log.Info("Processing a request to Create/Update a file in the Repository");

            // See if we can find a file already in the hub
            GitContent content = GitHubClient.GetContent(ContentPath);
            if (content != null)
            {
                log.Info("An existing file was found in the Repository");
                return UpdateFile(SourceFile, content);
            }

            log.Info("No existing file was found in the Repository");
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

                log.Info("Requesting the deletion of a file");

                // See if we can find a file already in the hub
                GitContent content = GitHubClient.GetContent(ContentPath);
                if (content == null)
                    return false;            

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

                string jsonResult = getResponse(request);
                
                log.Info("File successfully deleted from Repository");
                return true;

            }
            catch (FileNotFoundException fex)
            {
                log.WarnFormat("File NOT deleted from Repository - {0}", fex.Message);
                return false;
            }
            catch (Exception ex)
            {
                log.Error(ex);
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

                log.Info("Requesting the creation of a new file");

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

                string jsonResult = getResponse(request);

                log.Info("File sucessfully created in Repository");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex);
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
                    log.Warn("Aborting Update due to missing file");
                    return false;
                }

                return UpdateFile(SourceFile, content);
            }
            catch (Exception ex)
            { 
                log.Error(ex);
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

                log.Info("Requesting an update to a file in the Repository");

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

                string jsonResult = getResponse(request);

                log.Info("File sucessfully updated in the Repository");
                return true;
            }
            catch (FileNotFoundException fex)
            {
                log.Warn(fex.Message);
                return false;
            }
            catch (Exception ex)
            {
                log.Error(ex);
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
            {
                log.Warn("No file matching the request was found in the Repository");
                return string.Empty;
            }
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
        private static HttpWebRequest buildWebRequest(method requestMethod, string requestURL)
        {
            if (string.IsNullOrEmpty(requestURL)) { throw new ArgumentNullException("Must provide request URL"); }

            log.InfoFormat("Request: {0} [{1}]", requestMethod, requestURL);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestURL);
            request.Method = requestMethod.ToString();
            request.ContentType = "text/json";  // everything we're doing here is json based
            request.UserAgent = userAgent;  // GitHub requires userAgent be your username or repository
            request.Accept = "*/*";
            request.Headers.Add("authorization: token " + auth_token);

            return request;
        }

        ///// <summary>
        ///// Performs the web request without returning the response headers
        ///// </summary>
        ///// <param name="request">a valid HttpWebRequest object</param>
        ///// <returns>returns back the response stream and the response headers</returns>
        //private static string getResponseStream(HttpWebRequest request)
        //{
        //    WebHeaderCollection responseHeaders;
        //    return getResponseStream(request, out responseHeaders);
        //}

        ///// <summary>
        ///// Performs the web request and returns the response headers
        ///// </summary>
        ///// <param name="request">a valid HttpWebRequest object</param>
        ///// <param name="responseHeaders">collection of response headers</param>
        ///// <returns>returns back the response stream and the response headers</returns>
        //private static string getResponseStream(HttpWebRequest request, out WebHeaderCollection responseHeaders)
        //{
        //    if ((request == null)) { throw new ArgumentNullException("An empty request object was passed"); }

        //    string jsonResult = string.Empty;

        //    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        //    {
        //        responseHeaders = response.Headers;
        //        using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
        //        {
        //            jsonResult = streamReader.ReadToEnd();
        //        }
                
        //        log.DebugFormat("JSON response received:{0}{1}", Environment.NewLine, jsonResult);
        //        return jsonResult;
        //    }
        //}

        private static string getResponse(HttpWebRequest request)
        {
            HttpWebResponse getResponse;
            GitResponse cachedResponse;
            string jsonResponse = string.Empty;

            try
            {
                if ((request == null)) { throw new ArgumentNullException("An empty request object was passed"); }

                // check if the url is in cache
                object cacheData = Utils.GetCache(request.Address.AbsoluteUri);

                
                if (cacheData != null || request.Method != "GET")
                {
                    // data found in cache, load it up
                    cachedResponse = (GitResponse)cacheData;

                    // grab etag and add into request to be made to see if it's expired
                    request.Headers.Add("If-None-Match", cachedResponse.GetETAG);

                    // perform the request to see if the response would be different
                    try
                    {
                        // if data is stale then a HTTP 304 response will drop this into an WebException
                        // else it will contain updated information
                        using (getResponse = (HttpWebResponse)request.GetResponse())
                        {
                            log.Info("Cached data is stale - updating memory cache with latest and greatest");
                            using (StreamReader streamReader = new StreamReader(getResponse.GetResponseStream()))
                            {
                                jsonResponse = streamReader.ReadToEnd();
                                log.DebugFormat("JSON response received:{0}{1}", Environment.NewLine, jsonResponse);

                                // add latest info to memory cache
                                Utils.AddCache(request.Address.AbsoluteUri, new GitResponse(jsonResponse, getResponse.Headers));

                                // send back the jsonResponse
                                return jsonResponse;
                            }
                        }
                    }
                    catch (WebException wex)
                    {
                        if (wex.Response != null && wex.Response.Headers.Get("Status") == "304 Not Modified")
                        {
                            log.Info("Cached data is valid - using cache instead of a new request");
                            return cachedResponse.JsonResponse;
                        }
                        else
                        {
                            log.Error(wex);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        throw;
                    }

                }
                else
                {
                    using (getResponse = (HttpWebResponse)request.GetResponse())
                    {
                        using (StreamReader streamReader = new StreamReader(getResponse.GetResponseStream()))
                        {
                            jsonResponse = streamReader.ReadToEnd();
                            log.DebugFormat("JSON response received:{0}{1}", Environment.NewLine, jsonResponse);

                            // add latest info to memory cache
                            Utils.AddCache(request.Address.AbsoluteUri, new GitResponse(jsonResponse, getResponse.Headers));

                            return jsonResponse;
                        }
                    }
                }
            }
            catch (Exception)
            {

                throw;
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
