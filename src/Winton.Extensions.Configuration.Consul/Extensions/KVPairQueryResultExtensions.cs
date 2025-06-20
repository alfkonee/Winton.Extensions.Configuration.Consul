﻿// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Consul;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
    internal static class KVPairQueryResultExtensions
    {
        internal static bool HasValue(this QueryResult<KVPair[]>? result)
        {
            return result != null
                   && result.StatusCode != HttpStatusCode.NotFound
                   && result.Response != null
                   && result.Response.Any(kvp => kvp.HasValue());
        }

    internal static Dictionary<string, string?> ToConfigDictionary(
      this QueryResult<KVPair[]> result,
      Func<KVPair, IDictionary<string, string?>> convertConsulKVPairToConfig)
    {
      return (result.Response ?? new KVPair[0])
        .Where(kvp => kvp.HasValue())
        .SelectMany(convertConsulKVPairToConfig)
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }
  }
}