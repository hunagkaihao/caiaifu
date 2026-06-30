using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Ecs.HttpApiTool;

public static class HttpApiHelper
{
    //GET
    public static T Get<T>(string url, string resource, Dictionary<string, string> pars = null) where T:class, new()
    {
        Task<T> task = ExecApiAsync<T>(url, resource, Method.Get, pars);
        return task.GetAwaiter().GetResult();
    }
    
    public static async Task<T> GetAsync<T>(string url, string resource, Dictionary<string, string> pars = null) where T:class, new()
    {
        return await ExecApiAsync<T>(url, resource, Method.Get, pars);
    }

    public static RestResponse Get(string url, string resource, Dictionary<string, string> pars = null)
    {
        Task<RestResponse> task = ExecApiAsync(url, resource, Method.Get, pars);
        return task.GetAwaiter().GetResult();
    }
    
    public static async Task<RestResponse> GetAsync(string url, string resource, Dictionary<string, string> pars = null)
    {
        return await ExecApiAsync(url, resource, Method.Get, pars);
    }


    //POST
    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static T Post<T>(string url, string resource, object pars = null) where T:class, new()
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        Task<T> task = ExecApiAsync<T>(url, resource, Method.Post, pars);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static async Task<T> PostAsync<T>(string url, string resource, object pars = null) where T:class, new()
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        return await ExecApiAsync<T>(url, resource, Method.Post, pars);
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static RestResponse Post(string url, string resource, object pars = null)
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        Task<RestResponse> task = ExecApiAsync(url, resource, Method.Post, pars);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static async Task<RestResponse> PostAsync(string url, string resource, object pars = null)
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        return await ExecApiAsync(url, resource, Method.Post, pars);
    }

    public static async Task<RestResponse> PostTestAsync(string url, string resource, object pars = null)
    {
        using (RestClient client = new RestClient(url))
        {
            var request = new RestRequest(resource, Method.Post);
            request.AddBody(pars);
            return await client.ExecuteAsync(request);
        }
    }


    //DELETE
    public static T Delete<T>(string url, string resource, Dictionary<string, string> pars = null) where T:class, new()
    {
        Task<T> task = ExecApiAsync<T>(url, resource, Method.Delete, pars);
        return task.GetAwaiter().GetResult();
    }

    public static async Task<T> DeleteAsync<T>(string url, string resource, Dictionary<string, string> pars = null) where T:class, new()
    {
        return await ExecApiAsync<T>(url, resource, Method.Delete, pars);
    }

    public static RestResponse Delete(string url, string resource, Dictionary<string, string> pars = null)
    {
        Task<RestResponse> task = ExecApiAsync(url, resource, Method.Delete, pars);
        return task.GetAwaiter().GetResult();
    }

    public static async Task<RestResponse> DeleteAsync(string url, string resource, Dictionary<string, string> pars = null)
    {
        return await ExecApiAsync(url, resource, Method.Delete, pars);
    }


    //PUT
    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static T Put<T>(string url, string resource, object pars = null) where T:class, new()
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        Task<T> task = ExecApiAsync<T>(url, resource, Method.Put, pars);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static async Task<T> PutAsync<T>(string url, string resource, object pars = null) where T:class, new()
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        return await ExecApiAsync<T>(url, resource, Method.Put, pars);
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static RestResponse Put(string url, string resource, object pars = null)
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        Task<RestResponse> task = ExecApiAsync(url, resource, Method.Put, pars);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// par的取值可以为null，或Dictionary<string, string>，或其它class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static async Task<RestResponse> PutAsync(string url, string resource, object pars = null)
    {
        if(pars != null && pars.GetType() != typeof(Dictionary<string, string>) && !pars.GetType().IsClass)
            throw new ArgumentException("pars必须是null，或Dictionary<string, string>类型，或其它Class");
        return await ExecApiAsync(url, resource, Method.Put, pars);
    }



    private static async Task<RestResponse> ExecApiAsync(string baseUrl, string resource, Method method, object pars)
    {
        using (RestClient client = new RestClient(baseUrl))
        {
            var request = new RestRequest(resource, method);
            RestResponse restResponse = null;
            switch(method)
            {
                case Method.Post:
                    if(pars != null && pars.GetType() != typeof(Dictionary<string, string>))
                        request.AddBody(pars);
                    else if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                    {
                        foreach(var dic in (Dictionary<string, string>)pars)
                        request.AddParameter(dic.Key, dic.Value);
                    }
                    restResponse = await client.PostAsync(request);
                    break;

                case Method.Delete:
                    if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                        foreach(var dic in (Dictionary<string, string>)pars)
                            request.AddParameter(dic.Key, dic.Value);
                    restResponse = await client.DeleteAsync(request);
                    break;

                case Method.Put:
                    if(pars != null && pars.GetType() != typeof(Dictionary<string, string>))
                        request.AddBody(pars);
                    else if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                    {
                        foreach(var dic in (Dictionary<string, string>)pars)
                        request.AddParameter(dic.Key, dic.Value);
                    }
                    restResponse = await client.PutAsync(request);
                    break;

                case Method.Get:
                    if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                        foreach(var dic in (Dictionary<string, string>)pars)
                            request.AddParameter(dic.Key, dic.Value);
                    restResponse = await client.GetAsync(request);
                    break;

                // case Method.Patch:
                //     restResponse = await client.PatchAsync(request);
                //     break;
                // case Method.Options:
                //     restResponse = await client.OptionsAsync(request);
                //     break;
                // case Method.Head:
                //     restResponse = await client.HeadAsync(request);
                //     break;

                default:
                    throw new Exception($"方法{method.ToString()}暂不支持");
            } 
            return restResponse;           
        }
    }

    private static async Task<T> ExecApiAsync<T>(string baseUrl, string resource, Method method, object pars) where T : class, new()
    {
        using (RestClient client = new RestClient(baseUrl))
        {
            var request = new RestRequest(resource, method);
            T result = null;
            switch(method)
            {
                case Method.Post:
                    if(pars != null)
                        request.AddBody(pars);
                    result = await client.PostAsync<T>(request);
                    break;

                case Method.Delete:
                    if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                        foreach(var dic in (Dictionary<string, string>)pars)
                            request.AddParameter(dic.Key, dic.Value);
                    result = await client.DeleteAsync<T>(request);
                    break;

                case Method.Put:
                    if(pars != null)
                        request.AddBody(pars);
                    result = await client.PutAsync<T>(request);
                    break;

                case Method.Get:
                    if(pars != null && pars.GetType() == typeof(Dictionary<string, string>))
                        foreach(var dic in (Dictionary<string, string>)pars)
                            request.AddParameter(dic.Key, dic.Value);
                    result = await client.GetAsync<T>(request);
                    break;

                // case Method.Patch:
                //     restResponse = await client.PatchAsync(request);
                //     break;
                // case Method.Options:
                //     restResponse = await client.OptionsAsync(request);
                //     break;
                // case Method.Head:
                //     restResponse = await client.HeadAsync(request);
                //     break;

                default:
                    throw new Exception($"方法{method.ToString()}暂不支持");
            } 
            return result;           
        }
    }
}