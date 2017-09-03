﻿using Spectrum.API;
using Spectrum.API.Interfaces.Plugins;
using Spectrum.API.Interfaces.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spectrum.Plugins.ServerMod.Cmds;
using System;
using Spectrum.API.Configuration;
using System.Linq;
using System.IO;
using Spectrum.Plugins.ServerMod.CmdSettings;
using Spectrum.Plugins.ServerMod.PlaylistTools;
using Spectrum.Plugins.ServerMod.PlaylistTools.LevelFilters;
using Spectrum.Plugins.ServerMod.PlaylistTools.LevelFilters.Sorts;
using Spectrum.Plugins.ServerMod.Utilities;

namespace Spectrum.Plugins.ServerMod
{
    public class Entry : IPlugin
    {
        public string FriendlyName => "Server commands Mod";
        public string Author => "Corecii";
        public string Contact => "SteamID: Corecii; Discord: Corecii#3019";
        public APILevel CompatibleAPILevel => APILevel.XRay;

        public static string PluginVersion = "Version C.7.4.0";

        private static Settings Settings = new Settings(typeof(Entry));

        public void Initialize(IManager manager)
        {
            GeneralUtilities.testFunc(() =>
            {
                var levelFilters = new LevelFilter[]
                {
                    new LevelFilterAll(),
                    new LevelFilterAuthor(),
                    new LevelFilterCreated(),
                    new LevelFilterDifficulty(),
                    new LevelFilterIndex(),
                    new LevelFilterIsNew(),
                    new LevelFilterIsUpdated(),
                    new LevelFilterLast(),
                    new LevelFilterMode(),
                    new LevelFilterName(),
                    new LevelFilterPage(),
                    new LevelFilterPlaylist(),
                    new LevelFilterRegex(),
                    new LevelFilterSaved(),
                    new LevelFilterStars(),
                    new LevelFilterTime(),
                    new LevelFilterUpdated(),

                    new LevelSortFilterAuthor(),
                    new LevelSortFilterCreated(),
                    new LevelSortFilterDifficulty(),
                    new LevelSortFilterIndex(),
                    new LevelSortFilterMode(),
                    new LevelSortFilterName(),
                    new LevelSortFilterShuffle(),
                    new LevelSortFilterStars(),
                    new LevelSortFilterTime(),
                    new LevelSortFilterUpdated()
                };
                foreach (var filter in levelFilters)
                    FilteredPlaylist.AddFilterType(filter);
            });

            load();  // load existing data
            save();  // save defaults that were not loaded

            Events.Local.ChatSubmitMessage.Subscribe(data =>
            {
                GeneralUtilities.testFunc(() =>
                {
                    Chat_MessageSent(data.message_);
                });
            });

            Events.ClientToAllClients.ChatMessage.Subscribe(data =>
            {
                GeneralUtilities.testFunc(() =>
                {
                    var author = GeneralUtilities.ExtractMessageAuthor(data.message_);
                    var steamName = SteamworksManager.GetUserName().ToLower().Trim();
                    var profileName = G.Sys.PlayerManager_.Current_.profile_.Name_.ToLower().Trim();

                    if (!GeneralUtilities.IsSystemMessage(data.message_) && (author.ToLower().Trim() != steamName && author.ToLower().Trim() != profileName))
                        Chat_MessageReceived(author, GeneralUtilities.ExtractMessageBody(data.message_));
                });
            });

            Events.Network.ServerInitialized.Subscribe(data =>
            {
                G.Sys.GameManager_.StartCoroutine(serverInit());
            });
        }

        IEnumerator serverInit()
        {
            yield return new WaitForSeconds(1.0f);  // wait for the server to load
            if (Cmd.all.getCommand<UpdateCmd>().updateCheck)
                UpdateCmd.checkForUpdates(false);  // check for ServerMod updates
            Cmd.all.getCommand<SettingsCmd>().showNewSettings();  // show any new settings
            yield break;
        }

        private void Chat_MessageSent(string message)
        {
            var client = GeneralUtilities.localClient();
            if (client == null)
                Console.WriteLine("Error: Local client can't be found !");

            if (message.StartsWith("%"))
            {
                if (GeneralUtilities.isHost())
                    return;

                int pos = message.IndexOf(' ');
                string commandName = (pos > 0 ? message.Substring(1, pos) : message.Substring(1)).Trim();
                Cmd c = Cmd.all.getCommand(commandName);
                if (c == null)
                    return;
                if (!c.canUseAsClient && c.perm != PermType.LOCAL)
                {
                    MessageUtilities.sendMessage(client, "You can't use that command as client");
                    return;
                }
                exec(c, client, pos > 0 ? message.Substring(pos + 1).Trim() : "");
            }
            else
            {
                if (!message.StartsWith("!"))
                    return;

                if (message.ToLower().Trim() == "!plugin")
                {
                    printClient();
                    return;
                }

                if (!GeneralUtilities.isHost())
                    return;

                int pos = message.IndexOf(' ');
                string commandName = (pos > 0 ? message.Substring(1, pos) : message.Substring(1)).Trim();
                Cmd c = Cmd.all.getCommand(commandName);
                if (c == null)
                {
                    MessageUtilities.sendMessage(client,  "The command '" + commandName + "' doesn't exist.");
                    return;
                }

                exec(c, client, pos > 0 ? message.Substring(pos + 1).Trim() : "");
            }
        }

        private void Chat_MessageReceived(string author, string message)
        {
            if (!message.StartsWith("!"))
                return;

            if (message.ToLower().Trim() == "!plugin")
            {
                printClient();
                return;
            }

            if (!GeneralUtilities.isHost())
                return;

            var client = GeneralUtilities.clientFromName(author);
            if (client == null)
            {
                Console.WriteLine("Error: client can't be found");
                return;
            }
                
            int pos = message.IndexOf(' ');
            string commandName = (pos > 0 ? message.Substring(1, pos) : message.Substring(1)).Trim();
            Cmd c = Cmd.all.getCommand(commandName);

            if (c == null)
            {
                MessageUtilities.sendMessage(client, "The command '" + commandName + "' don't exist.");
                return;
            }

            if (c.perm == PermType.LOCAL)
                return;

            if(c.perm != PermType.ALL)
            {
                MessageUtilities.sendMessage(client, "You don't have the permission to do that!");
                return;
            }

            exec(c, client, pos > 0 ? message.Substring(pos + 1).Trim() : "");
        }

        private void exec(Cmd c, ClientPlayerInfo p, string message)
        {
            try
            {
                c.use(p, message);
            }
            catch (Exception error)
            {
                MessageUtilities.sendMessage("Error");
                Console.WriteLine(error);
            }
        }

        public void Shutdown()
        {
            
        }

        private void printClient()
        {
            MessageUtilities.sendMessage(GeneralUtilities.localClient().GetChatName() + " " + PluginVersion);
        }

        private static void reloadSettingsFromFile()
        {
            // NOTE: Code from Spectrum's Settings. Used because there is no provided method to reload settings.
            Type type = typeof(Entry);
            string postfix = "";
            string FileName;
            if (string.IsNullOrEmpty(postfix))
            {
                FileName = $"{type.Assembly.GetName().Name}.json";
            }
            else
            {
                FileName = $"{type.Assembly.GetName().Name}.{postfix}.json";
            }
            string FilePath = Path.Combine(Defaults.SettingsDirectory, FileName);

            if (File.Exists(FilePath))
            {
                using (var sr = new StreamReader(FilePath))
                {
                    var json = sr.ReadToEnd();
                    var reader = new JsonFx.Json.JsonReader();

                    Section sec = null;

                    try
                    {
                        sec = reader.Read<Section>(json);
                    }
                    catch
                    {
                    }

                    if (sec != null)
                    {
                        foreach (string k in sec.Keys)
                        {
                            Settings[k] = sec[k];
                        }
                    }
                }
            }
        }

        public static void reload()
        {
            reloadSettingsFromFile();
            load();
        }

        public static void load()
        {
            try
            {
                foreach (Cmd Command in Cmd.all.list())
                {
                    foreach (CmdSetting Setting in Command.settings)
                    {
                        if (Setting.FileId != "")
                        {
                            var value = Settings[Setting.FileId];
                            if (value != null)
                            {
                                UpdateResult result = Setting.UpdateFromObject(value);
                                if (!result.Valid)
                                    Console.WriteLine($"Invalid value for {Setting.FileId}: {result.Message}");
                                else if (result.Message != "")
                                    Console.WriteLine(result.Message);
                                Setting.Value = result.NewValue;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void save()
        {
            try
            {
                foreach (Cmd Command in Cmd.all.list())
                {
                    foreach (CmdSetting Setting in Command.settings)
                    {
                        if (Setting.FileId != "")
                        {
                            Settings[Setting.FileId] = Setting.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Settings.Save();
        }
    }
}
