/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Joe Mistachkin (joe@mistachkin.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace SQLite.Designer
{
    /// <summary>
    /// This class keeps track of the options configured on a per-solution file
    /// basis pertaining to the System.Data.SQLite design-time components.
    /// </summary>
    [Guid("5cf5656c-ccbe-4162-8780-0cbee936b90c")]
    internal static class SQLiteOptions
    {
        #region Private Constants
        /// <summary>
        /// This is the name of the setting containing the configured ADO.NET
        /// provider name.
        /// </summary>
        private static readonly string ProviderNameKey = "ProviderName";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the name of the environment variable that will be checked
        /// prior to setting the initial default value for the configured
        /// ADO.NET provider name, thus allowing the default value to be
        /// overridden via the environment.
        /// </summary>
        private static readonly string ProviderNameEnvVarName =
            "ProviderName_SQLiteDesigner";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the legacy provider name used by the System.Data.SQLite
        /// design-time components.  It is also the default value for the
        /// associated option key.
        /// </summary>
        private static readonly string LegacyProviderName = "System.Data.SQLite";

        ///////////////////////////////////////////////////////////////////////

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
        /// <summary>
        /// This is the provider name used when Entity Framework 6.x support is
        /// required for use with the System.Data.SQLite design-time components.
        /// This provider name is only available when this class is compiled for
        /// the .NET Framework 4.0 or later.
        /// </summary>
        private static readonly string Ef6ProviderName = "System.Data.SQLite.EF6";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// This is used to synchronize access to the static dictionary of
        /// options and <see cref="DbProviderFactory" /> objects.
        /// </summary>
        private static readonly object syncRoot = new object();

        /// <summary>
        /// This dictionary contains the key/value pairs representing the
        /// per-solution options configured for the current solution.  When
        /// a new solution is loaded by Visual Studio, this dictionary must
        /// be reset.
        /// </summary>
        private static Dictionary<string, string> options;

        /// <summary>
        /// This dictionary contains the <see cref="DbProviderFactory" />
        /// objects cached by this class.
        /// </summary>
        private static Dictionary<string, DbProviderFactory>
            dbProviderFactories;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method initializes (or resets) the per-solution configuration
        /// options.
        /// </summary>
        /// <param name="reset">
        /// Non-zero to reset the options if they are already initialized.
        /// When this method is called from the <see cref="SQLitePackage" />
        /// constructor, this value should always be true.
        /// </param>
        private static void Initialize(
            bool reset
            )
        {
            lock (syncRoot)
            {
                if (options != null)
                    options.Clear();
                else
                    options = new Dictionary<string, string>();

                string key = ProviderNameKey;
                string value = Environment.GetEnvironmentVariable(
                    ProviderNameEnvVarName);

                if (IsValidValue(key, value))
                    options[key] = value;
#if !NET_40 && !NET_45 && !NET_451 && !NET_452 && !NET_46 && !NET_461 && !NET_462 && !NET_47 && !NET_471
                else
                    options[key] = LegacyProviderName;
#endif
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        #region Provider Name Handling
        /// <summary>
        /// This method determines the name of the ADO.NET provider for the
        /// System.Data.SQLite design-time components to use.
        /// </summary>
        /// <returns>
        /// The configured ADO.NET provider name for System.Data.SQLite -OR-
        /// the default ADO.NET provider name for System.Data.SQLite in the
        /// event of any failure.  This method cannot return null.
        /// </returns>
        public static string GetProviderName()
        {
#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            return GetProviderName(Ef6ProviderName);
#else
            return GetProviderName(LegacyProviderName);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the name of the ADO.NET provider for the
        /// System.Data.SQLite design-time components to use.
        /// </summary>
        /// <param name="default">
        /// The value to return from this method if the name of the ADO.NET
        /// provider is unavailable or cannot be determined.
        /// </param>
        /// <returns>
        /// The configured ADO.NET provider name for System.Data.SQLite -OR-
        /// the default ADO.NET provider name for System.Data.SQLite in the
        /// event of any failure.
        /// </returns>
        private static string GetProviderName(
            string @default
            )
        {
            string key = ProviderNameKey;
            string value;

            if (GetValue(key, out value) && IsValidValue(key, value))
                return value;

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the name of the ADO.NET provider for
        /// the System.Data.SQLite design-time components to use.
        /// </summary>
        /// <param name="value">
        /// The ADO.NET provider name to use.
        /// </param>
        /// <returns>
        /// Non-zero upon success; otherwise, zero.  All ADO.NET provider names
        /// unknown to this class are rejected.
        /// </returns>
        public static bool SetProviderName(
            string value
            )
        {
            string key = ProviderNameKey;

            if (IsValidValue(key, value))
                return SetValue(key, value);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region User-Interface Handling
        /// <summary>
        /// This method attempts to select the configured ADO.NET provider name
        /// in the specified <see cref="ComboBox" />.  This method will only
        /// work correctly when called from the user-interface thread.
        /// </summary>
        /// <param name="comboBox">
        /// The <see cref="ComboBox" /> object where the selection is to be
        /// modified.
        /// </param>
        /// <returns>
        /// Non-zero upon success; otherwise, zero.
        /// </returns>
        public static bool SelectProviderName(
            ComboBox comboBox
            )
        {
            if (comboBox == null)
                return false;

            string value = GetProviderName(null);

            for (int index = 0; index < comboBox.Items.Count; index++)
            {
                object item = comboBox.Items[index];

                if (item == null)
                    continue;

                if ((value == null) || String.Equals(
                        item.ToString(), value, StringComparison.Ordinal))
                {
                    comboBox.SelectedIndex = index;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines if the specified ADO.NET provider name appears to be
        /// usable.
        /// </summary>
        /// <param name="name">
        /// The invariant name of the ADO.NET provider to create.  Using null
        /// or an empty string will cause this method to return false.
        /// </param>
        /// <returns>
        /// Non-zero if the <see cref="DbProviderFactory" /> was created or
        /// queried successfully; otherwise, zero.
        /// </returns>
        private static bool CheckProviderName(
            string name
            )
        {
            DbProviderFactory dbProviderFactory; /* NOT USED */

            return GetProviderFactory(name, true, out dbProviderFactory);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the cached <see cref="DbProviderFactory" /> object associated
        /// with the specified name, creating a new instance if necessary.
        /// </summary>
        /// <param name="name">
        /// The invariant name of the ADO.NET provider to create.  Using null
        /// or an empty string will cause this method to return false.
        /// </param>
        /// <param name="create">
        /// Non-zero to create a new instance of the ADO.NET provider, if
        /// necessary.
        /// </param>
        /// <param name="dbProviderFactory">
        /// The newly created <see cref="DbProviderFactory" /> object or null
        /// if it is unavailable or could not be created.
        /// </param>
        /// <returns>
        /// Non-zero if the <see cref="DbProviderFactory" /> was created or
        /// queried successfully; otherwise, zero.
        /// </returns>
        public static bool GetProviderFactory(
            string name,
            bool create,
            out DbProviderFactory dbProviderFactory
            )
        {
            lock (syncRoot)
            {
                dbProviderFactory = null;

                if (dbProviderFactories == null)
                {
                    dbProviderFactories =
                        new Dictionary<string, DbProviderFactory>();
                }

                if (String.IsNullOrEmpty(name))
                    return false;

                if (dbProviderFactories.TryGetValue(
                        name, out dbProviderFactory))
                {
                    return (dbProviderFactory != null);
                }

                if (!create)
                    return false;

                dbProviderFactory = null; /* NOTE: Pedantic. */

                try
                {
                    dbProviderFactory = DbProviderFactories.GetFactory(name);
                }
                catch
                {
                    // do nothing.
                }

                dbProviderFactories.Add(name, dbProviderFactory);
                return (dbProviderFactory != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified <see cref="ComboBox" /> item
        /// list with the recognized ADO.NET provider names.  This method will
        /// only work correctly when called from the user-interface thread.
        /// </summary>
        /// <param name="items">
        /// The <see cref="ComboBox.Items" /> property value containing the
        /// list of items to be modified.  This value cannot be null.
        /// </param>
        /// <returns>
        /// The number of items actually added to the list, which may be zero.
        /// </returns>
        public static int AddProviderNames(
            ComboBox.ObjectCollection items
            )
        {
            int result = 0;

            if (items == null)
                return result;

            IList<string> names = new List<string>();

#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
            names.Add(Ef6ProviderName);
#endif

            names.Add(LegacyProviderName);

            foreach (string name in names)
            {
                if (CheckProviderName(name))
                {
                    items.Add(name);
                    result++;
                }
            }

            return result;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hard-Coded Default Value Handling
        /// <summary>
        /// This method determines if the specified key/value pair represents
        /// the default value for that option.
        /// </summary>
        /// <param name="key">
        /// The name ("key") of the configuration option.
        /// </param>
        /// <param name="value">
        /// The value of the configuration option.
        /// </param>
        /// <returns>
        /// Non-zero if the key/value pair represents its default value.
        /// </returns>
        public static bool IsDefaultValue(
            string key,
            string value
            )
        {
            if (String.Equals(
                    key, ProviderNameKey, StringComparison.Ordinal) &&
                String.Equals(
                    value, LegacyProviderName, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines if the specified key/value pair is valid
        /// and supported by this class.
        /// </summary>
        /// <param name="key">
        /// The name ("key") of the configuration option.
        /// </param>
        /// <param name="value">
        /// The value of the configuration option.
        /// </param>
        /// <returns>
        /// Non-zero if the key/value pair represents a valid option key and
        /// value supported by this class.
        /// </returns>
        public static bool IsValidValue(
            string key,
            string value
            )
        {
            if (String.Equals(
                    key, ProviderNameKey, StringComparison.Ordinal) &&
                (String.Equals(
                    value, LegacyProviderName, StringComparison.Ordinal)
#if NET_40 || NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471
                || String.Equals(
                    value, Ef6ProviderName, StringComparison.Ordinal)
#endif
                ))
            {
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Core Option Handling
        /// <summary>
        /// This method returns the current list of option keys supported by
        /// the System.Data.SQLite design-time components.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable{T}" /> of strings containing the list of
        /// option keys supported by the System.Data.SQLite design-time
        /// components -OR- null in the event of any failure.
        /// </returns>
        public static IEnumerable<string> GetKeys(
            bool reset
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                Initialize(reset);

                return (options != null) ?
                    new List<string>(options.Keys) : null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines if the specified option key is supported by
        /// this class.
        /// </summary>
        /// <param name="key">
        /// The name ("key") of the configuration option.
        /// </param>
        /// <returns>
        /// Non-zero if the specified option key is supported by this class.
        /// </returns>
        public static bool HaveKey(
            string key
            )
        {
            lock (syncRoot)
            {
                if ((key == null) || (options == null))
                    return false;

                return options.ContainsKey(key);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to query and return the current value of the
        /// specified option key.
        /// </summary>
        /// <param name="key">
        /// The name ("key") of the configuration option.
        /// </param>
        /// <param name="value">
        /// Upon success, the current value for the configuration option;
        /// otherwise, null.
        /// </param>
        /// <returns>
        /// Non-zero for success; otherwise, zero.
        /// </returns>
        public static bool GetValue(
            string key,
            out string value
            )
        {
            lock (syncRoot)
            {
                value = null;

                if ((key == null) || (options == null))
                    return false;

                if (options.TryGetValue(key, out value))
                    return true;

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the value of the specified option key.
        /// </summary>
        /// <param name="key">
        /// The name ("key") of the configuration option.
        /// </param>
        /// <param name="value">
        /// The new value for the configuration option.
        /// </param>
        /// <returns>
        /// Non-zero for success; otherwise, zero.
        /// </returns>
        public static bool SetValue(
            string key,
            string value
            )
        {
            lock (syncRoot)
            {
                if ((key == null) || (options == null))
                    return false;

                options[key] = value;
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stream Handling
        /// <summary>
        /// This method attempts to read an option value from the specified
        /// stream.  The stream must be readable.  After this method returns,
        /// the stream may no longer be usable.
        /// </summary>
        /// <param name="stream">
        /// The stream to read the option value from.
        /// </param>
        /// <param name="value">
        /// Upon success, the read value for the configuration option;
        /// otherwise, null.
        /// </param>
        /// <returns>
        /// Non-zero for success; otherwise, zero.
        /// </returns>
        public static bool ReadValue(
            Stream stream,
            out string value
            )
        {
            value = null;

            if ((stream == null) || !stream.CanRead)
                return false;

            try
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    value = streamReader.ReadToEnd();
                    return true;
                }
            }
            catch (Exception)
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to write an option value to the specified
        /// stream.  The stream must be writable.  After this method returns,
        /// the stream may no longer be usable.
        /// </summary>
        /// <param name="stream">
        /// The stream to write the option value to.
        /// </param>
        /// <param name="value">
        /// The option value to be written.  This value may be null.
        /// </param>
        /// <returns>
        /// Non-zero for success; otherwise, zero.
        /// </returns>
        public static bool WriteValue(
            Stream stream,
            string value
            )
        {
            if ((stream == null) || !stream.CanWrite)
                return false;

            try
            {
                using (StreamWriter streamWriter = new StreamWriter(stream))
                {
                    streamWriter.Write(value);
                    return true;
                }
            }
            catch (Exception)
            {
                // do nothing.
            }

            return false;
        }
        #endregion
        #endregion
    }
}
