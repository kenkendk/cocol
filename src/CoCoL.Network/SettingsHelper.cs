using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;

namespace CoCoL.Network
{
	/// <summary>
	/// An attribute for setting commandline options
	/// </summary>
	public class CommandlineOptionAttribute : Attribute
	{
		/// <summary>
		/// The long name of this option, user supplies this with double hyphen (--)
		/// </summary>
		public string LongName { get; set; }
		/// <summary>
		/// The short name of this option, user supplies this with single hyphen (-)
		/// </summary>
		public string ShortName { get; set; }
		/// <summary>
		/// The name of a custom function to parse the input
		/// </summary>
		public string CustomParserName { get; set; }
		/// <summary>
		/// A custom function to parse the input
		/// </summary>
		public Func<string, object> GetCustomParser<T>(T target, Type datatype)
		{ 
			if (string.IsNullOrWhiteSpace(CustomParserName))
				return null;

			var m = typeof(T).GetMethod(
				CustomParserName, 
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase, 
				null,
				new [] { typeof(string) },
				null);

			if (m == null)
				throw new System.IO.InvalidDataException(string.Format("No such method found: {0}", CustomParserName));
			if (m.ReturnType != datatype)
				throw new System.IO.InvalidDataException(string.Format("Parser method {0} should return type {1} but returns type {2}", CustomParserName, datatype, m.ReturnType));

			return x => m.Invoke(target, new object[] { x });
		}
		/// <summary>
		/// Gets or sets the description, or helptext, presented to the user
		/// </summary>
		public string Description { get; set; }
		/// <summary>
		/// Gets the displayed default value, or null for using the field value
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.CommandlineOptionAttribute"/> class.
		/// </summary>
		/// <param name="shortname">The short name of this option, user supplies this with single hyphen (-).</param>
		/// <param name="longname">The long name of this option, user supplies this with double hyphen (--).</param>
		/// <param name="customparser">A custom function to parse the input.</param>
		/// <param name="description">Gets or sets the description, or helptext, presented to the user.</param>
		/// <param name="defaultvalue">Gets the displayed default value, or null for using the field value.</param>
		public CommandlineOptionAttribute(string description = null, string longname = null, string shortname = null, string defaultvalue = null, string customparser = null) 
		{
			ShortName = shortname;
			LongName = longname;
			Description = description;
			DefaultValue = defaultvalue;
			CustomParserName = customparser;
		}
	}

	/// <summary>
	/// Helper class used to read settings from the commandline
	/// </summary>
	public class SettingsHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.SettingsHelper"/> class.
		/// </summary>
		public SettingsHelper()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.SettingsHelper"/> class.
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		public SettingsHelper(List<string> args)
		{
			Parse(args, this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.SettingsHelper"/> class.
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		/// <param name="report">The callback method for reporting errors.</param>
		public SettingsHelper(List<string> args, Action<string> report)
		{
			Parse(args, this, report);
		}

		/// <summary>
		/// Parses the commandline args
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		/// <param name="target">The instance being updated with values</param>
		/// <param name="throwonerror">Indicates if an exception is thrown on a parsing error</param>
		/// <returns>The list of commandline arguments not parsed</returns>
		/// <typeparam name="T">The data type of the item to parse.</typeparam>
		public static bool Parse<T>(List<string> args, T target, bool throwonerror = true)
            where T : class
        {
            return Parse(args, target, Console.WriteLine);
		}

		/// <summary>
		/// Uses reflection to build a list of supported options
		/// </summary>
		/// <returns>The config dict.</returns>
		/// <param name="target">The instance target.</param>
		/// <typeparam name="T">The type of the target.</typeparam>
		private static Dictionary<string, FieldInfo> GetConfig<T>(T target)
            where T : class
        {
            var res = new Dictionary<string, FieldInfo>();
			var type = target == null ? typeof(T) : target.GetType();
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | (target == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.IgnoreCase))
			{
				var attr = field.GetCustomAttribute(typeof(CommandlineOptionAttribute), true) as CommandlineOptionAttribute;
				if (attr == null)
				{
					res["--" + field.Name.ToLowerInvariant()] = field;
				}
				else
				{
					res["--" + ((string.IsNullOrWhiteSpace(attr.LongName) ? field.Name : attr.LongName)).ToLowerInvariant()] = field;
					if (!string.IsNullOrWhiteSpace(attr.ShortName))
						res["-" + attr.ShortName] = field;
				}
			}

			return res;
		}

		/// <summary>
		/// Returns the config object as a human readable string
		/// </summary>
		/// <returns>The string.</returns>
		public static string AsString<T>(T self)
            where T : class
        {
            return string.Join(", ", (self == null ? typeof(T) : self.GetType()).GetFields(BindingFlags.Public | BindingFlags.NonPublic | (self == null ? BindingFlags.Static : BindingFlags.Instance)).Select(x => string.Format("{0}={1}", x.Name, x.GetValue(null))));
		}

        /// <summary>
        /// Parses the commandline args
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <param name="target">The instance being updated with values</param>
        /// <param name="report">A callback method for reporting errors</param>
        /// <param name="throwonerror">Indicates if an exception is thrown on a parsing error</param>
        /// <returns>The list of commandline arguments not parsed</returns>
        /// <typeparam name="T">The data type of the item to parse.</typeparam>
        public static bool Parse<T>(List<string> args, T target, Action<string> report, bool throwonerror = true)
            where T : class
		{
			if (args == null)
				return true;

			var reportmethod = report ?? (x => { });
			var type = target == null ? typeof(T) : target.GetType();
			var helpmarkers = new [] { "help", "/h", "/?", "/help", "-h", "-help", "-?", "--help" };

			if (args.Any(x => helpmarkers.Any(y => string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase))))
			{
				reportmethod("Help: ");

				foreach (var field in type.GetFields(BindingFlags.Public | (target == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.IgnoreCase))
				{
					var attr = field.GetCustomAttribute(typeof(CommandlineOptionAttribute), true) as CommandlineOptionAttribute;
					var optname = (attr == null || string.IsNullOrWhiteSpace(attr.LongName)) ? field.Name : attr.LongName;
					if (attr != null && !string.IsNullOrWhiteSpace(attr.ShortName))
						reportmethod(string.Format("   -{0}", attr.ShortName));

					reportmethod(string.Format("  --{0}={1}", optname, field.GetValue(target)));

					if (attr != null && !string.IsNullOrWhiteSpace(attr.Description))
						reportmethod(string.Format("    {0}", attr.Description));
					
					reportmethod("");
				}
				return false;
			}
				
			var dict = GetConfig(target);
            var re = new Regex("((?<key>--[^=]+)|(?<key>-[^=]+))((=\\\"(?<value>[^\"]*)\\\")|=(?<value>.*))?", RegexOptions.IgnoreCase);
			for(var i = 0; i < args.Count; i++)
			{
				var n = args[i];
                if (n == null)
                    continue;

				if (!n.StartsWith("-", StringComparison.Ordinal))
					continue;
				
				var m = re.Match(n);
				if (!m.Success || m.Length != n.Length)
					reportmethod(string.Format("Unmatched option: {0}", n));
				else
				{
					args.RemoveAt(i);
					i--;

					var key = m.Groups["key"].Value;
					var value = m.Groups["value"].Value;

					FieldInfo field;
					if (!dict.TryGetValue(key, out field) && n.StartsWith("--", StringComparison.Ordinal))
						dict.TryGetValue(key.ToLowerInvariant(), out field);

					if (field == null)
						reportmethod(string.Format("No such option: {0}", key));
					else
					{
						var attr = field.GetCustomAttribute(typeof(CommandlineOptionAttribute), true) as CommandlineOptionAttribute;
						if (attr != null && attr.GetCustomParser(target, field.FieldType) != null)
							field.SetValue(target, attr.GetCustomParser(target, field.FieldType)(value));
						else if (field.FieldType == typeof(int))
							field.SetValue(target, int.Parse(value));
						else if (field.FieldType == typeof(long))
							field.SetValue(target, long.Parse(value));
						else if (field.FieldType == typeof(bool))
							field.SetValue(target, bool.Parse(string.IsNullOrWhiteSpace(value) ? "true" : value));
						else if (field.FieldType == typeof(string))
							field.SetValue(target, value);
						else if (field.FieldType.IsEnum)
							field.SetValue(target, Enum.Parse(field.FieldType, value, true));
						else
							reportmethod(string.Format("Not a valid field type: {0} for option: {1}", field.FieldType.FullName, key));
					}
				}
			}

			return true;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="CoCoL.Network.SettingsHelper"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="CoCoL.Network.SettingsHelper"/>.</returns>
		public override string ToString()
		{
			return AsString(this);
		}
	}
}

