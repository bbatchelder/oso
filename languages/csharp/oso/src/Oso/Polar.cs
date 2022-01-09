﻿using System.Text.Json;
using Oso.Ffi;

namespace Oso;

public class Polar : IDisposable
{
    private readonly PolarHandle _handle;
    private readonly Host _host;

    // struct polar_Polar *polar_new(void);
    public Polar()
    {
        _handle = Native.polar_new();
        _host = new Host();
    }

    // struct polar_CResult_c_void *polar_load(struct polar_Polar *polar_ptr, const char *sources);
    public void Load(List<string> sources)
    {
        string sourcesJson = JsonSerializer.Serialize(sources.Select(source => new KeyValuePair<string, string>("src", source)));
        Native.Load(_handle, sourcesJson);
    }

    public void Load(string source)
    {
        // TODO
        string sourcesJson = $@"[{{""src"":""{source}"", ""filename"": null}}]"; // JsonSerializer.Serialize(sources.Select(source => new KeyValuePair<string, string>("src", source)));
        Native.Load(_handle, sourcesJson);
    }

    // struct polar_CResult_c_void *polar_clear_rules(struct polar_Polar *polar_ptr);
    public void ClearRules()
    {
        Native.ClearRules(_handle);
    }

    /*
     *  struct polar_CResult_c_void *polar_register_constant(struct polar_Polar *polar_ptr,
     *                                                      const char *name,
     *                                                      const char *value);
     */
    public void RegisterConstant(string name, string value)
    {
        Native.RegisterConstant(_handle, name, value);
    }

    /**
     *  struct polar_CResult_c_void *polar_register_mro(struct polar_Polar *polar_ptr,
     *                                                  const char *name,
     *                                                  const char *mro);
     */
    public void RegisterMro(string name, string mro)
    {
        Native.RegisterMro(_handle, name, mro);
    }

    // struct polar_Query *polar_next_inline_query(struct polar_Polar *polar_ptr, uint32_t trace);
    // TODO: Make this an iterator?
    /*
    public IEnumerator<Query> InlineQueries(int trace)
    {

    }
    */
    public Query? NextInlineQuery(uint trace)
    {
        var handle = Native.polar_next_inline_query(_handle, trace);
        return (handle != null) ? new Query(handle, _host) : null;
    }

    /*
    struct polar_CResult_Query *polar_new_query_from_term(struct polar_Polar *polar_ptr,
                                                          const char *query_term,
                                                          uint32_t trace);
    */
    public Query NewQueryFromTerm(string queryTerm, uint trace)
    {
        return Native.NewQueryFromTerm(_handle, _host, queryTerm, trace);
    }

    /*
    struct polar_CResult_Query *polar_new_query(struct polar_Polar *polar_ptr,
                                                const char *query_str,
                                                uint32_t trace);
    */
    public Query NewQuery(string query, uint trace)
    {
        return Native.NewQuery(_handle, _host, query, trace);
    }

    // struct polar_CResult_c_char *polar_next_polar_message(struct polar_Polar *polar_ptr);
    // TODO: Turn this into an IEnumerator?
    /*
    public IEnumerator<string> Messages
    {
        get
        {
            // Add error handling to check for error and throw PolarException
        }
    }
    */
    public string? NextMessage()
    {
        return Native.NextPolarMessage(_handle);
    }

    // uint64_t polar_get_external_id(struct polar_Polar *polar_ptr);
    public ulong ExternalId
    {
        get => Native.polar_get_external_id(_handle);
    }

    public void Dispose()
    {
        _handle.Dispose();
    }

    /*
    struct polar_CResult_c_char *polar_build_data_filter(struct polar_Polar *polar_ptr,
                                                         const char *types,
                                                         const char *results,
                                                         const char *variable,
                                                         const char *class_tag);
    */
    public string? BuildDataFilter(string types, string results, string variable, string classTag)
    {
        return Native.BuildDataFilter(_handle, types, results, variable, classTag);
    }

    /*
    struct polar_CResult_c_char *polar_build_filter_plan(struct polar_Polar *polar_ptr,
                                                         const char *types,
                                                         const char *results,
                                                         const char *variable,
                                                         const char *class_tag);
    */
    public string? BuildFilterPlan(string types, string results, string variable, string classTag)
    {
        return Native.BuildFilterPlan(_handle, types, results, variable, classTag);
    }
}