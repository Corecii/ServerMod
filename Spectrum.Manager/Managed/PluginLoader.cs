﻿using System;
using System.IO;
using System.Reflection;
using Spectrum.API.PluginInterfaces;
using Spectrum.Manager.Logging;
using Spectrum.Manager.Resources;

namespace Spectrum.Manager.Managed
{
    class PluginLoader
    {
        private string PluginDirectory { get; }
        private PluginContainer PluginContainer { get; }

        private SubsystemLog Log { get; }

        public PluginLoader(string pluginDirectory, PluginContainer pluginContainer)
        {
            PluginDirectory = pluginDirectory;
            PluginContainer = pluginContainer;

            Log = new SubsystemLog(Path.Combine(DefaultValues.LogDirectory, DefaultValues.PluginLoaderLogFileName), true);
            Log.Info("New startup...");
        }

        public void LoadPlugins()
        {
            Log.Info("Starting load procedure.");
            var filePaths = Directory.GetFiles(PluginDirectory, "*.plugin.dll");

            foreach (var path in filePaths)
            {
                Assembly asm;
                try
                {
                    Log.Info($"Now loading library file: '{path}'");
                    asm = Assembly.LoadFrom(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception occured while loading library file: '{path}'. Check the log for details.");
                    Log.Exception(e);

                    continue;
                }

                var exportedTypes = asm.GetExportedTypes();
                foreach (var exportedType in exportedTypes)
                {
                    // All plugins MUST have a type named Entry.
                    if (exportedType.Name == "Entry")
                    {
                        Log.Info("Plugin contains a valid entry point. Proceeding...");
                        try
                        {
                            Log.Info("Trying to validate the plugin...");
                            // All plugins MUST implement IPlugin interface.
                            if (typeof (IPlugin).IsAssignableFrom(exportedType))
                            {
                                var plugin = (IPlugin) Activator.CreateInstance(exportedType);
                                var pluginInfo = new PluginInfo
                                {
                                    Name = plugin.FriendlyName,
                                    Enabled = true,
                                    Plugin = plugin,
                                    IsUpdatable = false
                                };

                                // Plugin MAY also implement IUpdatable interface.
                                if (typeof (IUpdatable).IsAssignableFrom(exportedType))
                                {
                                    Log.Info("The plugin is going to be updated every frame.");
                                    pluginInfo.IsUpdatable = true;
                                }

                                if (PluginContainer.GetPluginByName(pluginInfo.Name) == null)
                                {
                                    PluginContainer.Add(pluginInfo);

                                    Log.Info("Succesfully loaded a new plugin:\n" +
                                             $"   Name: {pluginInfo.Name}\n" +
                                             $"   Author: {pluginInfo.Plugin.Author}\n" +
                                             $"   APILevel: {pluginInfo.Plugin.CompatibleAPILevel}\n" +
                                             $"   Contact: {pluginInfo.Plugin.Contact}\n" +
                                             $"   Is updatable: {pluginInfo.IsUpdatable}");
                                }
                                else
                                {
                                    Log.Error("Did NOT load the plugin: '{path}'. A plugin with this name has already been loaded once.");
                                }
                                continue;
                            }
                            Log.Error($"'{path}' is not a valid plugin. Does not implement common IPlugin interface.");
                        }
                        catch (Exception e)
                        {
                            Log.Exception(e);
                        }
                    }
                    else
                    {
                        Log.Error($"'{path}' is not a valid plugin. No valid entry point detected.");
                    }
                }
            }
        }
    }
}
