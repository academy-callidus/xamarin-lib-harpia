﻿using System;
using System.Collections.Generic;
using System.Text;

namespace xamarin_lib_harpia.Models.Services
{
    internal class ConnectionStatusService
    {
        private readonly IPrinterConnection Connection;
        private string Subtititle {get;set;}
        private bool isConnected {get;set;}

        public ConnectionStatusService(IPrinterConnection connection)
        {
            Connection = connection;
        }

        public bool Execute()
        {
            Connection.InitConnection();
            isConnected = Connection.IsConnected();
            Connection.CloseConnection();
            return isConnected;
        }

        public string ConnectionStatus()
        {
            Subtititle = Execute() ? "Conectado" : "Sem impressora";
            return Subtititle;
        }
    }
}
