// Copyright (c) Winton. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Consul;
using Winton.Extensions.Configuration.Consul.Parsers;

namespace Winton.Extensions.Configuration.Consul.Extensions
{
  internal static class KVPairExtensions
  {
    internal static IDictionary<string, string?> ConvertToConfig(
      this KVPair kvPair,
      string keyToRemove,
      IConfigurationParser parser)
    {
      using Stream stream = new MemoryStream(kvPair.Value);
      string configText;
      using (var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, true))
      {
        configText = reader.ReadToEnd();
      }

      stream.Position = 0;

      IDictionary<string, string?> parsed;
      try
      {
        parsed = parser.Parse(stream);
      }
      catch (Exception ex)
      {
        var wrapped = new InvalidOperationException(
          $"Failed to parse JSON configuration for the Consul KV key '{kvPair.Key}'. {ex.Message}",
          ex);
        wrapped.Data["ConsulKvKey"] = kvPair.Key;
        wrapped.Data["ConsulKvValue"] = configText;
        throw wrapped;
      }

      return parsed
        .ToDictionary(
          pair =>
          {
            var prefix = kvPair.Key
              .RemoveStart(keyToRemove)
              .TrimEnd('/')
              .Replace("/_", string.Empty)
              .Replace('/', ':');
            var key = $"{prefix}:{pair.Key}"
              .Trim(':');
            if (string.IsNullOrEmpty(key))
            {
              throw new InvalidKeyPairException(
                "The key must not be null or empty. Ensure that there is at least one key under the root of the config or that the data there contains more than just a single value.");
            }

            return key;
          },
          pair => pair.Value);
    }

    internal static bool HasValue(this KVPair kvPair)
    {
      return kvPair.IsLeafNode() && kvPair.Value != null && kvPair.Value.Any();
    }

    internal static bool IsLeafNode(this KVPair kvPair)
    {
      return !kvPair.Key.EndsWith("/");
    }

    private static string RemoveStart(this string s, string toRemove)
    {
      return s.StartsWith(toRemove) ? s.Remove(0, toRemove.Length) : s;
    }
  }
}