using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GG.Extensions
{
    public static class WebExtensions
    {
        
        /// <summary>
        ///     Setup a web request to send a JSON in the body
        /// </summary>
        /// <param name="json">The json to send</param>
        /// <param name="uri">The URI to send to</param>
        public static UnityWebRequest SetupPostWebRequest(string json, string uri)
        {
#if UNITY_2022_3_OR_NEWER
            UnityWebRequest request = UnityWebRequest.PostWwwForm(uri, "");
#else
            UnityWebRequest request = UnityWebRequest.Post(uri, "");
#endif

            byte[] bodyRaw = new UTF8Encoding().GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" };
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        /// <summary>
        /// Adds a Parameter value to the url
        /// </summary>
        /// <param name="request"></param>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        public static void AddParameter(this UnityWebRequest request, string parameter, string value)
        {
            if (request.url.Contains("?"))
            {
                request.url += $"&{parameter}={value}";
            }
            else
            {
                request.url += $"?{parameter}={value}";
            }
        }
        
        //public static async Task<Texture2D> GetTexture(string urlToImage) 
        //{
        //    UnityWebRequest www = UnityWebRequestTexture.GetTexture(urlToImage);

        //    www.SendWebRequest();

        //    if(www.isNetworkError || www.isHttpError) 
        //    {
        //        Debug.Log(www.error);
        //    }
        //    else
        //    {
        //        Texture2D myTexture = DownloadHandlerTexture.GetContent(www);
        //        return myTexture;
        //    }

        //    return null;
        //}
    }
}