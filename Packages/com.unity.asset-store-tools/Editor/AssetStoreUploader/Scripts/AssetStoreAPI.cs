using AssetStoreTools.Utility.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AssetStoreTools.Uploader
{
    public static class AssetStoreAPI
    {
        private const string ToolVersion = "V6.0.0";
        private const string AssetStoreProdUrl = "https://kharma.unity3d.com";
        private const string UnauthSessionId = "26c4202eb475d02864b40827dfff11a14657aa41";
        private const string KharmaSessionId = "kharma.sessionid";

        private static string s_sessionId = EditorPrefs.GetString(KharmaSessionId);
        private static HttpClient httpClient = new HttpClient();
        private static CancellationTokenSource s_downloadCancellationSource;

        public static string SavedSessionId
        {
            get => s_sessionId;
            set
            {
                s_sessionId = value;
                EditorPrefs.SetString(KharmaSessionId, value);
            }
        }

        public static bool IsCloudUserAvailable => CloudProjectSettings.userName != "anonymous";
        public static string LastLoggedInUser = "";
        public static Dictionary<string, OngoingUpload> ActiveUploads = new Dictionary<string, OngoingUpload>();
        public static bool IsUploading => (ActiveUploads.Count > 0);

        static AssetStoreAPI()
        {
            ServicePointManager.DefaultConnectionLimit = 500;
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
            httpClient.Timeout = TimeSpan.FromMinutes(1320);
        }

        #region Login API

        public static void Login(string email, string password, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            FormUrlEncodedContent data = GetLoginContent(new Dictionary<string, string> { { "user", email }, { "pass", password } });
            Login(data, onSuccess, onFail);
        }

        public static void LoginWithSession(Action<JsonValue> onSuccess, Action<ASError> onFail, Action onFailNoSession)
        {
            if (string.IsNullOrEmpty(SavedSessionId))
            {
                onFailNoSession?.Invoke();
                return;
            }

            FormUrlEncodedContent data = GetLoginContent(new Dictionary<string, string> { { "reuse_session", SavedSessionId }, { "xunitysession", UnauthSessionId } });
            Login(data, onSuccess, onFail);
        }

        public static void LoginWithToken(string token, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            FormUrlEncodedContent data = GetLoginContent(new Dictionary<string, string> { { "user_access_token", token } });
            Login(data, onSuccess, onFail);
        }

        private static async void Login(FormUrlEncodedContent data, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            Uri uri = new Uri($"{AssetStoreProdUrl}/login");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            try
            {
                var response = await httpClient.PostAsync(uri, data);
                UploadValuesCompletedLogin(response, onSuccess, onFail);
            }
            catch (Exception e)
            {
                onFail?.Invoke(ASError.GetGenericError(e));
            }
        }

        private static void UploadValuesCompletedLogin(HttpResponseMessage response, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            ASDebug.Log($"Upload Values Complete {response.ReasonPhrase}");
            ASDebug.Log($"Login success? {response.IsSuccessStatusCode}");
            try
            {
                response.EnsureSuccessStatusCode();
                var responseResult = response.Content.ReadAsStringAsync().Result;
                var success = JSONParser.AssetStoreResponseParse(responseResult, out ASError error, out JsonValue jsonResult);
                if (success)
                    onSuccess?.Invoke(jsonResult);
                else
                    onFail?.Invoke(error);
            }
            catch (HttpRequestException ex)
            {
                onFail?.Invoke(ASError.GetLoginError(response, ex));
            }
        }

        #endregion

        #region Package Metadata API

        private static async Task<JsonValue> GetPackageDataMain()
        {
            return await GetAssetStoreData(APIUri("asset-store-tools", "metadata/0", SavedSessionId));
        }

        private static async Task<JsonValue> GetPackageDataExtra()
        {
            return await GetAssetStoreData(APIUri("management", "packages", SavedSessionId));
        }

        private static async Task<JsonValue> GetCategories(bool useCached)
        {
            if(useCached)
            {
                if (AssetStoreCache.GetCachedCategories(out JsonValue cachedCategoryJson))
                    return cachedCategoryJson;

                ASDebug.LogWarning("Failed to retrieve cached category data. Proceeding to download");
            }
            var categoryJson = await GetAssetStoreData(APIUri("management", "categories", SavedSessionId));
            AssetStoreCache.CacheCategories(categoryJson);

            return categoryJson;
        }

        public static async void GetPackageDataFull(bool useCached, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            if (useCached)
            {
                if (AssetStoreCache.GetCachedPackageMetadata(out JsonValue cachedData))
                {
                    onSuccess?.Invoke(cachedData);
                    return;
                }
                
                ASDebug.LogWarning("Failed to retrieve cached package metadata. Proceeding to download");
            }

            try
            {
                var jsonMainData = await GetPackageDataMain();
                var jsonExtraData = await GetPackageDataExtra();
                var jsonCategoryData = await GetCategories(useCached);

                var joinedData = MergePackageData(jsonMainData, jsonExtraData, jsonCategoryData);
                AssetStoreCache.CachePackageMetadata(joinedData);

                onSuccess?.Invoke(joinedData);
            }
            catch (OperationCanceledException)
            {
                ASDebug.Log("Package metadata download operation cancelled");
                DisposeDownloadCancellation();
            }
            catch (Exception e)
            {
                onFail?.Invoke(ASError.GetGenericError(e));
            }
        }

        public static async void GetPackageThumbnails(JsonValue packageJson, bool useCached, Action<string, Texture2D> onSuccess, Action<string, ASError> onFail)
        {
            SetupDownloadCancellation();
            var packageDict = packageJson["packages"].AsDict();
            var packageEnum = packageDict.GetEnumerator();

            for (int i = 0; i < packageDict.Count; i++)
            {
                packageEnum.MoveNext();
                var package = packageEnum.Current;

                try
                {
                    s_downloadCancellationSource.Token.ThrowIfCancellationRequested();

                    if (package.Value["icon_url"]
                        .IsNull()) // If no URL is found in the package metadata, use the default image
                    {
                        Texture2D fallbackTexture = null;
                        ASDebug.Log($"Package {package.Key} has no thumbnail. Returning default image");
                        onSuccess?.Invoke(package.Key, fallbackTexture);
                        continue;
                    }

                    if (useCached &&
                        AssetStoreCache.GetCachedTexture(package.Key,
                            out Texture2D texture)) // Try returning cached thumbnails first 
                    {
                        ASDebug.Log($"Returning cached thumbnail for package {package.Key}");
                        onSuccess?.Invoke(package.Key, texture);
                        continue;
                    }

                    var textureBytes =
                        await DownloadPackageThumbnail(package.Value["icon_url"].AsString());
                    Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    tex.LoadImage(textureBytes);
                    AssetStoreCache.CacheTexture(package.Key, tex);
                    ASDebug.Log($"Returning downloaded thumbnail for package {package.Key}");
                    onSuccess?.Invoke(package.Key, tex);
                }
                catch (OperationCanceledException)
                {
                    DisposeDownloadCancellation();
                    ASDebug.Log("Package thumbnail download operation cancelled");
                    return;
                }
                catch (Exception e)
                {
                    onFail?.Invoke(package.Key, ASError.GetGenericError(e));
                }
                finally
                {
                    packageEnum.Dispose();
                }
            }
        }

        private static async Task<byte[]> DownloadPackageThumbnail(string url)
        {
            // icon_url is presented without http/https
            Uri uri = new Uri($"https:{url}");

            var textureBytes = await httpClient.GetAsync(uri, s_downloadCancellationSource.Token).
                ContinueWith((response) => response.Result.Content.ReadAsByteArrayAsync().Result, s_downloadCancellationSource.Token);
            s_downloadCancellationSource.Token.ThrowIfCancellationRequested();
            return textureBytes;
        }

        public static async void GetRefreshedPackageData(string packageId, Action<JsonValue> onSuccess, Action<ASError> onFail)
        {
            try
            {
                var refreshedDataJson = await GetPackageDataExtra();
                var refreshedPackage = default(JsonValue);

                // Find the updated package data in the latest data json
                foreach (var p in refreshedDataJson["packages"].AsList())
                {
                    if (p["id"] == packageId)
                    {
                        refreshedPackage = p["versions"].AsList()[p["versions"].AsList().Count - 1];
                        break;
                    }
                }

                if (refreshedPackage.Equals(default(JsonValue)))
                {
                    onFail?.Invoke(ASError.GetGenericError(new MissingMemberException($"Unable to find downloaded package data for package id {packageId}")));
                    return;
                }

                // Check if the supplied package id data has been cached and if it contains the corresponding package
                if (!AssetStoreCache.GetCachedPackageMetadata(out JsonValue cachedData) ||
                    !cachedData["packages"].AsDict().ContainsKey(packageId))
                {
                    onFail?.Invoke(ASError.GetGenericError(new MissingMemberException($"Unable to find cached package id {packageId}")));
                    return;
                }

                var cachedPackage = cachedData["packages"].AsDict()[packageId];

                // Retrieve the category map
                var categoryJson = await GetCategories(true);
                var categories = CreateCategoryDictionary(categoryJson);

                // Update the package data
                cachedPackage["name"] = refreshedPackage["name"].AsString();
                cachedPackage["status"] = refreshedPackage["status"].AsString();
                cachedPackage["extra_info"].AsDict()["category_info"].AsDict()["id"] = refreshedPackage["category_id"].AsString();
                cachedPackage["extra_info"].AsDict()["category_info"].AsDict()["name"] =
                    categories.ContainsKey(refreshedPackage["category_id"]) ? categories[refreshedPackage["category_id"].AsString()] : "Unknown";
                cachedPackage["extra_info"].AsDict()["modified"] = refreshedPackage["modified"].AsString();
                cachedPackage["extra_info"].AsDict()["size"] = refreshedPackage["size"].AsString();

                AssetStoreCache.CachePackageMetadata(cachedData);
                onSuccess?.Invoke(cachedPackage);
            }
            catch (OperationCanceledException)
            {
                ASDebug.Log("Package metadata download operation cancelled");
                DisposeDownloadCancellation();
            }
            catch (Exception e)
            {
                onFail?.Invoke(ASError.GetGenericError(e));
            }
        }

        public static List<string> GetPackageUploadedVersions(string packageId, string versionId)
        {
            var versions = new List<string>();
            try
            {
                // Retrieve the data for already uploaded versions (should prevent interaction with Uploader)
                var versionsTask = Task.Run(() => GetAssetStoreData(APIUri("content", $"preview/{packageId}/{versionId}", SavedSessionId)));
                if (!versionsTask.Wait(5000))
                    throw new TimeoutException("Could not retrieve uploaded versions within a reasonable time interval");

                var versionsJson = versionsTask.Result;
                foreach (var version in versionsJson["content"].AsDict()["unity_versions"].AsList())
                    versions.Add(version.AsString());
            }
            catch (OperationCanceledException)
            {
                ASDebug.Log("Package version download operation cancelled");
                DisposeDownloadCancellation();
            }
            catch (Exception e)
            {
                ASDebug.LogError(e);
            }

            return versions;
        }

        #endregion

        #region Package Upload API

        public static async Task<PackageUploadResult> UploadPackage(string packageId, string packageName, string filePath,
            string localPackageGuid, string localPackagePath, string localProjectPath)
        {
            try
            {
                ASDebug.Log("Upload task starting");
                // Reloading assemblies or entering Play Mode may cancel the upload as static variables are reset
                EditorApplication.LockReloadAssemblies();
                if (!IsUploading) // Only subscribe before the first upload
                    EditorApplication.playModeStateChanged += EditorPlayModeStateChangeHandler;

                var progressData = new OngoingUpload(packageId, packageName);
                ActiveUploads.Add(packageId, progressData);

                var result = await Task.Run(() => UploadPackageTask(progressData, filePath, localPackageGuid, localPackagePath, localProjectPath));

                ActiveUploads.Remove(packageId);

                ASDebug.Log("Upload task finished");
                return result;
            }
            catch (Exception e)
            {
                return PackageUploadResult.PackageUploadFail(ASError.GetGenericError(e));
            }
            finally
            {
                if (!IsUploading) // Only unsubscribe after the last upload
                    EditorApplication.playModeStateChanged -= EditorPlayModeStateChangeHandler;
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static PackageUploadResult UploadPackageTask(OngoingUpload currentUpload, string filePath,
            string localPackageGuid, string localPackagePath, string localProjectPath)
        {
            ASDebug.Log("Preparing to upload package within API");
            string api = "asset-store-tools";
            string uri = $"package/{currentUpload.VersionId}/unitypackage";

            Dictionary<string, string> packageParams = new Dictionary<string, string>
            {
                {"root_guid", localPackageGuid}, // NOTE: prepackaged uploads will not pass these parameters.              
                {"root_path", localPackagePath}, // We need to make sure that the backend validation
                {"project_path", localProjectPath} // service accepts such use-cases without failure
            };

            ASDebug.Log($"Creating upload request for {currentUpload.VersionId} {currentUpload.PackageName}");

            FileStream requestFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("X-Unity-Session", SavedSessionId);

            long chunkSize = 32768;
            try
            {
                ASDebug.Log("Starting upload process...");
                var watch = System.Diagnostics.Stopwatch.StartNew(); // Debugging

                var content = new StreamContent(requestFileStream, (int)chunkSize);
                var response = httpClient.PutAsync(APIUri(api, uri, SavedSessionId, packageParams), content, currentUpload.CancellationToken);

                // Progress tracking
                int updateIntervalMs = 100;
                DateTime previousTime = DateTime.Now;
                while (!response.IsCompleted)
                {
                    var currentTime = DateTime.Now;
                    if (DateTime.Now.Subtract(previousTime).Milliseconds < updateIntervalMs)
                        continue;
                    previousTime = currentTime;

                    float uploadProgress = (float)requestFileStream.Position / requestFileStream.Length * 100;
                    currentUpload.UpdateProgress(uploadProgress);
                }

                // 2020.3 - although cancellation token shows a requested cancellation, the HttpClient
                // tends to return a false 'IsCanceled' value, thus yielding an exception when attempting to read the response.
                // For now we'll just check the token as well, but this needs to be investigated later on.
                if (response.IsCanceled || currentUpload.CancellationToken.IsCancellationRequested)
                    currentUpload.CancellationToken.ThrowIfCancellationRequested();

                watch.Stop();
                
                ASDebug.Log($"Finished uploading, time taken: {watch.Elapsed.Seconds} seconds");
                var responseString = response.Result.Content.ReadAsStringAsync().Result;

                var success = JSONParser.AssetStoreResponseParse(responseString, out ASError error, out JsonValue json);
                ASDebug.Log("Upload response JSON: " + json.ToString());
                if (success)
                    return PackageUploadResult.PackageUploadSuccess();
                else
                    return PackageUploadResult.PackageUploadFail(error);
            }
            catch (OperationCanceledException)
            {
                // Uploading is canceled
                ASDebug.Log("Upload operation cancelled");
                return PackageUploadResult.PackageUploadCancelled();
            }
            catch (Exception e)
            {
                ASDebug.LogError("Upload operation encountered an undefined exception: " + e);
                var error = ASError.GetGenericError(e);
                return PackageUploadResult.PackageUploadFail(error);
            }
            finally
            {
                requestFileStream.Dispose();
                currentUpload.Dispose();
            }
        }

        public static void AbortPackageUpload(string packageId)
        {
            ActiveUploads[packageId]?.Cancel();
        }

        #endregion

        #region Utility Methods

        private static string GetLicenseHash()
        {
            return UnityEditorInternal.InternalEditorUtility.GetAuthToken().Substring(0, 40);
        }

        private static string GetHardwareHash()
        {
            return UnityEditorInternal.InternalEditorUtility.GetAuthToken().Substring(40, 40);
        }

        private static FormUrlEncodedContent GetLoginContent(Dictionary<string, string> loginData)
        {
            loginData.Add("unityversion", Application.unityVersion);
            loginData.Add("toolversion", ToolVersion);
            loginData.Add("license_hash", GetLicenseHash());
            loginData.Add("hardware_hash", GetHardwareHash());

            return new FormUrlEncodedContent(loginData);
        }

        private static async Task<JsonValue> GetAssetStoreData(Uri uri)
        {
            SetupDownloadCancellation();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("X-Unity-Session", SavedSessionId);

            var response = await httpClient.GetAsync(uri, s_downloadCancellationSource.Token)
                .ContinueWith((x) => x.Result.Content.ReadAsStringAsync().Result, s_downloadCancellationSource.Token);
            s_downloadCancellationSource.Token.ThrowIfCancellationRequested();

            if (!JSONParser.AssetStoreResponseParse(response, out var error, out var jsonMainData))
                throw error.Exception;

            return jsonMainData;
        }

        private static Uri APIUri(string apiPath, string endPointPath, string sessionId)
        {
            return APIUri(apiPath, endPointPath, sessionId, null);
        }

        // Method borrowed from A$ tools, could maybe be simplified to only retain what is necessary?
        private static Uri APIUri(string apiPath, string endPointPath, string sessionId, IDictionary<string, string> extraQuery)
        {
            Dictionary<string, string> extraQueryMerged;

            if (extraQuery == null)
                extraQueryMerged = new Dictionary<string, string>();
            else
                extraQueryMerged = new Dictionary<string, string>(extraQuery);

            extraQueryMerged.Add("unityversion", Application.unityVersion);
            extraQueryMerged.Add("toolversion", ToolVersion);
            extraQueryMerged.Add("xunitysession", sessionId);

            string uriPath = $"{AssetStoreProdUrl}/api/{apiPath}/{endPointPath}.json";
            UriBuilder uriBuilder = new UriBuilder(uriPath);

            StringBuilder queryToAppend = new StringBuilder();
            foreach (KeyValuePair<string, string> queryPair in extraQueryMerged)
            {
                string queryName = queryPair.Key;
                string queryValue = Uri.EscapeDataString(queryPair.Value);

                queryToAppend.AppendFormat("&{0}={1}", queryName, queryValue);
            }
            if (!string.IsNullOrEmpty(uriBuilder.Query))
                uriBuilder.Query = uriBuilder.Query.Substring(1) + queryToAppend;
            else
                uriBuilder.Query = queryToAppend.Remove(0, 1).ToString();

            return uriBuilder.Uri;
        }

        private static JsonValue MergePackageData(JsonValue mainPackageData, JsonValue extraPackageData, JsonValue categoryData)
        {
            ASDebug.Log($"Main package data\n{mainPackageData}");
            var mainDataDict = mainPackageData["packages"].AsDict();
            
            // Most likely both of them will be true at the same time, but better to be safe
            if (mainDataDict.Count == 0 || !extraPackageData.ContainsKey("packages"))
                return new JsonValue();

            ASDebug.Log($"Extra package data\n{extraPackageData}");
            var extraDataDict = extraPackageData["packages"].AsList();

            var categories = CreateCategoryDictionary(categoryData);

            foreach (var md in mainDataDict)
            {
                foreach (var ed in extraDataDict)
                {
                    if (ed["id"].AsString() != md.Key)
                        continue;

                    // Create a field for extra data
                    var extraData = JsonValue.NewDict();

                    // Add category field
                    var categoryEntry = JsonValue.NewDict();

                    var categoryId = ed["category_id"].AsString();
                    var categoryName = categories.ContainsKey(categoryId) ? categories[categoryId] : "Unknown";

                    categoryEntry["id"] = categoryId;
                    categoryEntry["name"] = categoryName;

                    extraData["category_info"] = categoryEntry;

                    // Add modified time and size
                    var versions = ed["versions"].AsList();
                    extraData["modified"] = versions[versions.Count - 1]["modified"];
                    extraData["size"] = versions[versions.Count - 1]["size"];

                    md.Value.AsDict()["extra_info"] = extraData;
                }
            }

            mainPackageData.AsDict()["packages"] = new JsonValue(mainDataDict);
            return mainPackageData;
        }

        private static Dictionary<string, string> CreateCategoryDictionary(JsonValue json)
        {
            var categories = new Dictionary<string, string>();

            var list = json.AsList();

            for (int i = 0; i < list.Count; i++)
            {
                var category = list[i].AsDict();
                if (category["status"].AsString() == "deprecated")
                    continue;
                categories.Add(category["id"].AsString(), category["assetstore_name"].AsString());
            }

            return categories;
        }
        
        public static bool IsPublisherValid(JsonValue json, out ASError error)
        {
            error = ASError.GetPublisherNullError(json["name"]);

            if (!json.ContainsKey("publisher"))
                return false;
            
            // If publisher account is not created - let them know
            return !json["publisher"].IsNull();
        }

        public static void AbortDownloadTasks()
        {
            s_downloadCancellationSource?.Cancel();
        }

        public static void AbortUploadTasks()
        {
            foreach(var upload in ActiveUploads)
            {
                AbortPackageUpload(upload.Key);
            }
        }

        private static void SetupDownloadCancellation()
        {
            if (s_downloadCancellationSource != null && s_downloadCancellationSource.IsCancellationRequested)
                DisposeDownloadCancellation();

            if (s_downloadCancellationSource == null)
                s_downloadCancellationSource = new CancellationTokenSource();
        }

        private static void DisposeDownloadCancellation()
        {
            s_downloadCancellationSource?.Dispose();
            s_downloadCancellationSource = null;
        }

        private static void EditorPlayModeStateChangeHandler(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) 
                return;
            
            EditorApplication.ExitPlaymode();
            EditorUtility.DisplayDialog("Notice", "Entering Play Mode is not allowed while there's a package upload in progress.\n\n" +
                                                  "Please wait until the upload is finished or cancel the upload from the Asset Store Uploader window", "OK");
        }

        #endregion
    }
}