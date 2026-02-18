using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatthiasToolbox.Basics.Interfaces;

namespace MatthiasToolbox.Basics.Datastructures
{
    internal interface ICache<TParameterIdentifier, TDictionaryIdentifier> : IClearable, IPurgeable
    {
        bool ContainsParameter(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName);
    }

    public class Cache<TData, TParameterIdentifier, TDictionaryIdentifier> : ICache<TParameterIdentifier, TDictionaryIdentifier>
    {
        #region cvar

        private bool _isAgeTrackingEnabled = true;
        private TDictionaryIdentifier _defaultDictionaryIdentifier;
        private Dictionary<TDictionaryIdentifier, Dictionary<TParameterIdentifier, TData>> _data = new Dictionary<TDictionaryIdentifier, Dictionary<TParameterIdentifier, TData>>();
        private Dictionary<TDictionaryIdentifier, Dictionary<TParameterIdentifier, DateTime>> _timeStamps;

        #endregion
        #region prop

        public bool IsAgeTrackingEnabled
        {
            get { return _isAgeTrackingEnabled; }
        }

        public TDictionaryIdentifier DefaultDictionaryIdentifier
        {
            get { return _defaultDictionaryIdentifier; }
            set { _defaultDictionaryIdentifier = value; }
        }

        #endregion
        #region ctor

        public Cache(TDictionaryIdentifier defaultDictionaryIdentifier, bool isAgeTrackingEnabled = true) 
        {
            if (isAgeTrackingEnabled)
                _timeStamps = new Dictionary<TDictionaryIdentifier, Dictionary<TParameterIdentifier, DateTime>>();
            else
                _isAgeTrackingEnabled = false;

            this._defaultDictionaryIdentifier = defaultDictionaryIdentifier;

            _data[_defaultDictionaryIdentifier] = new Dictionary<TParameterIdentifier, TData>();
        }

        #endregion
        #region impl

        #region get

        #region custom dictionary

        public TData Get(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName)
        {
            return _data[dictionaryName][parameterName];
        }

        public TData Get(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, TData defaultValue)
        {
            if (!ContainsParameter(dictionaryName, parameterName)) return defaultValue;
            return Get(dictionaryName, parameterName);
        }

        public bool TryGet(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, out TData result)
        {
            result = Get(dictionaryName, parameterName, default(TData));
            return ContainsParameter(dictionaryName, parameterName);
        }

        #endregion
        #region default dictionary

        public TData Get(TParameterIdentifier parameterName)
        {
            return Get(_defaultDictionaryIdentifier, parameterName);
        }

        public TData Get(TParameterIdentifier parameterName, TData defaultValue)
        {
            return Get(_defaultDictionaryIdentifier, parameterName, defaultValue);
        }

        public bool TryGet(TParameterIdentifier parameterName, out TData result)
        {
            return TryGet(_defaultDictionaryIdentifier, parameterName, out result);
        }

        #endregion

        #endregion
        #region put

        #region custom dictionary

        public void Put(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, TData value)
        {
            if (!_data.ContainsKey(dictionaryName)) _data[dictionaryName] = new Dictionary<TParameterIdentifier, TData>();

            _data[dictionaryName][parameterName] = value;

            if (_isAgeTrackingEnabled) _timeStamps[dictionaryName][parameterName] = DateTime.Now;
        }

        #endregion
        #region default dictionary

        public void Put(TParameterIdentifier parameterName, TData value)
        {
            Put(_defaultDictionaryIdentifier, parameterName, value);
        }

        #endregion

        #endregion
        #region purge

        /// <summary>
        /// Clears the cache's contents AND the structure
        /// see also <code>Purge()</code>
        /// </summary>
        public void Clear()
        {
            Purge();
            _data.Clear();
        }

        /// <summary>
        /// Clears the cache's contents but not the structure
        /// see also <code>Clear()</code>
        /// </summary>
        public void Purge()
        {
            foreach (Dictionary<TParameterIdentifier, TData> dict in _data.Values) dict.Clear();
        }

        #endregion
        #region contains

        public bool ContainsDictionary(TDictionaryIdentifier dictionaryName)
        {
            return _data.ContainsKey(dictionaryName);
        }

        public bool ContainsParameter(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName)
        {
            if (!ContainsDictionary(dictionaryName)) return false;
            return _data[dictionaryName].ContainsKey(parameterName);
        }

        #endregion

        #endregion
    }

    public class Cache<TDictionaryIdentifier, TParameterIdentifier> : IClearable, IPurgeable
    {
        #region cvar

        private bool _isAgeTrackingEnabled = true;
        private TDictionaryIdentifier _defaultDictionaryIdentifier;
        private Dictionary<Type, object> _subCaches;

        #endregion
        #region prop

        public bool IsAgeTrackingEnabled
        {
            get { return _isAgeTrackingEnabled; }
        }

        public TDictionaryIdentifier DefaultDictionaryIdentifier
        {
            get { return _defaultDictionaryIdentifier; }
            set { _defaultDictionaryIdentifier = value; }
        }

        #endregion
        #region ctor

        public Cache(TDictionaryIdentifier defaultDictionaryIdentifer, bool isAgeTrackingEnabled = false)
        {
            _subCaches = new Dictionary<Type, object>();
            _subCaches[typeof(bool)] = new Cache<bool, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(char)] = new Cache<char, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(string)] = new Cache<string, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(int)] = new Cache<int, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(long)] = new Cache<long, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(float)] = new Cache<float, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(double)] = new Cache<double, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(decimal)] = new Cache<decimal, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(DateTime)] = new Cache<DateTime, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
            _subCaches[typeof(TimeSpan)] = new Cache<TimeSpan, TParameterIdentifier, TDictionaryIdentifier>(defaultDictionaryIdentifer, isAgeTrackingEnabled);
        }

        #endregion
        #region impl

        #region get

        public T Get<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, Func<T> retrievalFunction)
        {
            if (!HasValue<T>(dictionaryName, parameterName)) return retrievalFunction.Invoke();
            return Get<T>(dictionaryName, parameterName);
        }

        public T Get<T>(TParameterIdentifier parameterName, Func<T> retrievalFunction)
        {
            return Get<T>(_defaultDictionaryIdentifier, parameterName, retrievalFunction);
        }

        #region custom dictionary

        public T Get<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName)
        {
            return GetCache<T>().Get(dictionaryName, parameterName);
        }

        public T Get<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, T defaultValue)
        {
            if (!SubCacheExists<T>()) return defaultValue;
            return GetCache<T>().Get(dictionaryName, parameterName, defaultValue);
        }

        public bool TryGet<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, out T result)
        {
            result = default(T);
            if (!SubCacheExists<T>()) return false;
            return GetCache<T>().TryGet(dictionaryName, parameterName, out result);
        }

        #endregion
        #region default dictionary

        public T Get<T>(TParameterIdentifier parameterName)
        {
            return Get<T>(_defaultDictionaryIdentifier, parameterName);
        }

        public T Get<T>(TParameterIdentifier parameterName, T defaultValue)
        {
            return Get<T>(_defaultDictionaryIdentifier, parameterName, defaultValue);
        }

        public bool TryGet<T>(TParameterIdentifier parameterName, out T result)
        {
            return TryGet<T>(_defaultDictionaryIdentifier, parameterName, out result);
        }

        #endregion

        #endregion
        #region put

        #region custom dictionary

        public void Put<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName, T value)
        {
            if (!SubCacheExists<T>()) CreateSubCache<T>();
            GetCache<T>().Put(dictionaryName, parameterName, value);
        }

        #endregion
        #region default dictionary

        public void Put<T>(TParameterIdentifier parameterName, T value)
        {
            Put(_defaultDictionaryIdentifier, parameterName, value);
        }

        #endregion

        #endregion
        #region purge

        /// <summary>
        /// Clears the cache's contents AND the structure
        /// see also <code>Purge()</code>
        /// </summary>
        public void Clear()
        {
            foreach (object o in _subCaches.Values) (o as IClearable).Clear();
            _subCaches.Clear();
        }

        /// <summary>
        /// Clears the cache's contents but not the structure
        /// see also <code>Clear()</code>
        /// </summary>
        public void Purge()
        {
            foreach (object o in _subCaches.Values) (o as IPurgeable).Purge();
        }

        #endregion
        #region exists

        public bool HasValue(TParameterIdentifier parameterName)
        {
            return HasValue(_defaultDictionaryIdentifier, parameterName);
        }

        public bool HasValue(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName)
        {
            foreach (object o in _subCaches)
            {
                if ((o as ICache<TParameterIdentifier, TDictionaryIdentifier>).ContainsParameter(dictionaryName, parameterName)) return true;
            }
            return false;
        }

        public bool HasValue<T>(TParameterIdentifier parameterName)
        {
            return HasValue<T>(_defaultDictionaryIdentifier, parameterName);
        }

        public bool HasValue<T>(TDictionaryIdentifier dictionaryName, TParameterIdentifier parameterName)
        {
            if (!SubCacheExists<T>()) return false;
            return GetCache<T>().ContainsParameter(dictionaryName, parameterName);
        }

        private bool SubCacheExists(Type t) { return _subCaches.ContainsKey(t); }

        private bool SubCacheExists<T>() { return _subCaches.ContainsKey(typeof(T)); }

        #endregion
        #region create

        private void CreateSubCache<T>(bool isAgeTrackingEnabled = false)
        {
            _subCaches[typeof(T)] = new Cache<T, TParameterIdentifier, TDictionaryIdentifier>(_defaultDictionaryIdentifier, isAgeTrackingEnabled);
        }

        #endregion

        #endregion
        #region util

        private Cache<T, TParameterIdentifier, TDictionaryIdentifier> GetCache<T>() { return _subCaches[typeof(T)] as Cache<T, TParameterIdentifier, TDictionaryIdentifier>; }

        #endregion
    }

    public class Cache<T> : Cache<int, T>
    {
        public Cache(int defaultDictionaryID = 0, bool isAgeTrackingEnabled = false) : base(defaultDictionaryID, isAgeTrackingEnabled) { }
    }

    public class Cache : Cache<string, string>
    {
        public Cache(string defaultDictionaryID = "Default Dictionary", bool isAgeTrackingEnabled = false) : base(defaultDictionaryID, isAgeTrackingEnabled) { }
    }
}
