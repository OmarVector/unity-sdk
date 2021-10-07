using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

public class NetworkClient
{
    // HttpClient is intended to be instantiated once per application, rather than per-use
    readonly HttpClient client = new HttpClient();
    private static GameObject dispatcherGO;
    private static NetworkCallbackEventDispatcher dispatcher;
    
    private static NetworkClient instance;
    public static NetworkClient Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new NetworkClient();
                instance.client.Timeout = TimeSpan.FromMilliseconds(3000);
                if (dispatcherGO == null)
                {
                    dispatcherGO = new GameObject();
                    dispatcher = dispatcherGO.AddComponent<NetworkCallbackEventDispatcher>();
                }
            }
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
            SaveContext.GetLocalState(), 
            // new LocalState(), // TODO: remove & uncomment line above
            includeData);
        Task.Factory.StartNew(async delegate { await PostGetMessages(requestBody, onSuccessAction, onErrorAction); });
    }

    public void PrivacyManagerViews(Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        Task.Factory.StartNew(async delegate { await GetGdprPrivacyManagerView(onSuccessAction, onErrorAction); });
    }

    public void MessageGdpr(Action<string> onSuccessAction, Action<Exception> onErrorAction, int millisTimeout)
    {
        Task.Factory.StartNew(async delegate { await GetGdprMessage(onSuccessAction, onErrorAction); });
    }

    public void ConsentGdpr( /*CONSENT_ACTION_TYPE*/ int actionType, Action<string> onSuccessAction,
        Action<Exception> onErrorAction, int millisTimeout,
        ConsentGdprSaveAndExitVariables pmSaveAndExitVariables = null)
    {
        var dict = new Dictionary<string, string> {{"type", "RecordString"}};
        var includeData = new IncludeDataPostGetMessagesRequest()
        {
            localState = dict,
            TCData = dict
            // messageMetaData = dict,
        };
        if (pmSaveAndExitVariables == null)
            pmSaveAndExitVariables = new ConsentGdprSaveAndExitVariables(
                language: "EN", //TODO: get default lan 
                privacyManagerId: "16879",
                categories: new ConsentGdprSaveAndExitVariablesCategory[] { },
                vendors: new ConsentGdprSaveAndExitVariablesVendor[] { });
        PostConsentGdprRequest body = new PostConsentGdprRequest(requestUUID: GUID.Value,
            idfaStatus: "accepted",
            localState: SaveContext.GetLocalState(),
            includeData: includeData,
            pmSaveAndExitVariables: pmSaveAndExitVariables
        );
        Task.Factory.StartNew(async delegate { await PostConsentGdpr(actionType, body, onSuccessAction, onErrorAction); });
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
                                    // {"messageId", "16434"},
                                    {"messageId", "16879"},
                                });
    }

    private static string GetGdprPrivacyManagerViewUriWithQueryParams()
    {
        // https://cdn.privacy-mgmt.com/consent/tcfv2/privacy-manager/privacy-manager-view?siteId=17935&consentLanguage=EN
        // return BuildUriWithQuery(baseAdr: "https://cdn.privacy-mgmt.com/",
        return BuildUriWithQuery(baseAdr: "https://cdn.sp-stage.net/",
                                path: "consent/tcfv2/privacy-manager/privacy-manager-view",
                                qParams: new Dictionary<string, string>()
                                {
                                    // { "siteId", "17935"},
                                    { "siteId", "4933"},
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
        // return BuildUriWithQuery(baseAdr: "https://cdn.privacy-mgmt.com/",
        return BuildUriWithQuery(baseAdr: "https://cdn.sp-stage.net/",
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
            dispatcher.Enqueue(delegate { onSuccessAction?.Invoke(responseBody); });
        }
        catch(HttpRequestException ex)
        {
            dispatcher.Enqueue(delegate { onErrorAction?.Invoke(ex); });
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
            dispatcher.Enqueue(delegate { onSuccessAction?.Invoke(responseBody); });
        }
        catch (Exception ex)
        {            
            dispatcher.Enqueue(delegate { onErrorAction?.Invoke(ex); });
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
            dispatcher.Enqueue(delegate { onSuccessAction?.Invoke(responseBody); });
        }
        catch (Exception ex)
        {            
            dispatcher.Enqueue(delegate { onErrorAction?.Invoke(ex); });
        }
    }

    async Task PostConsentGdpr(int actionType, PostConsentGdprRequest body, Action<string> onSuccessAction, Action<Exception> onErrorAction)
    {
        try
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            string json = JsonSerializer.Serialize(body, options);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(GetConsentGdprQueryParams(actionType), data);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            dispatcher.Enqueue(delegate { onSuccessAction?.Invoke(responseBody); });
        }
        catch (Exception ex)
        {          
            dispatcher.Enqueue(delegate { onErrorAction?.Invoke(ex); });
        }
    }
    #endregion
}