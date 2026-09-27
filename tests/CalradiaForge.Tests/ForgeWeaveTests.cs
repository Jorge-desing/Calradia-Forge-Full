using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using CalradiaForge.Core;
using CalradiaForge.Sdk;

internal static class ForgeWeaveTests
{
    public static void Run(Action<string,Action> test)
    {
        test("ForgeWeave exposes an additive event registry",()=>{
            var engine=new TestEngine();ForgeApi.Connect(engine);
            try {Require(ReferenceEquals(ForgeApi.Events,engine));}
            finally {ForgeApi.Disconnect();}
        });
        test("ForgeWeave exposes a replay registry only through the connected host",()=>{
            var engine=new TestEngine();ForgeApi.Connect(engine);
            try {Require(ReferenceEquals(ForgeApi.Replays,engine) && ForgeApi.Replays.Records.Count==0);}
            finally {ForgeApi.Disconnect();}
            Require(ForgeApi.Replays==null);
        });
        test("ForgeApi availability registration is atomic across reconnects",()=>{
            var first=new TestEngine();var second=new TestEngine();var calls=0;
            Action<IForgeRegistry> subscriber=registry=>{if(ReferenceEquals(registry,first) || ReferenceEquals(registry,second))calls++;};
            ForgeApi.Connect(first);
            try
            {
                ForgeApi.RegisterWhenAvailable(subscriber);
                Require(calls==1);
                ForgeApi.Disconnect();ForgeApi.Connect(second);
                Require(calls==2);
            }
            finally {ForgeApi.UnregisterWhenAvailable(subscriber);ForgeApi.Disconnect();}
        });
        test("ForgeApi availability delivery skips superseded reentrant generations",()=>{
            ForgeApi.Disconnect();
            var first=new TestEngine();var current=new TestEngine();
            var observed=new List<IForgeRegistry>();
            Action<IForgeRegistry> reconnect=registry=>{if(ReferenceEquals(registry,first))ForgeApi.Connect(current);};
            Action<IForgeRegistry> observer=registry=>observed.Add(registry);
            ForgeApi.RegisterWhenAvailable(reconnect);
            ForgeApi.RegisterWhenAvailable(observer);
            try
            {
                ForgeApi.Connect(first);
                Require(observed.Count==1 && ReferenceEquals(observed[0],current));
            }
            finally
            {
                ForgeApi.UnregisterWhenAvailable(reconnect);
                ForgeApi.UnregisterWhenAvailable(observer);
                ForgeApi.Disconnect();
            }
        });
        test("ForgeApi unregister skips a managed callback already captured by Connect",()=>{
            ForgeApi.Disconnect();
            using(var blockerEntered=new ManualResetEventSlim(false))
            using(var releaseBlocker=new ManualResetEventSlim(false))
            {
                Action<IForgeRegistry> blocker=_=>
                {
                    blockerEntered.Set();
                    if(!releaseBlocker.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException("Availability test blocker timed out.");
                };
                var callbackCount=0;
                Action<IForgeRegistry> managed=_=>Interlocked.Increment(ref callbackCount);
                Exception connectError=null;
                Exception disconnectError=null;
                Thread connectThread=null;
                ForgeApi.Available+=blocker;
                ForgeApi.RegisterWhenAvailable(managed);
                try
                {
                    connectThread=new Thread(()=>
                    {
                        try {ForgeApi.Connect(new TestEngine());}
                        catch(Exception error) {connectError=error;}
                        finally
                        {
                            try {ForgeApi.Disconnect();}
                            catch(Exception error) {disconnectError=error;}
                        }
                    });
                    connectThread.Start();
                    Require(blockerEntered.Wait(TimeSpan.FromSeconds(5)));

                    // Connect has already captured the invocation list, but the managed
                    // callback has not reached its turn yet.
                    ForgeApi.UnregisterWhenAvailable(managed);
                    releaseBlocker.Set();
                    Require(connectThread.Join(TimeSpan.FromSeconds(5)));
                    if(connectError!=null)throw new Exception("Connect failed during the availability race regression.",connectError);
                    if(disconnectError!=null)throw new Exception("Disconnect failed during the availability race regression.",disconnectError);
                    Require(callbackCount==0);
                }
                finally
                {
                    releaseBlocker.Set();
                    ForgeApi.UnregisterWhenAvailable(managed);
                    ForgeApi.Available-=blocker;
                    if(connectThread!=null && connectThread.IsAlive)connectThread.Join(TimeSpan.FromSeconds(5));
                }
            }
        });
        test("ForgeApi unregister waits for an in-flight managed callback and self-unregisters safely",()=>{
            ForgeApi.Disconnect();
            using(var callbackEntered=new ManualResetEventSlim(false))
            using(var releaseCallback=new ManualResetEventSlim(false))
            using(var unregisterStarted=new ManualResetEventSlim(false))
            using(var unregisterFinished=new ManualResetEventSlim(false))
            using(var firstConnectReturned=new ManualResetEventSlim(false))
            using(var allowSecondConnect=new ManualResetEventSlim(false))
            {
                var callbackCount=0;
                Action<IForgeRegistry> managed=_=>
                {
                    Interlocked.Increment(ref callbackCount);
                    callbackEntered.Set();
                    if(!releaseCallback.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException("Managed callback test timed out.");
                };
                Exception connectError=null;
                Exception disconnectError=null;
                Exception unregisterError=null;
                var connectThread=new Thread(()=>
                {
                    try
                    {
                        ForgeApi.Connect(new TestEngine());
                        firstConnectReturned.Set();
                        if(!allowSecondConnect.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException("Second connect was not released by the test.");
                        ForgeApi.Connect(new TestEngine());
                    }
                    catch(Exception error) {connectError=error;}
                    finally
                    {
                        try {ForgeApi.Disconnect();}
                        catch(Exception error) {disconnectError=error;}
                    }
                });
                Thread unregisterThread=null;
                ForgeApi.RegisterWhenAvailable(managed);
                try
                {
                    connectThread.Start();
                    Require(callbackEntered.Wait(TimeSpan.FromSeconds(5)));
                    unregisterThread=new Thread(()=>
                    {
                        unregisterStarted.Set();
                        try {ForgeApi.UnregisterWhenAvailable(managed);}
                        catch(Exception error) {unregisterError=error;}
                        finally {unregisterFinished.Set();}
                    });
                    unregisterThread.Start();
                    Require(unregisterStarted.Wait(TimeSpan.FromSeconds(5)));
                    Require(!unregisterFinished.Wait(TimeSpan.FromMilliseconds(100)));

                    releaseCallback.Set();
                    Require(unregisterThread.Join(TimeSpan.FromSeconds(5)));
                    Require(firstConnectReturned.Wait(TimeSpan.FromSeconds(5)));
                    allowSecondConnect.Set();
                    Require(connectThread.Join(TimeSpan.FromSeconds(5)));
                    if(connectError!=null)throw new Exception("Connect failed during the in-flight callback regression.",connectError);
                    if(disconnectError!=null)throw new Exception("Disconnect failed during the in-flight callback regression.",disconnectError);
                    if(unregisterError!=null)throw new Exception("Unregister failed during the in-flight callback regression.",unregisterError);
                    Require(callbackCount==1);
                    Require(callbackCount==1);
                }
                finally
                {
                    releaseCallback.Set();
                    allowSecondConnect.Set();
                    ForgeApi.UnregisterWhenAvailable(managed);
                    if(connectThread.IsAlive)connectThread.Join(TimeSpan.FromSeconds(5));
                    if(unregisterThread!=null && unregisterThread.IsAlive)unregisterThread.Join(TimeSpan.FromSeconds(5));
                }

                ForgeApi.Disconnect();
                var selfCalls=0;
                Action<IForgeRegistry> selfRemoving=null;
                selfRemoving=_=>
                {
                    Interlocked.Increment(ref selfCalls);
                    ForgeApi.UnregisterWhenAvailable(selfRemoving);
                };
                ForgeApi.RegisterWhenAvailable(selfRemoving);
                ForgeApi.Connect(new TestEngine());
                Require(selfCalls==1);
                ForgeApi.Connect(new TestEngine());
                Require(selfCalls==1);
                ForgeApi.Disconnect();
            }
        });
        test("ForgeWeave SubscribeWeave throws explicitly when no event registry is connected",()=>{
            ForgeApi.Disconnect();
            Throws<InvalidOperationException>(()=>ForgeCampaignEvents.SubscribeWeave("weave.legacy",ForgeEventKind.ForgeReady,_=>{}));
            var engine=new TestEngine();ForgeApi.Connect(engine);
            try
            {
                var handler=ForgeCampaignEvents.SubscribeWeave("weave.legacy",ForgeEventKind.ForgeReady,_=>{});
                Require(engine.ForgeWeaveSubscriptionCount==1);
                Require(ForgeCampaignEvents.UnsubscribeWeave(handler));
                Require(engine.ForgeWeaveSubscriptionCount==0);
            }
            finally {ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration waits for a host and registers on connect",()=>{
            ForgeApi.Disconnect();
            var engine=new TestEngine();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.waiting",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                Require(registration.State==ForgeWeaveRegistrationState.WaitingForHost);
                Require(registration.Error==null);
                ForgeApi.Connect(engine);
                Require(registration.State==ForgeWeaveRegistrationState.Registered);
                Require(engine.ForgeWeaveSubscriptionCount==1);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration attaches immediately to an existing host",()=>{
            ForgeApi.Disconnect();
            var engine=new TestEngine();ForgeApi.Connect(engine);
            ForgeWeaveRegistration registration=null;
            try
            {
                registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.immediate",ForgeEventKind.ForgeReady,_=>{});
                Require(registration.State==ForgeWeaveRegistrationState.Registered);
                Require(registration.Handler.Subscription.Descriptor.Id=="weave.immediate");
                Require(engine.ForgeWeaveSubscriptionCount==1);
                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed);
                Require(engine.ForgeWeaveSubscriptionCount==0);
            }
            finally {registration?.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration reports unsupported hosts and recovers on reconnect",()=>{
            ForgeApi.Disconnect();
            var unsupported=new EmptyRegistry();
            var engine=new TestEngine();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.unsupported",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(unsupported);
                Require(registration.State==ForgeWeaveRegistrationState.HostUnsupported);
                Require(registration.Error is InvalidOperationException);
                ForgeApi.Disconnect();
                Require(registration.State==ForgeWeaveRegistrationState.WaitingForHost);
                ForgeApi.Connect(engine);
                Require(registration.State==ForgeWeaveRegistrationState.Registered);
                Require(engine.ForgeWeaveSubscriptionCount==1);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration follows disconnect and host reconnection",()=>{
            ForgeApi.Disconnect();
            var first=new TestEngine();var second=new TestEngine();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.reconnect",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(first);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && first.ForgeWeaveSubscriptionCount==1);
                ForgeApi.Connect(second);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && first.ForgeWeaveSubscriptionCount==0 && second.ForgeWeaveSubscriptionCount==1);
                ForgeApi.Disconnect();
                Require(registration.State==ForgeWeaveRegistrationState.WaitingForHost && second.ForgeWeaveSubscriptionCount==0);
                ForgeApi.Connect(first);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && first.ForgeWeaveSubscriptionCount==1);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration disposal cancels a pending host subscription",()=>{
            ForgeApi.Disconnect();
            var engine=new TestEngine();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.pending.dispose",ForgeEventKind.ForgeReady,_=>{});
            registration.Dispose();
            try
            {
                ForgeApi.Connect(engine);
                Require(registration.State==ForgeWeaveRegistrationState.Disposed);
                Require(engine.ForgeWeaveSubscriptionCount==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave active registration disposal preserves host thread affinity",()=>{
            ForgeApi.Disconnect();
            var engine=new TestEngine();ForgeApi.Connect(engine);
            ForgeWeaveRegistration registration=null;
            try
            {
                registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.thread.affinity",ForgeEventKind.ForgeReady,_=>{});
                Require(registration.State==ForgeWeaveRegistrationState.Registered && engine.ForgeWeaveSubscriptionCount==1);
                Exception disposeError=null;
                var otherThread=new Thread(()=>{try {registration.Dispose();}catch(Exception ex) {disposeError=ex;}});
                otherThread.Start();otherThread.Join();
                Require(disposeError is InvalidOperationException);
                Require(registration.State==ForgeWeaveRegistrationState.Registered);
                Require(registration.Error is InvalidOperationException);
                Require(engine.ForgeWeaveSubscriptionCount==1);
                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && engine.ForgeWeaveSubscriptionCount==0);
            }
            finally {registration?.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration fails closed after an uncertain host registration",()=>{
            ForgeApi.Disconnect();
            var failing=new FailingEventRegistry();var engine=new TestEngine();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.failure",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(failing);
                Require(registration.State==ForgeWeaveRegistrationState.Failed);
                Require(registration.Error is InvalidOperationException);
                Require(failing.RegisterAttempts==1);
                ForgeApi.Disconnect();
                ForgeApi.Connect(engine);
                Require(registration.State==ForgeWeaveRegistrationState.Failed);
                Require(registration.Error is InvalidOperationException && engine.ForgeWeaveSubscriptionCount==0);

                registration.Dispose();
                var replacement=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.failure.recovered",ForgeEventKind.ForgeReady,_=>{});
                try { Require(replacement.State==ForgeWeaveRegistrationState.Registered && engine.ForgeWeaveSubscriptionCount==1); }
                finally { replacement.Dispose(); }
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed duplicate registration failure preserves the existing handler",()=>{
            ForgeApi.Disconnect();
            var engine=new TestEngine();ForgeApi.Connect(engine);
            var existing=ForgeCampaignEvents.SubscribeWeave("weave.duplicate",ForgeEventKind.ForgeReady,_=>{});
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.duplicate",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                Require(registration.State==ForgeWeaveRegistrationState.Failed);
                Require(engine.ForgeWeaveSubscriptionCount==1);
                Require(ForgeCampaignEvents.UnsubscribeWeave(existing));
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave managed registration retains failed unregistration for retry",()=>{
            ForgeApi.Disconnect();
            var first=new RetryableUnregisterEventRegistry();
            var second=new RetryableUnregisterEventRegistry();
            var third=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.unregister.retry",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(first);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && first.Count==1);
                first.ThrowNextUnregister=true;
                ForgeApi.Connect(second);
                Require(registration.State==ForgeWeaveRegistrationState.Failed && first.Count==1 && second.Count==0);

                ForgeApi.Connect(third);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error==null);
                Require(first.Count==0 && second.Count==0 && third.Count==1);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
            test("ForgeWeave reentrant reconnect cannot be overwritten by an older callback",()=>{
            ForgeApi.Disconnect();
            var first=new RetryableUnregisterEventRegistry();
            var outerHost=new RetryableUnregisterEventRegistry();
            var reentrantHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.reentrant.reconnect",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(first);
                first.OnNextUnregister=()=>ForgeApi.Connect(reentrantHost);
                ForgeApi.Connect(outerHost);

                Require(ReferenceEquals(ForgeApi.Registry,reentrantHost));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error==null);
                Require(first.Count==0 && outerHost.Count==0 && reentrantHost.Count==1);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave reentrant Register removes the stale generation",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.reentrant.reconnect",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            try
            {
                ForgeApi.Connect(staleHost);

                Require(ReferenceEquals(ForgeApi.Registry,currentHost));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error==null);
                Require(staleHost.Count==0 && currentHost.Count==1);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && staleHost.Count==0 && currentHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave stale reentrant Register cleanup can be retried",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.reentrant.retry",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            staleHost.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(staleHost);

                Require(ReferenceEquals(ForgeApi.Registry,currentHost));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error!=null);
                Require(staleHost.Count==1 && currentHost.Count==1);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && staleHost.Count==0 && currentHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave stale Register failure preserves the known current host",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var laterHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.reentrant.throw",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            staleHost.ThrowNextRegisterAfterAdd=true;
            try
            {
                ForgeApi.Connect(staleHost);

                Require(ReferenceEquals(ForgeApi.Registry,currentHost));
                Require(registration.State==ForgeWeaveRegistrationState.Failed && registration.Error is InvalidOperationException);
                Require(staleHost.Count==1 && currentHost.Count==1 && laterHost.Count==0);

                ForgeApi.Connect(laterHost);
                Require(ReferenceEquals(ForgeApi.Registry,laterHost));
                Require(registration.State==ForgeWeaveRegistrationState.Failed && registration.Error is InvalidOperationException);
                Require(staleHost.Count==1 && currentHost.Count==1 && laterHost.Count==0);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && registration.Error is InvalidOperationException);
                Require(staleHost.Count==1 && currentHost.Count==0 && laterHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave current Register failure after add fails closed without ID rollback",()=>{
            ForgeApi.Disconnect();
            var uncertainHost=new RetryableUnregisterEventRegistry {ThrowNextRegisterAfterAdd=true};
            var laterHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.current.throw",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                ForgeApi.Connect(uncertainHost);
                Require(registration.State==ForgeWeaveRegistrationState.Failed && registration.Error is InvalidOperationException);
                Require(uncertainHost.Count==1 && laterHost.Count==0);

                ForgeApi.Connect(laterHost);
                Require(registration.State==ForgeWeaveRegistrationState.Failed && registration.Error is InvalidOperationException);
                Require(uncertainHost.Count==1 && laterHost.Count==0);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && registration.Error is InvalidOperationException);
                Require(uncertainHost.Count==1 && laterHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave stale cleanup error does not overwrite a reentrant newer registration",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var replacementHost=new RetryableUnregisterEventRegistry();
            var triggerHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.cleanup.pending.reentrant",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            staleHost.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(staleHost);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error!=null && staleHost.Count==1 && currentHost.Count==1);

                staleHost.OnNextUnregister=()=>ForgeApi.Connect(replacementHost);
                staleHost.ThrowNextUnregister=true;
                ForgeApi.Connect(triggerHost);

                Require(ReferenceEquals(ForgeApi.Registry,replacementHost));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error!=null);
                Require(staleHost.Count==1 && currentHost.Count==0 && replacementHost.Count==1 && triggerHost.Count==0);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed);
                Require(staleHost.Count==0 && replacementHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave immediate stale cleanup error preserves a reentrant newer registration",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var replacementHost=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.cleanup.immediate.reentrant",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            staleHost.OnNextUnregister=()=>ForgeApi.Connect(replacementHost);
            staleHost.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(staleHost);

                Require(ReferenceEquals(ForgeApi.Registry,replacementHost));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error!=null);
                Require(staleHost.Count==1 && currentHost.Count==0 && replacementHost.Count==1);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed);
                Require(staleHost.Count==0 && replacementHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave disposal ignores a reentrant replacement host",()=>{
            ForgeApi.Disconnect();
            var activeHost=new RetryableUnregisterEventRegistry();
            var replacementHost=new RetryableUnregisterEventRegistry();
            ForgeApi.Connect(activeHost);
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.dispose.reentrant.connect",ForgeEventKind.ForgeReady,_=>{});
            activeHost.OnNextUnregister=()=>ForgeApi.Connect(replacementHost);
            try
            {
                Require(registration.State==ForgeWeaveRegistrationState.Registered && activeHost.Count==1);
                registration.Dispose();

                Require(registration.State==ForgeWeaveRegistrationState.Disposed);
                Require(ReferenceEquals(ForgeApi.Registry,replacementHost));
                Require(activeHost.Count==0 && replacementHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave reentrant disposal during stale cleanup prevents outer registration",()=>{
            ForgeApi.Disconnect();
            var staleHost=new RetryableUnregisterEventRegistry();
            var currentHost=new RetryableUnregisterEventRegistry();
            var triggerHost=new RetryableUnregisterEventRegistry {ThrowNextRegisterAfterAdd=true};
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.dispose.during.pending.cleanup",ForgeEventKind.ForgeReady,_=>{});
            staleHost.OnNextRegister=()=>ForgeApi.Connect(currentHost);
            staleHost.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(staleHost);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && staleHost.Count==1 && currentHost.Count==1);

                staleHost.OnNextUnregister=()=>registration.Dispose();
                ForgeApi.Connect(triggerHost);

                Require(registration.State==ForgeWeaveRegistrationState.Disposed && registration.Error==null);
                Require(staleHost.Count==0 && currentHost.Count==0 && triggerHost.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave reentrant pending cleanup tolerates nested list mutations",()=>{
            ForgeApi.Disconnect();
            var staleA=new RetryableUnregisterEventRegistry();
            var staleB=new RetryableUnregisterEventRegistry();
            var staleC=new RetryableUnregisterEventRegistry();
            var staleD=new RetryableUnregisterEventRegistry();
            var currentE=new RetryableUnregisterEventRegistry();
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.unregister.reentrant.pending.list",ForgeEventKind.ForgeReady,_=>{});
            staleA.OnNextRegister=()=>ForgeApi.Connect(staleB);
            staleB.OnNextRegister=()=>ForgeApi.Connect(staleC);
            staleA.ThrowNextUnregister=true;
            staleB.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(staleA);
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error!=null && staleA.Count==1 && staleB.Count==1 && staleC.Count==1);

                staleA.OnNextUnregister=()=>ForgeApi.Connect(currentE);
                ForgeApi.Connect(staleD);

                Require(ReferenceEquals(ForgeApi.Registry,currentE));
                Require(registration.State==ForgeWeaveRegistrationState.Registered && registration.Error==null);
                Require(staleA.Count==0 && staleB.Count==0 && staleC.Count==0 && staleD.Count==0 && currentE.Count==1);
                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && currentE.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave disposal can retry a failed host unregistration",()=>{
            ForgeApi.Disconnect();
            var engine=new RetryableUnregisterEventRegistry();
            ForgeApi.Connect(engine);
            var registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.dispose.retry",ForgeEventKind.ForgeReady,_=>{});
            try
            {
                Require(registration.State==ForgeWeaveRegistrationState.Registered && engine.Count==1);
                engine.ThrowNextUnregister=true;
                Throws<InvalidOperationException>(()=>registration.Dispose());
                Require(registration.State==ForgeWeaveRegistrationState.Registered && engine.Count==1);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && engine.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave registration can retry cleanup after reentrant disposal",()=>{
            ForgeApi.Disconnect();
            var engine=new RetryableUnregisterEventRegistry();
            ForgeWeaveRegistration registration=null;
            registration=ForgeCampaignEvents.SubscribeWeaveWhenAvailable("weave.register.dispose.retry",ForgeEventKind.ForgeReady,_=>{});
            engine.OnNextRegister=()=>registration.Dispose();
            engine.ThrowNextUnregister=true;
            try
            {
                ForgeApi.Connect(engine);
                Require(registration.State==ForgeWeaveRegistrationState.Failed);
                Require(registration.Error is InvalidOperationException && engine.Count==1);

                registration.Dispose();
                Require(registration.State==ForgeWeaveRegistrationState.Disposed && engine.Count==0);
            }
            finally {registration.Dispose();ForgeApi.Disconnect();}
        });
        test("ForgeWeave orders priorities and equal-priority dependencies deterministically",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("priority",ForgeEventKind.ForgeReady,calls,20));
            engine.Register(CreateHandler("zeta",ForgeEventKind.ForgeReady,calls,10,before:new[]{"alpha"}));
            engine.Register(CreateHandler("alpha",ForgeEventKind.ForgeReady,calls,10));
            engine.Register(CreateHandler("beta",ForgeEventKind.ForgeReady,calls,10));
            var result=Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(result.Status=="Completed" && calls.SequenceEqual(new[]{"priority","beta","zeta","alpha"}));
        });
        test("ForgeWeave invalidates cached plans on registration changes and preserves in-flight dispatch snapshots",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("seed",ForgeEventKind.ForgeReady,calls));
            Dispatch(engine,ForgeEventKind.ForgeReady); // Warm the plan before changing the registry.

            calls.Clear();
            engine.Register(CreateHandler("added",ForgeEventKind.ForgeReady,calls));
            var afterRegistration=Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(afterRegistration.InvokedCount==2 && calls.SequenceEqual(new[]{"added","seed"}));
            Require(engine.CaptureForgeWeave().HandlerCount==2);

            calls.Clear();
            Require(engine.Unregister("added"));
            var afterRemoval=Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(afterRemoval.InvokedCount==1 && calls.SequenceEqual(new[]{"seed"}));
            Require(engine.CaptureForgeWeave().HandlerCount==1);

            var reentrantCalls=new List<string>();var reentrantEngine=new TestEngine();
            var addDuringDispatch=true;
            reentrantEngine.Register(CreateHandler("a.add",ForgeEventKind.ForgeReady,reentrantCalls,observed:_=>{
                if(!addDuringDispatch)return;
                addDuringDispatch=false;
                reentrantEngine.Register(CreateHandler("b.added",ForgeEventKind.ForgeReady,reentrantCalls));
            }));
            var first=reentrantEngine.DispatchForgeEvent(ForgeEventKind.ForgeReady,Context.Campaign,new Services(),1,new Dictionary<string,string>(),CancellationToken.None);
            Require(first.InvokedCount==1 && reentrantCalls.SequenceEqual(new[]{"a.add"}));
            reentrantCalls.Clear();
            var second=Dispatch(reentrantEngine,ForgeEventKind.ForgeReady);
            Require(second.InvokedCount==2 && reentrantCalls.SequenceEqual(new[]{"a.add","b.added"}));

            var removalCalls=new List<string>();var removalEngine=new TestEngine();
            var removeDuringDispatch=true;
            removalEngine.Register(CreateHandler("a.remove",ForgeEventKind.ForgeReady,removalCalls,observed:_=>{
                if(!removeDuringDispatch)return;
                removeDuringDispatch=false;
                Require(removalEngine.Unregister("b.target"));
            }));
            removalEngine.Register(CreateHandler("b.target",ForgeEventKind.ForgeReady,removalCalls));
            var removalFirst=Dispatch(removalEngine,ForgeEventKind.ForgeReady);
            Require(removalFirst.InvokedCount==2 && removalCalls.SequenceEqual(new[]{"a.remove","b.target"}));
            removalCalls.Clear();
            var removalSecond=Dispatch(removalEngine,ForgeEventKind.ForgeReady);
            Require(removalSecond.InvokedCount==1 && removalCalls.SequenceEqual(new[]{"a.remove"}));
        });
        test("ForgeWeave custom topics build independent plans without topic leakage",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("custom.economy",ForgeEventKind.Custom,calls,topic:"economy.*"));
            engine.Register(CreateHandler("custom.military",ForgeEventKind.Custom,calls,topic:"military.*"));

            var economy=engine.DispatchCustomForgeEvent("economy.trade.buy",new Services(),0,new Dictionary<string,string>(),CancellationToken.None);
            Require(economy.InvokedCount==1 && calls.SequenceEqual(new[]{"custom.economy"}));
            calls.Clear();
            var military=engine.DispatchCustomForgeEvent("military.siege.start",new Services(),0,new Dictionary<string,string>(),CancellationToken.None);
            Require(military.InvokedCount==1 && calls.SequenceEqual(new[]{"custom.military"}));
            calls.Clear();
            var economyAgain=engine.DispatchCustomForgeEvent("economy.tax.levy",new Services(),0,new Dictionary<string,string>(),CancellationToken.None);
            Require(economyAgain.InvokedCount==1 && calls.SequenceEqual(new[]{"custom.economy"}));
        });
        test("ForgeWeave Snapshot refreshes finite plan findings after registration changes",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("snapshot.dependent",ForgeEventKind.ForgeReady,calls,before:new[]{"snapshot.target"}));

            var missingTarget=engine.CaptureForgeWeave();
            var blocked=missingTarget.Handlers.Single(handler=>handler.Id=="snapshot.dependent");
            Require(blocked.Status=="Blocked" && !string.IsNullOrWhiteSpace(blocked.BlockingReason));
            Require(missingTarget.Findings.Any(finding=>finding.Code=="forgeweave_order_missing"));

            engine.Register(CreateHandler("snapshot.target",ForgeEventKind.ForgeReady,calls));
            var registeredTarget=engine.CaptureForgeWeave();
            var ready=registeredTarget.Handlers.Single(handler=>handler.Id=="snapshot.dependent");
            Require(registeredTarget.HandlerCount==2 && ready.Status=="Ready" && ready.BlockingReason==null);
            Require(!registeredTarget.Findings.Any(finding=>finding.Code=="forgeweave_order_missing"));

            Require(engine.Unregister("snapshot.target"));
            var removedTarget=engine.CaptureForgeWeave();
            var blockedAgain=removedTarget.Handlers.Single(handler=>handler.Id=="snapshot.dependent");
            Require(removedTarget.HandlerCount==1 && blockedAgain.Status=="Blocked" && !string.IsNullOrWhiteSpace(blockedAgain.BlockingReason));
            Require(removedTarget.Findings.Any(finding=>finding.Code=="forgeweave_order_missing"));
        });
        test("ForgeWeave wide dispatch planning preserves deterministic order and reports a benchmark",()=>{
            const int rootCount=8;
            const int targetsPerRoot=24;
            const int dispatchCount=12;
            const int handlerCount=rootCount+rootCount*targetsPerRoot;
            var calls=new List<string>(handlerCount*dispatchCount);
            var engine=new TestEngine();
            for(var root=rootCount-1;root>=0;root--)
            {
                var targets=new List<string>(targetsPerRoot);
                for(var target=targetsPerRoot-1;target>=0;target--)
                    targets.Add("b.target."+(root*targetsPerRoot+target).ToString("D3",CultureInfo.InvariantCulture));
                engine.Register(CreateHandler("a.root."+root.ToString("D3",CultureInfo.InvariantCulture),ForgeEventKind.ForgeReady,calls,before:targets));
            }
            for(var target=handlerCount-rootCount-1;target>=0;target--)
                engine.Register(CreateHandler("b.target."+target.ToString("D3",CultureInfo.InvariantCulture),ForgeEventKind.ForgeReady,calls));

            var services=new Services();
            var data=new Dictionary<string,string>{{"source","benchmark"}};
            var total=Stopwatch.StartNew();
            var cold=Stopwatch.StartNew();
            var coldResult=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,Context.Campaign,services,1,data,CancellationToken.None);
            cold.Stop();
            Require(coldResult.Status=="Completed");

            var warm=Stopwatch.StartNew();
            for(var dispatch=1;dispatch<dispatchCount;dispatch++)
            {
                var result=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,Context.Campaign,services,1,data,CancellationToken.None);
                Require(result.Status=="Completed");
            }
            warm.Stop();
            total.Stop();

            Console.WriteLine("PERF ForgeWeave.DispatchPlan handlers="+handlerCount+
                " dispatches="+dispatchCount+
                " cold_ms="+cold.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture)+
                " warm_ms="+warm.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture)+
                " total_ms="+total.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture));
            Require(calls.Count==handlerCount*dispatchCount);
            for(var dispatch=0;dispatch<dispatchCount;dispatch++)
            {
                var offset=dispatch*handlerCount;
                for(var root=0;root<rootCount;root++)
                    Require(calls[offset+root]=="a.root."+root.ToString("D3",CultureInfo.InvariantCulture));
                for(var target=0;target<rootCount*targetsPerRoot;target++)
                    Require(calls[offset+rootCount+target]=="b.target."+target.ToString("D3",CultureInfo.InvariantCulture));
            }
        });
        test("ForgeWeave Snapshot and handler health measurements cover registry and retained history sizes",()=>{
            var handlerCounts=new[]{1,64,256};
            var historyCounts=new[]{0,1,64};
            const int snapshotIterations=8;
            const int healthIterations=512;
            foreach(var handlerCount in handlerCounts)
            foreach(var historyCount in historyCounts)
            {
                var calls=new List<string>(handlerCount*historyCount);
                var engine=new TestEngine();
                for(var handler=0;handler<handlerCount;handler++)
                {
                    var id="perf.snapshot."+handler.ToString("D3",CultureInfo.InvariantCulture);
                    engine.Register(CreateHandler(id,ForgeEventKind.ForgeReady,calls));
                }
                for(var dispatch=0;dispatch<historyCount;dispatch++)
                    Dispatch(engine,ForgeEventKind.ForgeReady);
                Require(calls.Count==handlerCount*historyCount);

                var lastId="perf.snapshot."+(handlerCount-1).ToString("D3",CultureInfo.InvariantCulture);
                var warmSnapshot=engine.CaptureForgeWeave();
                Require(warmSnapshot.HandlerCount==handlerCount);
                Require(warmSnapshot.RecentDispatches.Count==historyCount);
                Require(warmSnapshot.ReplayRecords.Count==historyCount);

                var snapshotTimer=Stopwatch.StartNew();
                for(var iteration=0;iteration<snapshotIterations;iteration++)
                {
                    var snapshot=engine.CaptureForgeWeave();
                    Require(snapshot.HandlerCount==handlerCount && snapshot.Handlers.Count==handlerCount);
                    Require(snapshot.RecentDispatches.Count==historyCount && snapshot.ReplayRecords.Count==historyCount);
                }
                snapshotTimer.Stop();
                Console.WriteLine("PERF ForgeWeave.Snapshot handlers="+handlerCount+
                    " history="+historyCount+
                    " iterations="+snapshotIterations+
                    " total_ms="+snapshotTimer.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture)+
                    " per_call_ms="+(snapshotTimer.Elapsed.TotalMilliseconds/snapshotIterations).ToString("F4",CultureInfo.InvariantCulture));

                var healthTimer=Stopwatch.StartNew();
                for(var iteration=0;iteration<healthIterations;iteration++)
                {
                    var health=engine.GetForgeWeaveHandlerHealth(lastId);
                    Require(health!=null && health.Id==lastId && health.InvocationCount==historyCount);
                }
                healthTimer.Stop();
                Console.WriteLine("PERF ForgeWeave.GetHandlerHealth handlers="+handlerCount+
                    " history="+historyCount+
                    " queries="+healthIterations+
                    " total_ms="+healthTimer.Elapsed.TotalMilliseconds.ToString("F3",CultureInfo.InvariantCulture)+
                    " per_call_ms="+(healthTimer.Elapsed.TotalMilliseconds/healthIterations).ToString("F4",CultureInfo.InvariantCulture));
            }
        });
        test("ForgeWeave blocks cyclic ordering instead of guessing",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("cycle.a",ForgeEventKind.ForgeReady,calls,0,before:new[]{"cycle.b"}));
            engine.Register(CreateHandler("cycle.b",ForgeEventKind.ForgeReady,calls,0,before:new[]{"cycle.a"}));
            engine.Register(CreateHandler("independent",ForgeEventKind.ForgeReady,calls));
            var result=Dispatch(engine,ForgeEventKind.ForgeReady);var snapshot=engine.CaptureForgeWeave();
            Require(calls.SequenceEqual(new[]{"independent"}) && result.SkippedCount==2 && snapshot.BlockedHandlerCount==2 && snapshot.Findings.Any(f=>f.Code=="forgeweave_order_cycle"));
        });
        test("ForgeWeave isolates errors and quarantines a repeated failure",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("failure",ForgeEventKind.ForgeReady,calls,0,fail:true,failureLimit:2));
            engine.Register(CreateHandler("healthy",ForgeEventKind.ForgeReady,calls));
            Dispatch(engine,ForgeEventKind.ForgeReady);Dispatch(engine,ForgeEventKind.ForgeReady);var third=Dispatch(engine,ForgeEventKind.ForgeReady);
            var failed=engine.CaptureForgeWeave().Handlers.Single(row=>row.Id=="failure");
            Require(calls.Count(value=>value=="healthy")==3 && failed.Quarantined() && failed.FailureCount==2 && third.QuarantinedCount==1);
        });
        test("ForgeWeave applies the existing campaign test gate to writers",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("campaign.write",ForgeEventKind.CampaignStarted,calls,access:ForgeEventAccess.CampaignWrite,context:Context.Campaign,changesState:true));
            var blocked=Dispatch(engine,ForgeEventKind.CampaignStarted);engine.TestingEnabled=true;engine.CampaignCopyConfirmed=true;var allowed=Dispatch(engine,ForgeEventKind.CampaignStarted);
            Require(blocked.SkippedCount==1 && allowed.InvokedCount==1 && calls.SequenceEqual(new[]{"campaign.write"}));
        });
        test("ForgeWeave preserves the logical source context for ContextLeaving",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("campaign.cleanup",ForgeEventKind.ContextLeaving,calls,context:Context.Campaign));
            var services=new Services {CurrentContext=Context.Mission};
            var result=engine.DispatchForgeEvent(ForgeEventKind.ContextLeaving,Context.Campaign,services,1,new Dictionary<string,string>{{"from","Campaign"},{"to","Mission"}},CancellationToken.None);
            Require(result.InvokedCount==1 && calls.SequenceEqual(new[]{"campaign.cleanup"}));
        });
        test("ForgeWeave copies bounded scalar event data",()=>{
            var data=new Dictionary<string,string>();for(var index=0;index<40;index++)data["key"+index]=new string('x',600);
            var eventData=new ForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,1,data,CancellationToken.None);data["key0"]="changed";
            Require(eventData.Data.Count==32 && eventData.Data["key0"].Length==513 && eventData.Data["key0"]!="changed" && typeof(ForgeEvent).GetProperty("Services")==null && typeof(ForgeEvent).GetProperty("Data").PropertyType==typeof(IReadOnlyDictionary<string,string>));
        });
        test("ForgeWeave Pulse handlers cannot run every frame",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("pulse",ForgeEventKind.Pulse,calls,minimumInterval:250));
            var first=Dispatch(engine,ForgeEventKind.Pulse);var second=Dispatch(engine,ForgeEventKind.Pulse);
            var health=engine.CaptureForgeWeave().Handlers.Single(row=>row.Id=="pulse");
            Require(first.InvokedCount==1 && second.InvokedCount==0 && second.SkippedCount==1 && calls.Count==1 && health.MinimumIntervalMilliseconds==250 && health.LastOutcome.StartsWith("Skipped: Pulse interval"));
        });
        test("ForgeWeave filters copied scalar data before invocation",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            var filter=new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{"suite","alpha"}}};
            engine.Register(CreateHandler("filter.alpha",ForgeEventKind.ForgeReady,calls,filter:filter));
            filter.RequiredData["suite"]="mutated after registration";
            var mismatch=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,new Dictionary<string,string>{{"suite","beta"}},CancellationToken.None);
            var match=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,new Dictionary<string,string>{{"suite","alpha"}},CancellationToken.None);
            var health=engine.CaptureForgeWeave().Handlers.Single(row=>row.Id=="filter.alpha");
            Require(mismatch.InvokedCount==0 && mismatch.HandlerOutcomes.Any(outcome=>outcome.Status=="Filtered") && match.InvokedCount==1 && calls.SequenceEqual(new[]{"filter.alpha"}));
            Require(health.Filter.RequiredData.Count==1 && health.Filter.RequiredData["suite"]=="alpha");
        });
        test("ForgeWeave applies the same filter to retained replay data",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();
                engine.Register(CreateHandler("filter.replay",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live,filter:new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{"kind","expected"}}}));
                var original=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,new Dictionary<string,string>{{"kind","other"}},CancellationToken.None);
                var replay=replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                Require(replay.Replayed && replay.InvokedCount==0 && replay.HandlerOutcomes.Any(outcome=>outcome.Status=="Filtered") && calls.Count==0);
            });
        });
        test("ForgeWeave rejects oversized or malformed event filters",()=>{
            var tooMany=new Dictionary<string,string>();for(var index=0;index<9;index++)tooMany["key"+index]="value";
            Throws(()=>new TestEngine().Register(CreateHandler("filter.too_many",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {RequiredData=tooMany})));
            Throws(()=>new TestEngine().Register(CreateHandler("filter.empty_key",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{" ","value"}}})));
            Throws(()=>new TestEngine().Register(CreateHandler("filter.long_value",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {RequiredData=new Dictionary<string,string>{{"key",new string('x',513)}}})));
        });
        test("ForgeWeave records declared execution budget overruns without aborting",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("budget.warn",ForgeEventKind.ForgeReady,calls,budgetMilliseconds:1,observed:@event=>Thread.Sleep(4)));
            var result=Dispatch(engine,ForgeEventKind.ForgeReady);var snapshot=engine.CaptureForgeWeave();var health=snapshot.Handlers.Single(row=>row.Id=="budget.warn");
            Require(result.InvokedCount==1 && result.BudgetExceededCount==1 && result.HandlerOutcomes.Single().BudgetExceeded && result.HandlerOutcomes.Single().Status=="OverBudget" && snapshot.BudgetExceededCount==1 && health.BudgetExceededCount==1 && health.LastOutcome=="OverBudget" && calls.Count==1);
        });
        test("ForgeWeave validates bounded execution budget declarations",()=>{
            Throws(()=>new TestEngine().Register(CreateHandler("budget.too_high",ForgeEventKind.ForgeReady,new List<string>(),budgetMilliseconds:5001)));
            Throws(()=>new TestEngine().Register(CreateHandler("budget.invalid_policy",ForgeEventKind.ForgeReady,new List<string>(),budgetPolicy:(ForgeBudgetPolicy)99)));
        });
        test("ForgeWeave stops only later framework handlers",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("a.stop",ForgeEventKind.ForgeReady,calls,stop:true));engine.Register(CreateHandler("b.later",ForgeEventKind.ForgeReady,calls));
            var result=Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(result.PropagationStopped && calls.SequenceEqual(new[]{"a.stop"}) && result.SkippedCount==1);
        });
        test("ForgeWeave rejects unsafe writer and Pulse declarations",()=>{
            var engine=new TestEngine();
            Throws(()=>engine.Register(CreateHandler("unsafe.observe",ForgeEventKind.ForgeReady,new List<string>(),changesState:true)));
            Throws(()=>engine.Register(CreateHandler("unsafe.pulse",ForgeEventKind.Pulse,new List<string>(),minimumInterval:0)));
        });
        test("ForgeWeave retains bounded, detached replay evidence",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();
                engine.Register(CreateHandler("record.observe",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.ObserveOnly));
                var data=new Dictionary<string,string>();
                for(var index=0;index<40;index++)data["key"+index]=new string('x',600);
                var original=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,data,CancellationToken.None);
                data["key0"]="mutated by caller";
                var first=replays.Records.Single(record=>record.Sequence==original.Sequence);
                Require(first.Data.Count==32 && first.Data["key0"].Length==513 && first.Data["key0"]!="mutated by caller" && first.OriginalHandlerOutcomes.Count==1);
                first.Data["key0"]="mutated returned record";
                first.OriginalHandlerOutcomes[0].Status="mutated returned outcome";
                var fresh=replays.Records.Single(record=>record.Sequence==original.Sequence);
                Require(fresh.Data["key0"]!="mutated returned record" && fresh.OriginalHandlerOutcomes[0].Status!="mutated returned outcome");
                for(var index=0;index<64;index++)Dispatch(engine,ForgeEventKind.ForgeReady);
                Require(replays.Records.Count==64 && !replays.Records.Any(record=>record.Sequence==original.Sequence));
            });
        });
        test("ForgeWeave replay invokes only handlers that explicitly opt in",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();var replaySignals=new List<string>();
                engine.Register(CreateHandler("replay.disabled",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Disabled));
                engine.Register(CreateHandler("replay.observe",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.ObserveOnly,observed:@event=>replaySignals.Add("observe:"+@event.IsReplay+":"+@event.SourceSequence)));
                engine.Register(CreateHandler("replay.live",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live,observed:@event=>replaySignals.Add("live:"+@event.IsReplay+":"+@event.SourceSequence)));
                var original=Dispatch(engine,ForgeEventKind.ForgeReady);
                var replay=replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                Require(replay.Replayed && replay.SourceSequence==original.Sequence && replay.ReplaySequence.HasValue);
                Require(calls.Count(id=>id=="replay.disabled")==1 && calls.Count(id=>id=="replay.observe")==2 && calls.Count(id=>id=="replay.live")==2);
                Require(replaySignals.Contains("observe:True:"+original.Sequence) && replaySignals.Contains("live:True:"+original.Sequence));
                Require(replay.HandlerOutcomes.Any(outcome=>outcome.HandlerId=="replay.disabled" && outcome.Status=="Skipped"));
                var health=engine.CaptureForgeWeave().Handlers.ToDictionary(handler=>handler.Id);
                Require(health["replay.disabled"].ReplayMode==ForgeReplayMode.Disabled && health["replay.observe"].ReplayMode==ForgeReplayMode.ObserveOnly && health["replay.live"].ReplayMode==ForgeReplayMode.Live);
            });
        });
        test("ForgeWeave replay rejects a retained event outside its original context",()=>{
            WithReplays((engine,replays)=>{
                var services=new Services {CurrentContext=Context.Campaign};
                engine.Register(CreateHandler("replay.campaign",ForgeEventKind.CampaignStarted,new List<string>(),context:Context.Campaign,replayMode:ForgeReplayMode.ObserveOnly));
                var original=engine.DispatchForgeEvent(ForgeEventKind.CampaignStarted,services,1,new Dictionary<string,string>{{"source","context"}},CancellationToken.None);
                services.CurrentContext=Context.Mission;
                var rejected=replays.Replay(original.Sequence,services,CancellationToken.None);
                var snapshot=engine.CaptureForgeWeave();
                Require(!rejected.Replayed && rejected.Status=="Rejected" && (rejected.RejectionReason??"").Contains("Campaign") && snapshot.ReplayAttemptCount==1 && snapshot.ReplayRejectedCount==1);
            });
        });
        test("ForgeWeave replay keeps existing campaign writer gates",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();var services=new Services {CurrentContext=Context.Campaign};
                engine.Register(CreateHandler("replay.writer",ForgeEventKind.CampaignStarted,calls,access:ForgeEventAccess.CampaignWrite,context:Context.Campaign,changesState:true,replayMode:ForgeReplayMode.Live));
                var original=engine.DispatchForgeEvent(ForgeEventKind.CampaignStarted,services,1,new Dictionary<string,string>{{"source","writer"}},CancellationToken.None);
                var denied=replays.Replay(original.Sequence,services,CancellationToken.None);
                Require(denied.Replayed && denied.InvokedCount==0 && denied.HandlerOutcomes.Any(outcome=>outcome.HandlerId=="replay.writer" && outcome.Status=="Rejected" && (outcome.Reason??"").Contains("Enable testing")) && calls.Count==0);
                engine.TestingEnabled=true;engine.CampaignCopyConfirmed=true;
                var allowed=replays.Replay(original.Sequence,services,CancellationToken.None);
                Require(allowed.Replayed && allowed.InvokedCount==1 && calls.SequenceEqual(new[]{"replay.writer"}));
            });
        });
        test("ForgeWeave replay never records a replay as another source",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();
                engine.Register(CreateHandler("replay.loop",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live));
                var original=Dispatch(engine,ForgeEventKind.ForgeReady);
                var replay=replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                var snapshot=engine.CaptureForgeWeave();
                Require(replay.Replayed && snapshot.ReplayRecordCount==1 && replays.Records.Count==1);
                Require(snapshot.RecentDispatches.Any(entry=>entry.IsReplay && entry.SourceSequence==original.Sequence));
                var stale=replays.Replay(replay.ReplaySequence.Value,new Services(),CancellationToken.None);
                snapshot=engine.CaptureForgeWeave();
                Require(!stale.Replayed && stale.Status=="Rejected" && snapshot.ReplayRecordCount==1 && snapshot.ReplayAttemptCount==2 && snapshot.ReplaySuccessCount==1 && snapshot.ReplayRejectedCount==1);
            });
        });
        test("ForgeWeave isolates replay failures and quarantines the failing handler",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();
                engine.Register(CreateHandler("replay.failure",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live,failOnReplay:true,failureLimit:1));
                engine.Register(CreateHandler("replay.healthy",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live));
                var original=Dispatch(engine,ForgeEventKind.ForgeReady);
                var first=replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                var second=replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                var failed=engine.CaptureForgeWeave().Handlers.Single(row=>row.Id=="replay.failure");
                Require(first.Replayed && first.FailureCount==1 && first.HandlerOutcomes.Any(outcome=>outcome.HandlerId=="replay.failure" && outcome.Status=="Failed"));
                Require(second.Replayed && second.QuarantinedCount==1 && failed.Quarantined() && failed.FailureCount==1);
                Require(calls.Count(id=>id=="replay.failure")==2 && calls.Count(id=>id=="replay.healthy")==3);
            });
        });
        test("ForgeWeave replay evidence survives report normalization and is safely rendered",()=>{
            var report=new SessionReport {ForgeWeave=new ForgeWeaveSnapshot {
                Status="Replay ready",ReplayRecordCount=1,ReplayAttemptCount=1,ReplaySuccessCount=1,
                Handlers=new List<ForgeWeaveHandlerHealth>{new ForgeWeaveHandlerHealth {Id="replay.handler",Module="forgeweave.tests",Event=ForgeEventKind.ForgeReady,ReplayMode=ForgeReplayMode.Live}},
                ReplayRecords=new List<ForgeReplayRecord>{new ForgeReplayRecord {Sequence=7,Event=ForgeEventKind.ForgeReady,Context=Context.Any,Data=new Dictionary<string,string>{{"payload","<record>"}},OriginalHandlerOutcomes=new List<ForgeHandlerOutcome>{new ForgeHandlerOutcome {HandlerId="replay.handler",Status="Completed"}}}},
                RecentReplays=new List<ForgeReplayResult>{new ForgeReplayResult {SourceSequence=7,ReplaySequence=8,Event=ForgeEventKind.ForgeReady,Context=Context.Any,Replayed=true,Status="<outcome>",HandlerOutcomes=new List<ForgeHandlerOutcome>{new ForgeHandlerOutcome {HandlerId="replay.handler",Status="Completed"}}}}
            }};
            var workspace=new ReportWorkspace();workspace.Open(report,"Replay report");
            var html=ReportHtml.Render(report);
            Require(workspace.Section("framework","").Contains("ReplayRecords") && html.Contains("Replay") && html.Contains("&lt;record&gt;") && html.Contains("&lt;outcome&gt;") && !html.Contains("<record>") && !html.Contains("<outcome>"));
        });
        test("ForgeWeave unregisters handlers dynamically by ID and instance",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            var handlerA=CreateHandler("unreg.a",ForgeEventKind.ForgeReady,calls);
            var handlerB=CreateHandler("unreg.b",ForgeEventKind.ForgeReady,calls);
            engine.Register(handlerA);
            engine.Register(handlerB);
            Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(calls.Count==2 && calls.Contains("unreg.a") && calls.Contains("unreg.b"));
            calls.Clear();
            var removedA=engine.Unregister("unreg.a");
            var removedAgainA=engine.Unregister("unreg.a");
            Require(removedA && !removedAgainA);
            var removedB=engine.Unregister(handlerB);
            var removedAgainB=engine.Unregister(handlerB);
            Require(removedB && !removedAgainB);
            Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(calls.Count==0);
            engine.Register(CreateHandler("unreg.a",ForgeEventKind.ForgeReady,calls));
            Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(calls.SequenceEqual(new[]{"unreg.a"}));
        });
        test("ForgeWeave resets quarantines by module or globally",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("quarantine.mod1",ForgeEventKind.ForgeReady,calls,fail:true,failureLimit:1));
            Dispatch(engine,ForgeEventKind.ForgeReady);
            var health=engine.GetForgeWeaveHandlerHealth("quarantine.mod1");
            Require(health!=null && health.Quarantined());
            var restoredOther=engine.ResetModuleForgeWeaveQuarantines("other_module");
            Require(restoredOther==0 && engine.GetForgeWeaveHandlerHealth("quarantine.mod1").Quarantined());
            var restoredModule=engine.ResetModuleForgeWeaveQuarantines("forgeweave.tests");
            Require(restoredModule==1 && !engine.GetForgeWeaveHandlerHealth("quarantine.mod1").Quarantined());
            Dispatch(engine,ForgeEventKind.ForgeReady);
            Require(engine.GetForgeWeaveHandlerHealth("quarantine.mod1").Quarantined());
            var restoredAll=engine.ResetAllForgeWeaveQuarantines();
            Require(restoredAll==1 && !engine.GetForgeWeaveHandlerHealth("quarantine.mod1").Quarantined());
        });
        test("ForgeWeave filters events using ExcludedData negative criteria",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            var filter=new ForgeEventFilter {
                RequiredData=new Dictionary<string,string>{{"tier","elite"}},
                ExcludedData=new Dictionary<string,string>{{"state","dead"},{"faction","outlaw"}}
            };
            engine.Register(CreateHandler("filter.excluded",ForgeEventKind.ForgeReady,calls,filter:filter));
            var excluded=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,new Dictionary<string,string>{{"tier","elite"},{"state","dead"}},CancellationToken.None);
            Require(excluded.InvokedCount==0 && excluded.HandlerOutcomes.Any(o=>o.Status=="Filtered") && calls.Count==0);
            var matching=engine.DispatchForgeEvent(ForgeEventKind.ForgeReady,new Services(),1,new Dictionary<string,string>{{"tier","elite"},{"state","alive"}},CancellationToken.None);
            Require(matching.InvokedCount==1 && calls.SequenceEqual(new[]{"filter.excluded"}));
            var tooMany=new Dictionary<string,string>();for(var index=0;index<9;index++)tooMany["key"+index]="value";
            Throws(()=>new TestEngine().Register(CreateHandler("filter.ex_too_many",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {ExcludedData=tooMany})));
            Throws(()=>new TestEngine().Register(CreateHandler("filter.ex_empty_key",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {ExcludedData=new Dictionary<string,string>{{" ","value"}}})));
            Throws(()=>new TestEngine().Register(CreateHandler("filter.ex_long_val",ForgeEventKind.ForgeReady,new List<string>(),filter:new ForgeEventFilter {ExcludedData=new Dictionary<string,string>{{"k",new string('y',513)}}})));
        });
        test("ForgeWeave tracks MinMilliseconds and returns handler health",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("health.metrics",ForgeEventKind.ForgeReady,calls));
            Dispatch(engine,ForgeEventKind.ForgeReady);
            Dispatch(engine,ForgeEventKind.ForgeReady);
            var health=engine.GetForgeWeaveHandlerHealth("health.metrics");
            Require(health!=null && health.Id=="health.metrics" && health.InvocationCount==2 && health.MinMilliseconds>=0);
            var snapshot=engine.CaptureForgeWeave();
            var eventHealth=snapshot.Events.Single(e=>e.Event==ForgeEventKind.ForgeReady);
            Require(eventHealth.MinMilliseconds>=0);
            var missing=engine.GetForgeWeaveHandlerHealth("nonexistent");
            Require(missing==null);
        });
        test("ForgeWeave clears journal and replay histories",()=>{
            WithReplays((engine,replays)=>{
                var calls=new List<string>();
                engine.Register(CreateHandler("history.handler",ForgeEventKind.ForgeReady,calls,replayMode:ForgeReplayMode.Live));
                var original=Dispatch(engine,ForgeEventKind.ForgeReady);
                replays.Replay(original.Sequence,new Services(),CancellationToken.None);
                var snapshotBefore=engine.CaptureForgeWeave();
                Require(snapshotBefore.RecentDispatches.Count>0 && replays.Records.Count>0 && snapshotBefore.RecentReplays.Count>0);
                engine.ClearForgeWeaveJournal();
                var snapshotAfterJournal=engine.CaptureForgeWeave();
                Require(snapshotAfterJournal.RecentDispatches.Count==0 && snapshotAfterJournal.ReplayRecordCount>0);
                engine.ClearForgeWeaveReplays();
                var snapshotAfterReplays=engine.CaptureForgeWeave();
                Require(replays.Records.Count==0 && snapshotAfterReplays.RecentReplays.Count==0 && snapshotAfterReplays.ReplayRecordCount==0);
            });
        });
        test("ForgeWeave dispatches host lifecycle events GameLoaded and MissionEnded",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("host.loaded",ForgeEventKind.GameLoaded,calls));
            engine.Register(CreateHandler("host.mission_end",ForgeEventKind.MissionEnded,calls));
            var loadedResult=engine.DispatchForgeEvent(ForgeEventKind.GameLoaded,new Services(),1,new Dictionary<string,string>{{"save","savegame_001"}},CancellationToken.None);
            Require(loadedResult.InvokedCount==1 && calls.Contains("host.loaded"));
            var missionResult=engine.DispatchForgeEvent(ForgeEventKind.MissionEnded,new Services(),1,new Dictionary<string,string>{{"scene","battle_terrain_01"}},CancellationToken.None);
            Require(missionResult.InvokedCount==1 && calls.Contains("host.mission_end"));
        });
        test("ForgeWeave publishes custom events with exact and wildcard topic matching",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("handler.exact",ForgeEventKind.Custom,calls,topic:"economy.trade.buy"));
            engine.Register(CreateHandler("handler.wildcard",ForgeEventKind.Custom,calls,topic:"economy.*"));
            engine.Register(CreateHandler("handler.all",ForgeEventKind.Custom,calls,topic:"*"));
            engine.Register(CreateHandler("handler.military",ForgeEventKind.Custom,calls,topic:"military.*"));

            // Publish exact topic match
            var res1 = engine.DispatchCustomForgeEvent("economy.trade.buy", new Services(), 0, new Dictionary<string, string>{{"item","iron_ore"}}, CancellationToken.None);
            Require(res1.InvokedCount == 3);
            Require(calls.Contains("handler.exact") && calls.Contains("handler.wildcard") && calls.Contains("handler.all") && !calls.Contains("handler.military"));

            calls.Clear();
            // Publish matching wildcard only
            var res2 = engine.DispatchCustomForgeEvent("economy.tax.levy", new Services(), 0, new Dictionary<string, string>{{"denars","500"}}, CancellationToken.None);
            Require(res2.InvokedCount == 2);
            Require(!calls.Contains("handler.exact") && calls.Contains("handler.wildcard") && calls.Contains("handler.all") && !calls.Contains("handler.military"));

            calls.Clear();
            // Publish military topic
            var res3 = engine.DispatchCustomForgeEvent("military.siege.assault", new Services(), 0, new Dictionary<string, string>{{"settlement","pravend"}}, CancellationToken.None);
            Require(res3.InvokedCount == 2);
            Require(calls.Contains("handler.all") && calls.Contains("handler.military") && !calls.Contains("handler.wildcard"));
        });
        test("ForgeApi.PublishCustomEvent dispatches to custom event subscribers",()=>{
            var engine=new TestEngine();ForgeApi.Connect(engine);
            try
            {
                var calls=new List<string>();
                string receivedValue = null;
                engine.Register(CreateHandler("custom.subscriber",ForgeEventKind.Custom,calls,topic:"combat.duel.*",observed:ev=>{
                    if (ev.Data.TryGetValue("challenger", out var ch)) receivedValue = ch;
                }));

                bool delivered = ForgeApi.PublishCustomEvent("combat.duel.challenge", new[] { new KeyValuePair<string, string>("challenger", "derthert") });
                Require(delivered);
                Require(calls.Contains("custom.subscriber"));
                Require(receivedValue == "derthert");

                bool unmatched = ForgeApi.PublishCustomEvent("diplomacy.peace", new[] { new KeyValuePair<string, string>("kingdom", "battania") });
                Require(!unmatched);
            }
            finally { ForgeApi.Disconnect(); }
        });
        test("ForgeWeave smart circuit breaker transitions Closed -> Open -> HalfOpen -> Closed",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            bool shouldFail = true;
            engine.Register(CreateHandler("breaker.handler",ForgeEventKind.ForgeReady,calls,failureLimit:2,circuitBreakerCooldownSeconds:1,failCondition:()=>shouldFail));

            // Run 1: fail #1 (Closed)
            Dispatch(engine, ForgeEventKind.ForgeReady);
            var h1 = engine.GetForgeWeaveHandlerHealth("breaker.handler");
            Require(h1.CircuitState == "Closed" && h1.FailureCount == 1 && !h1.Quarantined());

            // Run 2: fail #2 -> reaches failureLimit -> transitions to Open (Quarantined)
            Dispatch(engine, ForgeEventKind.ForgeReady);
            var h2 = engine.GetForgeWeaveHandlerHealth("breaker.handler");
            Require(h2.CircuitState == "Open" && h2.FailureCount == 2 && h2.Quarantined());

            // Run 3: immediate call -> still in cooldown -> skipped
            var res3 = Dispatch(engine, ForgeEventKind.ForgeReady);
            Require(res3.QuarantinedCount == 1 && res3.InvokedCount == 0);
            var h3 = engine.GetForgeWeaveHandlerHealth("breaker.handler");
            Require(h3.CircuitState == "Open");

            // Sleep past cooldown
            Thread.Sleep(1100);

            // Now, fix the error so probe succeeds
            shouldFail = false;

            // Run 4: probationary probe triggers in HalfOpen -> succeeds -> transitions to Closed
            var res4 = Dispatch(engine, ForgeEventKind.ForgeReady);
            Require(res4.InvokedCount == 1);
            var h4 = engine.GetForgeWeaveHandlerHealth("breaker.handler");
            Require(h4.CircuitState == "Closed" && !h4.Quarantined() && h4.ConsecutiveFailureCount == 0);
        });
        test("ForgeWeave smart circuit breaker applies exponential backoff when probationary probe fails",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("backoff.handler",ForgeEventKind.ForgeReady,calls,failureLimit:1,circuitBreakerCooldownSeconds:1,fail:true));

            // Run 1: fail -> Open (cooldown: 1s)
            Dispatch(engine, ForgeEventKind.ForgeReady);
            var h1 = engine.GetForgeWeaveHandlerHealth("backoff.handler");
            Require(h1.CircuitState == "Open" && h1.Quarantined());

            // Sleep past 1s cooldown
            Thread.Sleep(1100);

            // Run 2: HalfOpen probe -> fails again -> backoff to Open (cooldown doubled to 2s)
            Dispatch(engine, ForgeEventKind.ForgeReady);
            var h2 = engine.GetForgeWeaveHandlerHealth("backoff.handler");
            Require(h2.CircuitState == "Open" && h2.Quarantined() && h2.LastOutcome.Contains("Probe failed; circuit open (cooldown: 2s)"));
        });
        test("ForgeWeave permanent quarantine policy stays quarantined without auto-recovery",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("perm.handler",ForgeEventKind.ForgeReady,calls,failureLimit:1,circuitBreakerPolicy:ForgeCircuitBreakerPolicy.PermanentQuarantine,fail:true));

            Dispatch(engine, ForgeEventKind.ForgeReady);
            var h1 = engine.GetForgeWeaveHandlerHealth("perm.handler");
            Require(h1.Quarantined() && h1.CircuitState == "Open");

            Thread.Sleep(50);
            var res2 = Dispatch(engine, ForgeEventKind.ForgeReady);
            Require(res2.QuarantinedCount == 1 && res2.InvokedCount == 0);
        });
        test("ForgeWeave tracks APM latency percentiles and histogram distribution",()=>{
            var calls=new List<string>();var engine=new TestEngine();
            engine.Register(CreateHandler("apm.handler",ForgeEventKind.ForgeReady,calls,observed:ev=>{
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 2) { }
            }));

            for (int i = 0; i < 10; i++)
            {
                Dispatch(engine, ForgeEventKind.ForgeReady);
            }

            var h = engine.GetForgeWeaveHandlerHealth("apm.handler");
            Require(h.InvocationCount == 10);
            Require(h.P50Milliseconds > 0);
            Require(h.P95Milliseconds >= h.P50Milliseconds);
            Require(h.P99Milliseconds >= h.P95Milliseconds);
            Require((h.Bucket1To5Ms + h.Bucket5To20Ms + h.BucketOver20Ms) == 10);
        });
    }

    static ForgeWeaveDispatchResult Dispatch(TestEngine engine,ForgeEventKind kind)=>engine.DispatchForgeEvent(kind,new Services(),1,new Dictionary<string,string>{{"source","test"}},CancellationToken.None);
    static void WithReplays(Action<TestEngine,IForgeReplayRegistry> action)
    {
        var engine=new TestEngine();ForgeApi.Connect(engine);
        try {Require(ForgeApi.Replays!=null);action(engine,ForgeApi.Replays);}
        finally {ForgeApi.Disconnect();}
    }
    static EventHandler CreateHandler(string id,ForgeEventKind kind,List<string> calls,int priority=0,IEnumerable<string> before=null,ForgeEventAccess access=ForgeEventAccess.Observe,Context context=Context.Any,bool changesState=false,bool fail=false,bool stop=false,int failureLimit=3,int minimumInterval=0,ForgeReplayMode replayMode=ForgeReplayMode.Disabled,bool failOnReplay=false,Action<ForgeEvent> observed=null,ForgeEventFilter filter=null,int budgetMilliseconds=0,ForgeBudgetPolicy budgetPolicy=ForgeBudgetPolicy.Warn,string topic="",ForgeCircuitBreakerPolicy circuitBreakerPolicy=ForgeCircuitBreakerPolicy.AutoRecover,int circuitBreakerCooldownSeconds=5,Func<bool> failCondition=null)
    {
        return new EventHandler(new ForgeEventSubscription {
            Descriptor=new Descriptor {Id=id,Module="forgeweave.tests",Name=id,Context=context,ChangesState=changesState},Event=kind,Topic=topic,Priority=priority,
            Before=before==null?new List<string>():before.ToList(),After=new List<string>(),Access=access,ReplayMode=replayMode,Filter=filter??new ForgeEventFilter(),BudgetMilliseconds=budgetMilliseconds,BudgetPolicy=budgetPolicy,FailureLimit=failureLimit,MinimumIntervalMilliseconds=minimumInterval,
            CircuitBreakerPolicy=circuitBreakerPolicy,CircuitBreakerCooldownSeconds=circuitBreakerCooldownSeconds
        },@event=>{
            calls.Add(id);
            observed?.Invoke(@event);
            if(stop)@event.StopPropagation("test stop");
            if(fail || (failCondition!=null && failCondition()) || (failOnReplay && @event.IsReplay))
                throw new InvalidOperationException("test failure");
        });
    }
    static void Require(bool value){if(!value)throw new Exception("ForgeWeave assertion failed");}
    static void Throws(Action action){try{action();}catch{return;}throw new Exception("Expected ForgeWeave rejection");}
    static TException Throws<TException>(Action action) where TException:Exception
    {
        try {action();}
        catch(TException error) {return error;}
        throw new Exception("Expected "+typeof(TException).Name);
    }

    class EmptyRegistry:IForgeRegistry
    {
        public void Register(ITestCase test) { }
        public void Register(ICommand command) { }
        public void Register(IDiagnosticProvider provider) { }
    }

    sealed class FailingEventRegistry:EmptyRegistry,IForgeEventRegistry
    {
        public int RegisterAttempts { get; private set; }
        public void Register(IForgeEventHandler handler)
        {
            RegisterAttempts++;
            throw new InvalidOperationException("test registry rejected ForgeWeave handler");
        }
        public bool Unregister(string id)=>false;
        public bool Unregister(IForgeEventHandler handler)=>false;
        public bool PublishCustom(string topic,IEnumerable<KeyValuePair<string,string>> data=null)=>false;
    }

    sealed class RetryableUnregisterEventRegistry:EmptyRegistry,IForgeEventRegistry
    {
        readonly Dictionary<string,IForgeEventHandler> handlers=new Dictionary<string,IForgeEventHandler>(StringComparer.Ordinal);
        public int Count=>handlers.Count;
        public bool ThrowNextUnregister { get; set; }
        public bool ThrowNextRegisterAfterAdd { get; set; }
        public Action OnNextRegister { get; set; }
        public Action OnNextUnregister { get; set; }
        public void Register(IForgeEventHandler handler)
        {
            var id=handler?.Subscription?.Descriptor?.Id;
            if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("handler ID required");
            if(handlers.ContainsKey(id))throw new ArgumentException("duplicate handler ID");
            var callback=OnNextRegister;
            OnNextRegister=null;
            callback?.Invoke();
            handlers.Add(id,handler);
            if(ThrowNextRegisterAfterAdd)
            {
                ThrowNextRegisterAfterAdd=false;
                throw new InvalidOperationException("test host threw after adding the ForgeWeave handler");
            }
        }
        public bool Unregister(string id)=>!string.IsNullOrWhiteSpace(id) && handlers.Remove(id);
        public bool Unregister(IForgeEventHandler handler)
        {
            var callback=OnNextUnregister;
            OnNextUnregister=null;
            callback?.Invoke();
            if(ThrowNextUnregister)
            {
                ThrowNextUnregister=false;
                throw new InvalidOperationException("test host could not unregister handler");
            }
            return handler!=null && Unregister(handler.Subscription?.Descriptor?.Id);
        }
        public bool PublishCustom(string topic,IEnumerable<KeyValuePair<string,string>> data=null)=>false;
    }

    sealed class EventHandler:IForgeEventHandler
    {
        readonly Action<ForgeEvent> action;
        public ForgeEventSubscription Subscription { get; }
        public EventHandler(ForgeEventSubscription subscription,Action<ForgeEvent> action) {Subscription=subscription;this.action=action;}
        public void Handle(ForgeEvent @event) {action(@event);}
    }
    sealed class Services:ITestServices
    {
        public Context CurrentContext { get; set; }=Context.Campaign;
        public bool IsCampaignActive=>CurrentContext==Context.Campaign;
        public object GetService(Type type)=>null;
        public void Register(string module,string level,string message) { }
    }
    static bool Quarantined(this ForgeWeaveHandlerHealth health)=>health.Status=="Quarantined";
}
