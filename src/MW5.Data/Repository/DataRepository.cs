﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MW5.Data.Views;
using MW5.Plugins.Concrete;
using MW5.Plugins.Enums;
using MW5.Plugins.Events;
using MW5.Plugins.Interfaces;
using MW5.Plugins.Mvp;
using MW5.Plugins.Services;
using MW5.Shared;

namespace MW5.Data.Repository
{
    /// <summary>
    /// A model for repository dock panel
    /// </summary>
    public class DataRepository: IRepository
    {
        private readonly IGeoDatabaseService _databaseService;
        private readonly IFileDialogService _fileDialogService;
        private List<string> _folders;
        private List<WmsServer> _wmsServers;
        private List<DatabaseConnection> _connections;
        public event EventHandler<FolderEventArgs> FolderAdded;
        public event EventHandler<FolderEventArgs> FolderRemoved;
        public event EventHandler<ConnectionEventArgs> ConnectionAdded;
        public event EventHandler<ConnectionEventArgs> ConnectionRemoved;

        public DataRepository(IGeoDatabaseService databaseService, IFileDialogService fileDialogService)
        {
            if (databaseService == null) throw new ArgumentNullException("databaseService");
        
            _databaseService = databaseService;
            _fileDialogService = fileDialogService;

            Init();
        }

        private void Init()
        {
            _folders = new List<string>();

            _connections = new List<DatabaseConnection>();

            _wmsServers = new List<WmsServer>() 
            { 
                new WmsServer("Lizard Tech", "http://demo.lizardtech.com/lizardtech/iserv/ows") 
            };
        }

        public IEnumerable<string> Folders
        {
            get { return _folders; }
        }

        public IEnumerable<DatabaseConnection> Connections
        {
            get { return _connections; }
        }

        public IEnumerable<WmsServer> WmsServers
        {
            get { return _wmsServers; }
        }

        public void AddFolderLink()
        {
            string path;
            if (_fileDialogService.ChooseFolder(string.Empty, out path))
            {
                AddFolderLink(path);
            }
        }

        public void AddFolderLink(string path)
        {
            if (Directory.Exists(path) && !HasFolder(path))
            {
                _folders.Add(path);
                FireEvent(FolderAdded, path);
            }
        }

        public void RemoveFolderLink(string path, bool silent)
        {
            if (!HasFolder(path)) return;

            if (silent || MessageService.Current.Ask("Do you want to remove a link to this folder from the repository?" +
                                           Environment.NewLine + "(The folder will remain intact on the disk.)"))
            {

                if (_folders.Remove(GetFolder(path)))
                {
                    FireEvent(FolderRemoved, path);
                }
            }
        }

        public void AddConnectionWithPrompt(GeoDatabaseType? databaseType = null)
        {
            var connection = _databaseService.PromptUserForConnection(databaseType);
            if (connection != null)
            {
                AddConnection(connection);
            }
        }

        public void AddConnection(DatabaseConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            _connections.Add(connection);
            FireEvent(ConnectionAdded, connection);
        }

        public void RemoveConnection(DatabaseConnection connection, bool silent)
        {
            if (!silent && _connections.Contains(connection))
            {
                if (!MessageService.Current.Ask("Do you want to remove database connection: " + connection.Name + "?"))
                {
                    return;
                }
            }

            _connections.Remove(connection);
            FireEvent(ConnectionRemoved, connection);
        }

        public void RemoveWmsServer(WmsServer server)
        {
            if (_wmsServers.Remove(server))
            {
                // TODO: fire event
            }
        }

        public void AddWmsServer(WmsServer server)
        {
            if (server != null)
            {
                // TODO: fire event
                _wmsServers.Add(server);
            }
        }

        private bool HasFolder(string path)
        {
            return GetFolder(path) != null;
        }

        private string GetFolder(string path)
        {
            return _folders.FirstOrDefault(f => f.EqualsIgnoreCase(path));
        }
        
        private void FireEvent(EventHandler<ConnectionEventArgs> handler, DatabaseConnection connection)
        {
            if (handler != null)
            {
                handler(this, new ConnectionEventArgs(connection));
            }
        }

        private void FireEvent(EventHandler<FolderEventArgs> handler, string path) 
        {
            if (handler != null)
            {
                handler(this, new FolderEventArgs(path));
            }
        }
    }
}
