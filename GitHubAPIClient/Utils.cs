using System.IO;

namespace GitHubAPIClient
{
    public static class Utils
    {

        public static string EncodeFile(string FilePath)
        {
            string plainText = File.ReadAllText(FilePath);
            return Base64Encode(plainText);
        }

        public static string Base64Encode(string plainText)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(bytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
