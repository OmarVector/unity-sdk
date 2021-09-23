using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

public class NetworkClient
{
    // HttpClient is intended to be instantiated once per application, rather than per-use
    readonly HttpClient client = new HttpClient();

    private static NetworkClient instance;
    public static NetworkClient Instance
    {
        get
        {
            if (instance == null)
                instance = new NetworkClient();
            return instance;
        }
    }
    
    #region Public
    public void GetMessages(int accountId, string propertyHref, CampaignsPostGetMessagesRequest campaigns, Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        string idfaStatus = "unknown";
        var dict = new Dictionary<string, string> {{"type", "RecordString"}};
        var includeData = new IncludeDataPostGetMessagesRequest()
        {
            localState = dict,
            messageMetaData = dict,
            TCData = dict
        };
        var requestBody = new PostGetMessagesRequest(accountId, propertyHref, idfaStatus, GUID.Value, campaigns, 
            // SaveContext.GetLocalState(), 
            new LocalState(), // TODO: remove & uncomment line above
            includeData);
        PostGetMessages(requestBody, onSuccessAction, onErrorAction).Wait(millisTimeout);
    }

    public void PrivacyManagerViews(Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        GetGdprPrivacyManagerView(onSuccessAction, onErrorAction).Wait(millisTimeout);
    }

    public void MessageGdpr(Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        GetGdprMessage(onSuccessAction, onErrorAction).Wait(millisTimeout);
    }

    public void ConsentGdpr(Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        var dict = new Dictionary<string, string> {{"type", "RecordString"}};
        var includeData = new IncludeDataPostGetMessagesRequest()
        {
            localState = dict,
            TCData = dict
            // messageMetaData = dict,
        };
        PostConsentGdprRequest body = new PostConsentGdprRequest(requestUUID: GUID.Value, 
                                                                 idfaStatus: "accepted", 
                                                                 localState: SaveContext.GetLocalState(),
                                                                 includeData: includeData);
        PostConsentGdpr(body, onSuccessAction, onErrorAction).Wait(millisTimeout);
    }
    #endregion
    
    #region Query Parameters
    private static string GetGdprMessageUriWithQueryParams()
    {
        // https://cdn.sp-stage.net/wrapper/v2/message/gdpr?env=stage&consentLanguage=en&propertyId=4933&messageId=16434
        return BuildUriWithQuery(baseAdr: "https://cdn.sp-stage.net/wrapper/v2/",
                                path: "wrapper/v2/message/gdpr",
                                qParams: new Dictionary<string, string>()
                                {
                                    {"env", "stage"},
                                    {"consentLanguage", "en"},
                                    {"propertyId", "4933"},
                                    {"messageId", "16434"},
                                });
    }

    private static string GetGdprPrivacyManagerViewUriWithQueryParams()
    {
        // https://cdn.privacy-mgmt.com/consent/tcfv2/privacy-manager/privacy-manager-view?siteId=17935&consentLanguage=EN
        return BuildUriWithQuery(baseAdr: "https://cdn.privacy-mgmt.com/",
                                path: "consent/tcfv2/privacy-manager/privacy-manager-view",
                                qParams: new Dictionary<string, string>()
                                {
                                    { "siteId", "17935"},
                                    { "consentLanguage", "EN"},
                                });
    }

    private static string GetGetMessagesUriWithQueryParams()
    {
        // https://cdn.sp-stage.net/wrapper/v2/get_messages/?env=stage
        return BuildUriWithQuery(baseAdr: "https://cdn.sp-stage.net/",
            path: "wrapper/v2/get_messages/",
            qParams: new Dictionary<string, string>()
            {
                { "env", "stage"},
            });
    }
    
    private static string GetConsentGdprQueryParams(int action)
    {
        // https://cdn.privacy-mgmt.com/wrapper/v2/messages/choice/gdpr/11?env=prod
        return BuildUriWithQuery(baseAdr: "https://cdn.privacy-mgmt.com/",
            path: "wrapper/v2/messages/choice/gdpr/" + action.ToString(),
            qParams: new Dictionary<string, string>()
            {
                { "env", "stage"},
            });
    }
    
    private static string BuildUriWithQuery(string baseAdr, string path, Dictionary<string, string> qParams)
    {
        var builder = new UriBuilder(baseAdr) {Port = -1};
        builder.Path = path;
        var query = HttpUtility.ParseQueryString(builder.Query);
        foreach (KeyValuePair<string, string> kv in qParams)
        {
            query[kv.Key] = kv.Value;
        }                             
        builder.Query = query.ToString();
        return builder.ToString();
    }
    #endregion

    #region Network Requests
    async Task GetGdprMessage(Action<string> onSuccessAction, Action<Exception> onErrorAction)
    {
        try
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(GetGdprMessageUriWithQueryParams());
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            onSuccessAction?.Invoke(responseBody);
        }
        catch(HttpRequestException ex)
        {
            onErrorAction?.Invoke(ex);
        }
    }

    async Task GetGdprPrivacyManagerView(Action<string> onSuccessAction, Action<Exception> onErrorAction)
    {
        try
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(GetGdprPrivacyManagerViewUriWithQueryParams());
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            onSuccessAction?.Invoke(responseBody);
        }
        catch (Exception ex)
        {            
            onErrorAction?.Invoke(ex);
        }
    }

    async Task PostGetMessages(PostGetMessagesRequest body, Action<string> onSuccessAction, Action<Exception> onErrorAction)
    {
        try
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            string json = JsonSerializer.Serialize(body, options);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(GetGetMessagesUriWithQueryParams(), data);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            onSuccessAction?.Invoke(responseBody);
        }
        catch (Exception ex)
        {            
            onErrorAction?.Invoke(ex);
        }
    }

    async Task PostConsentGdpr(PostConsentGdprRequest body, Action<string> onSuccessAction, Action<Exception> onErrorAction)
    {
        try
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            string json = JsonSerializer.Serialize(body, options);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(GetConsentGdprQueryParams(11), data);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            onSuccessAction?.Invoke(responseBody);
        }
        catch (Exception ex)
        {            
            onErrorAction?.Invoke(ex);
        }
    }
    #endregion
}



internal class PostConsentGdprRequest
{
    // [JsonInclude] public List<string> pubData = new List<string>();                //TODO
    // [JsonInclude] public List<string> pmSaveAndExitVariables = new List<string>(); //TODO
    
    [JsonInclude] public IncludeDataPostGetMessagesRequest includeData;
    [JsonInclude] public string requestUUID;
    [JsonInclude] public string idfaStatus;
    [JsonInclude] public LocalState localState;

    public PostConsentGdprRequest(string requestUUID, string idfaStatus, LocalState localState, IncludeDataPostGetMessagesRequest includeData)
    {
        this.requestUUID = requestUUID;
        this.idfaStatus = idfaStatus;
        this.localState = localState;
        this.includeData = includeData;
    }
}

/*
string json = @"{
                ""pubData"": {},
                ""includeData"": {
                    ""localState"": {
                        ""type"": ""RecordString""
                    },
                    ""TCData"": {
                        ""type"": ""RecordString""
                    }
                },
                ""requestUUID"": ""76886C9A-232E-40F7-A7EB-A6640A6071DD"",
                ""pmSaveAndExitVariables"": {},
                ""idfaStatus"": ""accepted"",
                ""localState"": 
                {
                    ""ios14"": {
                        ""mmsCookies"": [
                            ""_sp_v1_uid=1:668:c848c5c3-24e9-4a74-9d8b-60cfa832f706"",
                            ""_sp_v1_data=2:340139:1620986612:0:1:0:1:0:0:_:-1"",
                            ""_sp_v1_ss=1:H4sIAAAAAAAAAItWqo5RKimOUbKKBjLyQAyD2lidGKVUEDOvNCcHyC4BK6iurVWKBQAW54XRMAAAAA%3D%3D"",
                            ""_sp_v1_opt=1:"",
                            ""_sp_v1_stage="",
                            ""_sp_v1_csv=null"",
                            ""_sp_v1_lt=1:""
                        ],
                        ""propertyId"": 16893
                    },
                    ""gdpr"": {
                        ""messageId"": 488398,
                        ""mmsCookies"": [
                            ""_sp_v1_uid=1:291:583c104a-4817-4de5-b155-492a59e018bc"",
                            ""_sp_v1_data=2:338205:1620986612:0:1:0:1:0:0:_:-1"",
                            ""_sp_v1_ss=1:H4sIAAAAAAAAAItWqo5RKimOUbKKBjLyQAyD2lidGKVUEDOvNCcHyC4BK6iurVWKBQAW54XRMAAAAA%3D%3D"",
                            ""_sp_v1_opt=1:"",
                            ""_sp_v1_consent=1!-1:-1:-1:-1:-1:-1"",
                            ""_sp_v1_stage="",
                            ""_sp_v1_csv=null"",
                            ""_sp_v1_lt=1:""
                        ],
                        ""propertyId"": 16893,
                        ""uuid"": ""8f967da5-064f-48f6-9ef9-0518e1587142""
                    }
                }
            }";
*/