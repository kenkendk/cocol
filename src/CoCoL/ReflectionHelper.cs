using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

#if LIMITED_REFLECTION_SUPPORT

// CoCoL-PCL implementation of missing System.Reflection items

#if PCL_BUILD
namespace System.Reflection
{
    [Flags]
    internal enum BindingFlags
	{
		Default = 0,
		IgnoreCase = 1,
		DeclaredOnly = 2,
		Instance = 4,
		Static = 8,
		Public = 16,
		NonPublic = 32,
		FlattenHierarchy = 64,
		InvokeMethod = 256,
		CreateInstance = 512,
		GetField = 1024,
		SetField = 2048,
		GetProperty = 4096,
		SetProperty = 8192,
		PutDispProperty = 16384,
		PutRefDispProperty = 32768,
		ExactBinding = 65536,
		SuppressChangeType = 131072,
		OptionalParamBinding = 262144,
		IgnoreReturn = 16777216
	}
}
#endif

namespace CoCoL
{

	/// <summary>
	/// Class that contains methods for dealing with shortcommings in the PCL reflection library
	/// </summary>
	internal static class ReflectionHelper
	{
		private static IEnumerable<Type> GetTypeChain(Type t, BindingFlags flags)
		{
			do
			{
				yield return t;
				if (flags.HasFlag(BindingFlags.DeclaredOnly) || !flags.HasFlag(BindingFlags.FlattenHierarchy))
					break;
				
				t = t.GetTypeInfo().BaseType;
			}
			while(t != null);
		}

		private static IEnumerable<FieldInfo> Filter(IEnumerable<FieldInfo> mx, BindingFlags flags)
		{
			if (flags.HasFlag(BindingFlags.Default) || (flags.HasFlag(BindingFlags.Public) && flags.HasFlag(BindingFlags.NonPublic)))
				return mx;

			if (flags.HasFlag(BindingFlags.Public))
				return mx.Where(x => x.IsPublic);
			else
				return mx.Where(x => x.IsPrivate);
		}
			
		public static IEnumerable<FieldInfo> GetFields(this Type t, BindingFlags flags)
		{
			return Filter(GetTypeChain(t, flags).SelectMany(x => x.GetTypeInfo().DeclaredFields), flags);
		}

		public static IEnumerable<PropertyInfo> GetProperties(this Type t, BindingFlags flags)
		{
			return GetTypeChain(t, flags).SelectMany(x => x.GetTypeInfo().DeclaredProperties);
		}

		public static bool IsAssignableFrom(this Type source, Type target)
		{
			return source.GetTypeInfo().IsAssignableFrom(target.GetTypeInfo());
		}

		public static Type[] GetTypes(this Assembly self)
		{
			return self.DefinedTypes.Select(x => x.DeclaringType).ToArray();
		}

		public static Type[] GetInterfaces(this Type self)
		{
			return self.GetTypeInfo().ImplementedInterfaces.ToArray();
		}

        public static MethodInfo GetMethod(this Type self, string name, params Type[] types)
        {
            return GetMethod(self, name, BindingFlags.Default, types);
        }

        public static MethodInfo GetMethod(this Type self, string name, BindingFlags bindingFlags, params Type[] types)
		{
			var typeslen = types == null ? 0 : types.Length;
			if (typeslen == 0)
				return self.GetTypeInfo().GetDeclaredMethod(name);
			else
			{
				var res = self.GetTypeInfo().GetDeclaredMethods(name).Where(x =>
					{
						var xp = x.GetParameters();
						var xplen = x.GetParameters() == null ? 0 : x.GetParameters().Length;
						if (xplen != typeslen)
							return false;

						for(var i = 0; i < typeslen; i++)
							if (xp[i].ParameterType != types[i])
								return false;

						return true;
					}).ToArray();

				// Grab exception or return value from core
				if (res.Length != 1)
					return self.GetTypeInfo().GetDeclaredMethod(name);
				else
					return res[0];
			}
		}

		public static bool IsClass(this Type self)
		{
			return self.GetTypeInfo().IsClass;
		}

		public static ConstructorInfo GetConstructor(this Type self, Type[] arguments)
		{
			return self.GetTypeInfo().DeclaredConstructors.Where(x =>
			{
				var p = x.GetParameters();
					if ((p == null ? 0 : p.Length) != (arguments == null ? 0 : arguments.Length))
						return false;

				if (p == null || arguments == null)
					return true;
					
				return p.Zip(arguments, (a,b) => a.ParameterType == b).All(y => y);
			})
			.FirstOrDefault();
		}

		public static Attribute[] GetCustomAttributes(this Type self, Type attrtype, bool inherit)
		{
			return self.GetTypeInfo().GetCustomAttributes(attrtype, inherit).ToArray();
		}
	}

}

#endif
