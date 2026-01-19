using UnityEngine.Networking;
using System;
using System.Text;

namespace Anatidae
{
    public class AnatidaeProxyWebRequest
    {
        public static UnityWebRequest Get(string uri)
        {
            UnityEngine.Debug.Log(uri);
            return UnityWebRequest.Get(uri);
        }

        public static UnityWebRequest Post(string uri, string postData, string contentType)
        {
            UnityEngine.Debug.Log(uri);
            UnityWebRequest request = new UnityWebRequest(uri, "POST");
            SetupPost(request, postData, contentType);
            return request;
        }

        private static void SetupPost(UnityWebRequest request, string postData, string contentType)
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            if (string.IsNullOrEmpty(postData))
            {
                request.SetRequestHeader("Content-Type", contentType);
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.uploadHandler.contentType = contentType;
        }
    }
}