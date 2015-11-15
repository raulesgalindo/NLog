﻿// 
// Copyright (c) 2004-2011 Jaroslaw Kowalski <jaak@jkowalski.net>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

#if !SILVERLIGHT && !MONO

namespace NLog.UnitTests.Targets
{
    using System.Diagnostics;
    using NLog.Config;
    using NLog.Targets;
    using System;
    using System.Linq;
    using Xunit;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using NLog.Layouts;

    public class EventLogTargetTests : NLogTestBase
    {
        private const int MaxMessageSize = 16384;

        [Fact]
        public void WriteEventLogEntryTrace()
        {
            WriteEventLogEntry2(LogLevel.Trace, EventLogEntryType.Information);
        }

        [Fact]
        public void WriteEventLogEntryDebug()
        {
            WriteEventLogEntry2(LogLevel.Debug, EventLogEntryType.Information);
        }

        [Fact]
        public void WriteEventLogEntryInfo()
        {
            WriteEventLogEntry2(LogLevel.Info, EventLogEntryType.Information);
        }
        [Fact]
        public void WriteEventLogEntryWarn()
        {
            WriteEventLogEntry2(LogLevel.Warn, EventLogEntryType.Warning);
        }

        [Fact]
        public void WriteEventLogEntryError()
        {
            WriteEventLogEntry2(LogLevel.Error, EventLogEntryType.Error);
        }
        [Fact]
        public void WriteEventLogEntryFatal()
        {
            WriteEventLogEntry2(LogLevel.Fatal, EventLogEntryType.Error);
        }


        [Fact]
        public void WriteEventLogEntryFatalCustomEntryType()
        {
            WriteEventLogEntry2(LogLevel.Warn, EventLogEntryType.SuccessAudit, new SimpleLayout("SuccessAudit"));
        }

        [Fact]
        public void WriteEventLogEntryFatalCustomEntryTyp_caps()
        {
            WriteEventLogEntry2(LogLevel.Warn, EventLogEntryType.SuccessAudit, new SimpleLayout("SUCCESSAUDIT"));
        }

        [Fact]
        public void WriteEventLogEntryFatalCustomEntryTyp_fallback()
        {
            WriteEventLogEntry2(LogLevel.Warn, EventLogEntryType.Warning, new SimpleLayout("fallback to auto determined"));
        }

        [Fact]
        public void WriteEventLogEntryFatalCustomEntryTyp_error()
        {
            WriteEventLogEntry2(LogLevel.Debug, EventLogEntryType.Error, new SimpleLayout("error"));
        }

        [Fact]
        public void WriteEventLogEntryLargerThanMaxMessageSizeWithOverflowTruncate_TruncatesTheMessage()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize + 1));
            string loggedMessage = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Truncate, message).Single();

            Assert.Equal(MaxMessageSize, loggedMessage.Length);
        }

        [Fact]
        public void WriteEventLogEntryEqualToMaxMessageSizeWithOverflowTruncate_TheMessageIsNotTruncated()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize));
            string loggedMessage = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Truncate, message).Single();

            Assert.Equal(MaxMessageSize, loggedMessage.Length);
        }
        
        [Fact]
        public void WriteEventLogEntryLargerThanMaxMessageSizeWithOverflowMultipleEntries_TheMessageIsSplit()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize + 1));
            var chunks = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Multiple, message).ToList();

            Assert.Equal(2, chunks.Count);
            Assert.Equal(MaxMessageSize, chunks[0].Length);
            Assert.Equal(1, chunks[1].Length);
        }

        [Fact]
        public void WriteEventLogEntryEqualToMaxMessageSizeWithOverflowMultipleEntries_TheMessageIsNotSplit()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize));
            var loggedMessage = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Multiple, message).Single();

            Assert.Equal(MaxMessageSize, loggedMessage.Length);
        }

        [Fact]
        public void WriteEventLogEntryEqualToMaxMessageSizeWithOverflowIgnore_TheMessageIsWritten()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize));
            string loggedMessage = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Ignore, message).Single();

            Assert.Equal(MaxMessageSize, loggedMessage.Length);
        }

        [Fact]
        public void WriteEventLogEntryLargerThanMaxMessageSizeWithOverflowIgnore_TheMessageIsSkipped()
        {
            string message = string.Join("", Enumerable.Repeat("a", MaxMessageSize + 1));
            bool wasWritten = WriteEventLogEntryForOverflow(EventLogTargetOverlowAction.Ignore, message).Any();

            Assert.False(wasWritten);
        }

        private static IEnumerable<string> WriteEventLogEntryForOverflow(EventLogTargetOverlowAction overflowAction, string message)
        {
            string sourceName = Guid.NewGuid().ToString();
            var target = CreateEventLogTarget(null, sourceName);
            target.OnOverflow = overflowAction;
            target.Layout = new SimpleLayout("${message}");
            LoggingConfiguration config = new LoggingConfiguration();
            LoggingRule rule = new LoggingRule("*", LogLevel.Info, target);
            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;

            var logger = LogManager.GetLogger("WriteEventLogEntryOverflow");
            var el = new EventLog(target.Log);

            var loggedNotBefore = DateTime.Now.AddMinutes(-1);
            logger.Log(LogLevel.Info, message);

            var entries = GetEventRecords(el.Log).TakeWhile(e => e.TimeCreated > loggedNotBefore && e.ProviderName == sourceName).ToList();

            var chunks = from entry in entries
                         from prop in entry.Properties
                         let msg = Convert.ToString(prop.Value)
                         where msg.StartsWith("a")
                         orderby msg descending
                         select msg;

            return chunks;
        }

        private static void WriteEventLogEntry2(LogLevel logLevel, EventLogEntryType eventLogEntryType, Layout entryType = null)
        {
            var target = CreateEventLogTarget(entryType);
            SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Trace);
            var logger = LogManager.GetLogger("WriteEventLogEntry");
            var el = new EventLog(target.Log);

            var loggedNotBefore = DateTime.Now.AddMinutes(-1);

            var testValue = Guid.NewGuid().ToString();
            logger.Log(logLevel, testValue);

            var entries = GetEventRecords(el.Log).TakeWhile(e => e.TimeCreated > loggedNotBefore).ToList();
            //debug-> error
            var expectedSource = target.GetFixedSource();
            EntryExists(entries, testValue, expectedSource, eventLogEntryType);
        }

        private static EventLogTarget CreateEventLogTarget(Layout entryType = null, string sourceName = "NLog.UnitTests")
        {
            var target = new EventLogTarget();
            //The Log to write to is intentionally lower case!!
            target.Log = "application";
            // set the source explicitly to prevent random AppDomain name being used as the source name
            target.Source = sourceName;
            if (entryType != null)
            {
                //set only when not default
                target.EntryType = entryType;
            }
            return target;
        }

        private static void EntryExists(IEnumerable<EventRecord> entries, string expectedValue, string expectedSource, EventLogEntryType expectedEntryType)
        {
            var entryExists = entries
                .Any(entry =>
                    entry.ProviderName == expectedSource &&
                    HasEntryType(entry, expectedEntryType) &&
                    entry.Properties.Any(prop => Convert.ToString(prop.Value).Contains(expectedValue))
                    );

            Assert.True(entryExists, string.Format(
                "Failed to find entry of type '{1}' from source '{0}' containing text '{2}' in the message", 
                expectedSource, expectedEntryType, expectedValue));
        }


        private static IEnumerable<EventRecord> GetEventRecords(string logName)
        {
            var query = new EventLogQuery(logName, PathType.LogName) { ReverseDirection = true };
            using (var reader = new EventLogReader(query))
                for (var eventInstance = reader.ReadEvent(); eventInstance != null; eventInstance = reader.ReadEvent())
                    yield return eventInstance;
        }

        private static bool HasEntryType(EventRecord eventRecord, EventLogEntryType entryType)
        {
            var keywords = (StandardEventKeywords) (eventRecord.Keywords ?? 0);
            var level = (StandardEventLevel) (eventRecord.Level ?? 0);
            bool isClassicEvent = keywords.HasFlag(StandardEventKeywords.EventLogClassic);
            switch (entryType)
            {
                case EventLogEntryType.Error:
                    return isClassicEvent && level == StandardEventLevel.Error;
                case EventLogEntryType.Warning:
                    return isClassicEvent && level == StandardEventLevel.Warning;
                case EventLogEntryType.Information:
                    return isClassicEvent && level == StandardEventLevel.Informational;
                case EventLogEntryType.SuccessAudit:
                    return keywords.HasFlag(StandardEventKeywords.AuditSuccess);
                case EventLogEntryType.FailureAudit:
                    return keywords.HasFlag(StandardEventKeywords.AuditFailure);
            }
            return false;
        }

    }
}

#endif