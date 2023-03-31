
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

public class SychronizerOrganisator
{

    public bool completed => sender.completed && receiver.completed && log.completed;

    SenderCsvSynchronizer sender;
    ReceiverCsvSynchronizer receiver;
    GroupLogCsvSynchronizer log;

    public SychronizerOrganisator(ulong simulationId, List<BLESender> senders, List<BLEReceiver> receivers, List<TracingApp> apps)
    {
        sender = new SenderCsvSynchronizer(simulationId, senders, 2);
        receiver = new ReceiverCsvSynchronizer(simulationId, receivers, 4);
        log = new GroupLogCsvSynchronizer(simulationId, apps, 2);
        sender.Init();
        receiver.Init();
        log.Init();
    }

    public void NewLoop()
    {
        sender.NewLoop();
        receiver.NewLoop(); 
        log.NewLoop();
    }
    
    internal void Destruct()
    {
        sender.Destruct();
        receiver.Destruct();
        log.Destruct(); 
    }

    bool final = false;

    public void Iterate()
    { 
        sender.Iterate(final);   
        receiver.Iterate(final);
        log.Iterate(final);
    }
        
    public void FinalSync()
    {
        final = true;
        NewLoop();
    }

}
