using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Conference.Common.Entity {
    public class ServiceConfigurationSettingConnectionFactory: IDbConnectionFactory {
        private readonly object _lockObject = new object();
        private readonly IDbConnectionFactory _parent;
        private Dictionary<string, string> _cachedConnectionStringMap = new Dictionary<string, string>();

        public ServiceConfigurationSettingConnectionFactory(IDbConnectionFactory parent) {
            this._parent = parent;
        }

        public DbConnection CreateConnection(string nameOrConnectionString) {
            string connectionString = null;
            if(!IsConnectionString(nameOrConnectionString)) {
                if(!this._cachedConnectionStringMap.TryGetValue(nameOrConnectionString, out connectionString)) {
                    lock(this._lockObject) {
                        if(!this._cachedConnectionStringMap.TryGetValue(nameOrConnectionString, out connectionString)) {
                            string connectionStringName = "DbContext." + nameOrConnectionString;
                            try {
                                var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
                                if(connectionStringSettings!=null) {
                                    connectionString = connectionStringSettings.ConnectionString;
                                }
                            }
                            catch(ConfigurationErrorsException) {
                                
                            }

                            var immutableDictionary = this._cachedConnectionStringMap.Concat(new[] {
                                new KeyValuePair<string, string>(nameOrConnectionString, connectionString)
                            }).ToDictionary(x => x.Key, x => x.Value);

                            this._cachedConnectionStringMap = immutableDictionary;
                        }
                    }
                }
            }

            if(connectionString==null) {
                connectionString = nameOrConnectionString;
            }

            return this._parent.CreateConnection(connectionString);
        }

        private bool IsConnectionString(string connectionStringCandidate) {
            return connectionStringCandidate.IndexOf('=') >= 0;
        }
    }
}