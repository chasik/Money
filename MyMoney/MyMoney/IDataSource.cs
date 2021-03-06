﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMoney
{
    public delegate void ConnectedHandler(string message);
    public delegate void GetInstrumentsHandler();
    public delegate void ThreadStarted(string message);
    public delegate void GetInformation(InfoElement _element, string _mess);
    public delegate void ChangeGlass(DateTime _dt, double _p, double _v, int _row, ActionGlassItem _a);
    public delegate void AddTick(DateTime _dt, double _p, double _v, ActionGlassItem _a);

    public interface IDataSource
    {
        DataTable dtInstruments {get;set;}
        void ConnectToDataSource();
        void ThreadConnect();
        void GetAllInstruments();
        void ThreadInstruments();
        event ConnectedHandler OnConnected;
        event GetInstrumentsHandler OnGetInstruments;
        event GetInformation OnInformation;

        event ChangeGlass OnChangeGlass;
        event AddTick OnAddTick;

        GlassGraph glassgraph { get; set; }
    }
}
