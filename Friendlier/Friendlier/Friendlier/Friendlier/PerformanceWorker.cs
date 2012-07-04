#region File Description
//-----------------------------------------------------------------------------
// PerformanceWorker.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Management;

namespace Xyglo
{
        /// <summary>
        /// We have a PerformanceWorker thread for initialising the (slow to start) PerformanceCounters.
        /// This means that our main thread can start up nice and quickly.
        /// </summary>
        public class PerformanceWorker
        {
            /// <summary>
            /// Initialise the performance counters
            /// </summary>
            public void initialise()
            {
                // Initialise the m_cpuCounter
                //
                m_cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                // And likewise the Memory counter
                //
                m_memCounter = new PerformanceCounter("Memory", "Available MBytes");
            }

            /// <summary>
            /// Return a CounterSample for the CPU
            /// </summary>
            /// <returns></returns>
            public CounterSample getCpuSample()
            {
                return m_cpuCounter.NextSample();
            }

            /// <summary>
            /// Return a CounterSample for the Memory
            /// </summary>
            /// <returns></returns>
            public CounterSample getMemorySample()
            {
                return m_memCounter.NextSample();
            }

            /// <summary>
            /// This method is called when the thread is started
            /// </summary>
            public void startWorking()
            {
                if (m_cpuCounter == null && m_memCounter == null)
                {
                    Logger.logMsg("PerformanceWorker::startWorking() - initialising the worker thread for performance counters");
                    initialise();
                }

                while (!m_shouldStop)
                {
                    Thread.Sleep(50); // sleep for 50ms
                }

                Console.WriteLine("PerformanceWorker::startWorking() - terminating gracefully");
            }

            /// <summary>
            /// Stop this thread
            /// </summary>
            public void requestStop()
            {
                m_cpuCounter = null;
                m_memCounter = null;
                m_shouldStop = true;
            }

            /// <summary>
            /// Volatile is used as hint to the compiler that this data member will be accessed by multiple threads.
            /// </summary>
            private volatile bool m_shouldStop;

            /// <summary>
            /// CPU counter
            /// </summary>
            public volatile PerformanceCounter m_cpuCounter;

            /// <summary>
            /// Memory counter
            /// </summary>
            public volatile PerformanceCounter m_memCounter;
        }
}
