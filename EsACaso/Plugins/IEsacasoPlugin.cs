using System.Windows.Forms;

namespace EsACaso
{
    /// <summary>
    /// Interface that plugins must implement to integrate with EsACaso.
    /// The host will call Initialize(mainForm) after loading the plugin assembly.
    /// </summary>
    public interface IEsacasoPlugin
    {
        /// <summary>
        /// Unique name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version of the plugin.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Called by the host to initialize the plugin.
        /// The plugin can add UI elements, menu items, or start background services.
        /// </summary>
        /// <param name="mainForm">The main form of the application.</param>
        void Initialize(Form mainForm);
    }
}