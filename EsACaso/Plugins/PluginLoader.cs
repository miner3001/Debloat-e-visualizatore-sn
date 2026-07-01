using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace EsACaso
{
    /// <summary>
    /// Discovers and loads plugins from the "Plugins" subdirectory.
    /// </summary>
    public static class PluginLoader
    {
        private static readonly List<IEsacasoPlugin> _loadedPlugins = new List<IEsacasoPlugin>();
        public static IReadOnlyList<IEsacasoPlugin> LoadedPlugins => _loadedPlugins;

        /// <summary>
        /// Scans the Plugins folder for .dll files, loads them, and instantiates types implementing IEsacasoPlugin.
        /// </summary>
        /// <param name="mainForm">The main form to pass to each plugin's Initialize method.</param>
        public void LoadPlugins(Form mainForm)
        {
            string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
                Logger.Info($"Created plugins directory: {pluginsPath}");
                return;
            }

            var dllFiles = Directory.GetFiles(pluginsPath, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dll in dllFiles)
            {
                try
                {
                    Assembly asm = Assembly.LoadFrom(dll);
                    var types = asm.GetTypes()
                        .Where(t => typeof(IEsacasoPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        try
                        {
                            var plugin = (IEsacasoPlugin)Activator.CreateInstance(type);
                            plugin.Initialize(mainForm);
                            _loadedPlugins.Add(plugin);
                            Logger.Info($"Loaded plugin: {plugin.Name} v{plugin.Version} from {dll}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to create instance of type {type.FullName} from {dll}: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load assembly {dll}: {ex}");
                }
            }
        }

        /// <summary>
        /// Unloads all loaded plugins (note: actual unloading of assemblies requires separate AppDomain; here we just clear list).
        /// </summary>
        public void UnloadPlugins()
        {
            _loadedPlugins.Clear();
            Logger.Info("All plugins unloaded.");
        }
    }
}