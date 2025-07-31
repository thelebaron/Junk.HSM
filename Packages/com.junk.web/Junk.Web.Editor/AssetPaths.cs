using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Junk.Web.Editor
{
    /// <summary>
    /// Static utility class for loading stylesheets used in the graph editor.
    /// Provides centralized stylesheet management and easy extensibility for additional stylesheets.
    /// </summary>
    internal static class AssetPaths
    {
        /// <summary>
        /// The folder path where editor stylesheets are stored.
        /// </summary>
        private const string StylesheetFolderPath = "Packages/com.junk.web/Junk.Web.Editor/";
        
        /// <summary>
        /// The filename of the main graph editor stylesheet.
        /// </summary>
        private const string GraphEditorStylesheetName = "GraphEditor.uss";
        
        /// <summary>
        /// Loads the main GraphEditor stylesheet using AssetDatabase.
        /// </summary>
        /// <returns>The loaded StyleSheet, or null if the file could not be found or loaded.</returns>
        public static StyleSheet LoadGraphEditorStylesheet()
        {
            return LoadStylesheet(GraphEditorStylesheetName);
        }
        
        /// <summary>
        /// Loads a stylesheet by filename from the stylesheet folder.
        /// </summary>
        /// <param name="filename">The filename of the stylesheet to load (including .uss extension).</param>
        /// <returns>The loaded StyleSheet, or null if the file could not be found or loaded.</returns>
        public static StyleSheet LoadStylesheet(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                Debug.LogWarning("EditorStylesheetLoader: Filename cannot be null or empty.");
                return null;
            }
            
            string fullPath = StylesheetFolderPath + filename;
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(fullPath);
            
            if (styleSheet == null)
            {
                Debug.LogWarning($"EditorStylesheetLoader: Could not load stylesheet at path: {fullPath}");
            }
            
            return styleSheet;
        }
        
        /// <summary>
        /// Gets the full path to a stylesheet file in the stylesheet folder.
        /// </summary>
        /// <param name="filename">The filename of the stylesheet (including .uss extension).</param>
        /// <returns>The full asset path to the stylesheet.</returns>
        public static string GetStylesheetPath(string filename)
        {
            return StylesheetFolderPath + filename;
        }
        
        /// <summary>
        /// Checks if a stylesheet exists at the specified filename.
        /// </summary>
        /// <param name="filename">The filename of the stylesheet to check (including .uss extension).</param>
        /// <returns>True if the stylesheet exists, false otherwise.</returns>
        public static bool StylesheetExists(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;
                
            string fullPath = StylesheetFolderPath + filename;
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(fullPath) != null;
        }
    }
}
