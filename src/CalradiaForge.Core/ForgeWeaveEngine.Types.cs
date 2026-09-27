using System;
using System.Collections.Generic;
using CalradiaForge.Sdk;

namespace CalradiaForge.Core
{
    public sealed partial class ForgeWeaveEngine
    {
        sealed class RegisteredHandler
        {
            public readonly IForgeEventHandler Handler;
            public readonly ForgeEventSubscription Subscription;
            public bool Quarantined;
            public string BlockingReason,LastOutcome,LastError,LastInvokedAt;
            public int InvocationCount,FailureCount,BudgetExceededCount,ConsecutiveFailureCount;
            public double TotalMilliseconds,MaxMilliseconds,MinMilliseconds,LastMilliseconds;
            public string LastBudgetExceededAt;
            public long LastPulseTimestamp;

            // Circuit Breaker
            public string CircuitState = "Closed";
            public long LastStateChangeTimestamp;
            public int CurrentCooldownSeconds;

            // Latency Telemetry (Rolling 64-sample buffer + histogram buckets)
            public readonly double[] SampleTimings = new double[64];
            public int SampleCount;
            public int SampleIndex;
            public int BucketUnder1Ms, Bucket1To5Ms, Bucket5To20Ms, BucketOver20Ms;

            public RegisteredHandler(IForgeEventHandler handler,ForgeEventSubscription subscription)
            {
                Handler=handler;
                Subscription=subscription;
                CurrentCooldownSeconds = Math.Max(1, subscription.CircuitBreakerCooldownSeconds);
            }

            public void RecordTiming(double ms)
            {
                SampleTimings[SampleIndex] = ms;
                if (SampleCount < 64) SampleCount++;
                SampleIndex = (SampleIndex + 1) % 64;

                if (ms < 1.0) BucketUnder1Ms++;
                else if (ms < 5.0) Bucket1To5Ms++;
                else if (ms < 20.0) Bucket5To20Ms++;
                else BucketOver20Ms++;
            }

            public (double p50, double p95, double p99) CalculatePercentiles()
            {
                if (SampleCount == 0) return (0, 0, 0);
                var buffer = new double[SampleCount];
                Array.Copy(SampleTimings, buffer, SampleCount);
                Array.Sort(buffer);
                double p50 = buffer[(int)(SampleCount * 0.50)];
                double p95 = buffer[Math.Min((int)(SampleCount * 0.95), SampleCount - 1)];
                double p99 = buffer[Math.Min((int)(SampleCount * 0.99), SampleCount - 1)];
                return (p50, p95, p99);
            }
        }
        sealed class EventState
        {
            public ForgeEventKind Event;
            public int DispatchCount,HandlerInvocationCount,FailureCount,BudgetExceededCount,SkippedCount;
            public double TotalMilliseconds,MaxMilliseconds,MinMilliseconds;
            public string LastDispatchedAt;
        }
        sealed class OrderEdge
        {
            public readonly RegisteredHandler From,To;
            public OrderEdge(RegisteredHandler from,RegisteredHandler to) {From=from;To=to;}
        }
        sealed class DispatchPlan
        {
            public List<RegisteredHandler> Ordered=new List<RegisteredHandler>();
            public Dictionary<RegisteredHandler,string> Blocked=new Dictionary<RegisteredHandler,string>();
            public List<Finding> Findings=new List<Finding>();
        }
    }
}
