// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StyleCopLocator.cs" company="http://stylecop.codeplex.com">
//   MS-PL
// </copyright>
// <license>
//   This source code is subject to terms and conditions of the Microsoft 
//   Public License. A copy of the license can be found in the License.html 
//   file at the root of this distribution. If you cannot locate the  
//   Microsoft Public License, please send an email to dlr@microsoft.com. 
//   By using this source code in any fashion, you are agreeing to be bound 
//   by the terms of the Microsoft Public License. You must not remove this 
//   notice, or any other, from this software.
// </license>
// <summary>
//   The style cop locator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace StyleCop.ReSharper600.Core
{
    #region Using Directives

    using System.IO;
    using System.Reflection;

    using Microsoft.Win32;

    #endregion

    /// <summary>
    /// The style cop locator.
    /// </summary>
    public static class StyleCopLocator
    {
        #region Public Methods and Operators

        /// <summary>
        /// Gets the StyleCop assembly path.
        /// </summary>
        /// <returns>
        /// The path to the StyleCop assembly or null if not found.
        /// </returns>
        public static string GetStyleCopPath()
        {
            string directory = RetrieveFromRegistry() ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return directory == null ? directory : Path.Combine(directory, Constants.StyleCopAssemblyName);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the StyleCop install location from the registry. This registry key is created by StyleCop during install.
        /// </summary>
        /// <returns>
        /// Returns the registry key value or null if not found.
        /// </returns>
        private static string RetrieveFromRegistry()
        {
            const string SubKey = @"SOFTWARE\CodePlex\StyleCop";
            const string Key = "InstallDir";

            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(SubKey);
            return registryKey == null ? null : registryKey.GetValue(Key) as string;
        }

        #endregion
    }
}