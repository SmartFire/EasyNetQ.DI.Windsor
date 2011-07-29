﻿using System;
using System.Collections.Generic;
using System.IO;

namespace EasyNetQ.Hosepipe
{
    public class FileMessageWriter : IMessageWriter
    {
        public void Write(IEnumerable<string> messages, QueueParameters parameters)
        {
            var count = 0;
            foreach (string message in messages)
            {
                var fileName = parameters.QueueName + "." + count.ToString() + ".message.txt";
                var path = Path.Combine(parameters.MessageFilePath, fileName);
                if(File.Exists(path))
                {
                    Console.WriteLine("Overwriting existing messsage file: {0}", path);
                }
                File.WriteAllText(path, message);
                count++;
            }
        }
    }
}