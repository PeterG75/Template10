﻿using System.ComponentModel;

namespace Template10.Portable.Common
{
    public interface IPropertyBag
    {
        event PropertyChangedEventHandler MapChanged;

        string[] AllKeys();
        bool ContainsKey(string key);

        T GetValue<T>(string key);
        string GetString(string key);

        (bool Success, string Value) TryGetValue(string key);
        (bool Success, T Value) TryGetValue<T>(string key);

        void SetValue<T>(string key, T value);
        void SetString(string key, string value);

        void Clear();
    }
}