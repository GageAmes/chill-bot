﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using log4net;
using log4net.Config;

namespace Reiati.ChillBot
{
    /// <summary>
    /// Class containing the main entry point for the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// A logger.
        /// </summary>
        private static ILog logger;

        /// <summary>
        /// The client(s) used to connect to discord.
        /// </summary>
        private static DiscordShardedClient client;

        /// <summary>
        /// Main entry point for the application.
        /// </summary>
        public static void Main(string[] args)
        {
            if (!Program.TryStartLogger())
            {
                Console.WriteLine(string.Format(
                    "Application Exit - No logging config found;{{filePath:{0}}}",
                    HardCoded.Logging.ConfigFilePath));
                return;
            }

            Console.CancelKeyPress += 
                delegate(object sender, ConsoleCancelEventArgs args)
                {
                    Program.logger.Info("Shutdown initiated - console");
                    client?.StopAsync().GetAwaiter().GetResult();
                    client.Dispose();
                };

            try
            {
                Program.InitializeClient().GetAwaiter().GetResult();
                Task.Delay(Timeout.Infinite).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Program.logger.ErrorFormat(
                    "Shutdown initiated - exception thrown;{{message:{0},\nstack:{1}}}",
                    e.Message,
                    e.StackTrace);
            }
        }

        /// <summary>
        /// Tries to start the logger.
        /// </summary>
        private static bool TryStartLogger()
        {
            // Required for colored console output: https://issues.apache.org/jira/browse/LOG4NET-658
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var configFile = new FileInfo(HardCoded.Logging.ConfigFilePath);

            if (!configFile.Exists)
            {
                return false;
            }

            XmlConfigurator.Configure(configFile);
            Program.logger = LogManager.GetLogger("Bootstrap");
            Program.logger.InfoFormat("Logging configuration: {0}", configFile.FullName);
            return true;
        }

        /// <summary>
        /// Initializes the clients.
        /// </summary>
        /// <returns>When the clients have been initialized and started.</returns>
        private static async Task InitializeClient()
        {
            var config = new DiscordSocketConfig
            {
                TotalShards = 1,
            };

            var client = new DiscordShardedClient(config);
            client.Log += LogAsyncFromClient;
            client.ShardConnected += ReadyHandlerAsync;

            string token = await File.ReadAllTextAsync(HardCoded.Discord.TokenFilePath);

            await client.LoginAsync(Discord.TokenType.Bot, token.Trim());
            await client.StartAsync();
            Program.client = client;
        }

        /// <summary>
        /// Logs to the console when a shard is ready.
        /// </summary>
        /// <param name="shard">The shard which is ready.</param>
        /// <returns>When the task has completed.</returns>
        private static async Task ReadyHandlerAsync(DiscordSocketClient shard)
        {
            Program.logger.InfoFormat("Shard ready - {{shardId:{0}}}", shard.ShardId);

            shard.Log += LogAsyncFromShard;
            await Task.Delay(0);
        }

        /// <summary>
        /// Forwards all client logs to the console.
        /// </summary>
        /// <param name="log">The client log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task LogAsyncFromClient(Discord.LogMessage log)
        {
            Program.logger.DebugFormat("Client log - {0}", log.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Fowards all shard logs to the console.
        /// </summary>
        /// <param name="log">The shard log.</param>
        /// <returns>When the task has completed.</returns>
        private static Task LogAsyncFromShard(Discord.LogMessage log)
        {
            Program.logger.DebugFormat("Shard log - {0}", log.ToString());
            return Task.CompletedTask;
        }
    }
}