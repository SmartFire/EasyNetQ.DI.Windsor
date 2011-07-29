﻿// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class FileMessageWriterTests
    {
        [SetUp]
        public void SetUp() {}

        [Test, Explicit("Writes files to the file system")]
        public void WriteSomeFiles()
        {
            var writer = new FileMessageWriter();
            var messages = new List<string>
            {
                "This is message one",
                "This is message two",
                "This is message three"
            };

            var parameters = new QueueParameters
            {
                QueueName = "TheNameOfTheQueue",
                MessageFilePath = @"C:\temp\MessageOutput"
            };

            writer.Write(messages, parameters);
        }
 
    }
}

// ReSharper restore InconsistentNaming