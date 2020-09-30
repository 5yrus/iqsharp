// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Quantum.IQSharp
{
    public class PerformanceMonitor : IPerformanceMonitor
    {

        private readonly ILogger<PerformanceMonitor> Logger;
        private bool alive = false;
        private Thread? thread = null;

        /// <inheritdoc />
        public event EventHandler<SimulatorPerformanceArgs>? OnSimulatorPerformanceAvailable;

        /// <inheritdoc />
        public event EventHandler<KernelPerformanceArgs>? OnKernelPerformanceAvailable;

        public PerformanceMonitor(
            ILogger<PerformanceMonitor> logger
        )
        {
            Logger = logger;
        }

        /// <inheritdoc />
        public void Report()
        {

            var managedRamUsed = GC.GetTotalMemory(forceFullCollection: false);
            var totalRamUsed = Process.GetCurrentProcess().WorkingSet64;
            Logger.LogInformation(
                "Estimated RAM usage:" +
                "\n\tManaged: {Managed} bytes" +
                "\n\tTotal:   {Total} bytes",
                managedRamUsed,
                totalRamUsed
            );
            OnKernelPerformanceAvailable?.Invoke(this, new KernelPerformanceArgs(
                managedRamUsed, totalRamUsed
            ));
        }

        /// <inheritdoc />
        public void Start()
        {
            alive = true;
            thread = new Thread(EventLoop);
            thread.Start();
        }

        /// <inheritdoc />
        public void Join() => thread?.Join();

        /// <inheritdoc />
        public void Stop()
        {
            alive = false;
            thread?.Interrupt();
            Join();
            thread = null;
        }

        protected void EventLoop()
        {
            while (alive)
            {
                Report();
                Thread.Sleep(15000);
            }
        }

        /// <summary>
        ///      Given a new simulator performance record, emits an event with
        ///      that performance data.
        /// </summary>
        internal void ReportSimulatorPerformance(SimulatorPerformanceArgs args)
        {
            this.OnSimulatorPerformanceAvailable?.Invoke(this, args);
        }

    }
}
