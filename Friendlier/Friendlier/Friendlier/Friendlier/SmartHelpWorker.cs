#region File Description
//-----------------------------------------------------------------------------
// SmartHelpWorker.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

//#define SMART_HELP_DEBUG

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
    /// A structure that represents a request to do something in the SmartHelpWorker thread
    /// </summary>
    public struct SmartHelpRequest
    {
        public SyntaxManager m_syntaxManager;
        public FileBuffer m_fileBuffer;
        public int m_startLine;
        public int m_endLine;
        public DateTime m_creationTime;
    };

    /// <summary>
    /// SmartHelpWorker status
    /// </summary>
    public enum SmartHelpStatus
    {
        Quiescent,                     // just sittin' around
        ProcessingHighlights           // currently processing highlights in thread
    };

    public class SmartHelpWorker
    {
        /// <summary>
        /// Volatile is used as hint to the compiler that this data member will be accessed by multiple threads.
        /// </summary>
        private volatile bool m_shouldStop = false;

        /// <summary>
        /// Current status of this helper
        /// </summary>
        protected volatile SmartHelpStatus m_status = SmartHelpStatus.Quiescent;

        /// <summary>
        /// List of requests
        /// </summary>
        protected volatile List<SmartHelpRequest> m_requestQueue = new List<SmartHelpRequest>();

        /// <summary>
        /// Initialise
        /// </summary>
        public void initialise()
        {
        }

        /// <summary>
        /// This method is called when the thread is started
        /// </summary>
        public void startWorking()
        {
            while (!m_shouldStop)
            {
                Thread.Sleep(10); // thread sleep in ms

                switch(m_status)
                {
                    case SmartHelpStatus.Quiescent:
                        if (m_requestQueue.Count > 0)
                        {
                            m_status = SmartHelpStatus.ProcessingHighlights;
                        }
                        break;

                    case SmartHelpStatus.ProcessingHighlights:
                        // Check that we have something to process
                        //
                        if (m_requestQueue.Count == 0)
                        {
                            Logger.logMsg("SmartHelpWorker::startWorking() - got a process request but nothing on queue");
                            m_status = SmartHelpStatus.Quiescent;
                            break;
                        }

                        // We're doing something now - generate highlighting in the background 
                        // (in this thread) and then copy it into the foreground thread (all in
                        // FileBuffer) once this processing has completed.  This is used for
                        // updating items that aren't being shown on the screen.
                        //
#if SMART_HELP_DEBUG
                        Logger.logMsg("SmartHelpWorker::startWorking() - picked up and processing highlights");
                        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                        sw.Start();
#endif
                        FilePosition startPosition = new FilePosition(0, m_requestQueue[0].m_startLine);
                        FilePosition endPosition = new FilePosition(m_requestQueue[0].m_fileBuffer.getLine(m_requestQueue[0].m_endLine).Length, m_requestQueue[0].m_endLine);
                        
                        // Now background regenerate everything - this must be interruptable and if it is interruppted (by other
                        // thread calling SyntaxManager.interruptProcessing() then the copyBackground will not happen (the
                        // generateAllHighlighting call will return false).
                        //
                        if (m_requestQueue[0].m_syntaxManager.generateAllHighlighting(m_requestQueue[0].m_fileBuffer, true))
                        {
                            // And copy once this completes succesfully
                            //
                            m_requestQueue[0].m_fileBuffer.copyBackgroundHighlighting();
                            Logger.logMsg("Updated all highlighting");
                        }

                        // Remove this instance of something to process
                        //
                        m_requestQueue.RemoveAt(0);

                        // And rest
                        //
                        m_status = SmartHelpStatus.Quiescent;

#if SMART_HELP_DEBUG
                        // Stop the timer
                        sw.Stop();
                        Logger.logMsg("SmartHelpWorker::startWorking() - completed SmartHelpStatus.ProcessingHighlights in " + sw.Elapsed.TotalMilliseconds + " ms");
#endif
                        break;

                    default:
                        // no idea what we're doing here
                        break;
                }
            }

            Console.WriteLine("SmartHelpWorker::startWorking() - terminating gracefully");
        }

        /// <summary>
        /// Update the syntax highlighting over a given range
        /// </summary>
        /// <param name="syntaxManager"></param>
        /// <param name="fileBuffer"></param>
        /// <returns></returns>
        public bool updateSyntaxHighlighting(SyntaxManager syntaxManager, FileBuffer fileBuffer, int startLine = -1, int endLine = -1)
        {
            // If we're busy doing something else then we can't do this right now
            //
            //if (m_status != SmartHelpStatus.Quiescent)
                //return false;

#if SMART_HELP_DEBUG
            Logger.logMsg("SmartHelpWorker::updateSyntaxHighlighting() - got request to process highlights");
#endif

            // Create a request
            //
            SmartHelpRequest shRequest = new SmartHelpRequest();
            shRequest.m_creationTime = DateTime.Now;
            shRequest.m_fileBuffer = fileBuffer;
            shRequest.m_syntaxManager = syntaxManager;
            shRequest.m_startLine = (startLine == -1) ? 0 : startLine;


            // Limit end line in case we don't have enough in the FileBuffer
            //
            if (endLine >= fileBuffer.getLineCount())
            {
                shRequest.m_endLine = Math.Max(fileBuffer.getLineCount() - 1, 0);
            }
            else
            {
                shRequest.m_endLine = ( endLine == -1 ) ? (Math.Max(fileBuffer.getLineCount() - 1, 0)) : endLine;
            }

            // Push the request on the queue
            //
            m_requestQueue.Add(shRequest);

            return true;
        }

        /// <summary>
        /// Stop this thread
        /// </summary>
        public void requestStop()
        {
            //m_cpuCounter = null;
            //m_memCounter = null;
            m_shouldStop = true;
        }

        /// <summary>
        /// Get the status of this thread
        /// </summary>
        /// <returns></returns>
        public SmartHelpStatus getStatus()
        {
            return m_status;
        }
    }
}
