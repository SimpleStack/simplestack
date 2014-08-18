using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SimpleStack.Config;
using SimpleStack.Extensions;
using SimpleStack.Interfaces;
using SimpleStack.Serializers;

namespace SimpleStack
{
	public class RestPath : IRestPath
	{
		private const string IgnoreParam = "ignore";
		private const string WildCard = "*";
		private const char WildCardChar = '*';
		private const string PathSeperator = "/";
		private const char PathSeperatorChar = '/';
		private const char ComponentSeperator = '.';
		private const string VariablePrefix = "{";

		private readonly string allowedVerbs;
		private readonly bool allowsAllVerbs;
		private readonly bool[] componentsWithSeparators = new bool[0];
		private readonly bool isWildCardPath;

		private readonly string[] literalsToMatch = new string[0];
		private readonly Dictionary<string, string> propertyNamesMap = new Dictionary<string, string>();
		private readonly string restPath;
		private readonly StringMapTypeDeserializer typeDeserializer;

		private readonly int variableArgsCount;
		private readonly string[] variablesNames = new string[0];
		public string[] Verbs = new string[0];

		public RestPath(Type requestType, string path) : this(requestType, path, null)
		{
		}

		public RestPath(Type requestType, string path, string verbs, string summary = null, string notes = null)
		{
			RequestType = requestType;
			Summary = summary;
			Notes = notes;
			restPath = path;

			allowsAllVerbs = verbs == null || verbs == WildCard;
			if (!allowsAllVerbs)
			{
				allowedVerbs = verbs.ToUpper();
			}

			var componentsList = new List<string>();

			//We only split on '.' if the restPath has them. Allows for /{action}.{type}
			var hasSeparators = new List<bool>();
			foreach (string component in restPath.Split(PathSeperatorChar))
			{
				if (string.IsNullOrEmpty(component)) continue;

				if (component.Contains(VariablePrefix)
				    && component.Contains(ComponentSeperator))
				{
					hasSeparators.Add(true);
					componentsList.AddRange(component.Split(ComponentSeperator));
				}
				else
				{
					hasSeparators.Add(false);
					componentsList.Add(component);
				}
			}

			string[] components = componentsList.ToArray();
			TotalComponentsCount = components.Length;

			literalsToMatch = new string[TotalComponentsCount];
			variablesNames = new string[TotalComponentsCount];
			componentsWithSeparators = hasSeparators.ToArray();
			PathComponentsCount = componentsWithSeparators.Length;
			string firstLiteralMatch = null;
			int lastVariableMatchPos = -1;

			var sbHashKey = new StringBuilder();
			for (int i = 0; i < components.Length; i++)
			{
				string component = components[i];

				if (component.StartsWith(VariablePrefix))
				{
					variablesNames[i] = component.Substring(1, component.Length - 2);
					variableArgsCount++;
					lastVariableMatchPos = i;
				}
				else
				{
					literalsToMatch[i] = component.ToLower();
					sbHashKey.Append(i + PathSeperatorChar.ToString() + literalsToMatch);

					if (firstLiteralMatch == null)
					{
						firstLiteralMatch = literalsToMatch[i];
					}
				}
			}

			if (lastVariableMatchPos != -1)
			{
				string lastVariableMatch = variablesNames[lastVariableMatchPos];
				isWildCardPath = lastVariableMatch[lastVariableMatch.Length - 1] == WildCardChar;
				if (isWildCardPath)
				{
					variablesNames[lastVariableMatchPos] = lastVariableMatch.Substring(0, lastVariableMatch.Length - 1);
				}
			}

			FirstMatchHashKey = !isWildCardPath
				                    ? PathComponentsCount + PathSeperator + firstLiteralMatch
				                    : WildCardChar + PathSeperator + firstLiteralMatch;

			IsValid = sbHashKey.Length > 0;
			UniqueMatchHashKey = sbHashKey.ToString();

			typeDeserializer = new StringMapTypeDeserializer(RequestType);
			RegisterCaseInsenstivePropertyNameMappings();
		}

		/// <summary>
		///     The number of segments separated by '/' determinable by path.Split('/').Length
		///     e.g. /path/to/here.ext == 3
		/// </summary>
		public int PathComponentsCount { get; set; }

		/// <summary>
		///     The total number of segments after subparts have been exploded ('.')
		///     e.g. /path/to/here.ext == 4
		/// </summary>
		public int TotalComponentsCount { get; set; }

		public string Path
		{
			get { return restPath; }
		}

		public string Summary { get; private set; }

		public string Notes { get; private set; }

		public bool AllowsAllVerbs
		{
			get { return allowsAllVerbs; }
		}

		public string AllowedVerbs
		{
			get { return allowedVerbs; }
		}

		public bool IsValid { get; set; }

		/// <summary>
		///     Provide for quick lookups based on hashes that can be determined from a request url
		/// </summary>
		public string FirstMatchHashKey { get; private set; }

		public string UniqueMatchHashKey { get; private set; }
		public Type RequestType { get; private set; }

		public object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance)
		{
			string[] requestComponents = pathInfo.Split(PathSeperatorChar)
			                                     .Where(x => !string.IsNullOrEmpty(x)).ToArray();

			ExplodeComponents(ref requestComponents);

			if (requestComponents.Length != TotalComponentsCount)
			{
				bool isValidWildCardPath = isWildCardPath
				                           && requestComponents.Length >= TotalComponentsCount - 1;

				if (!isValidWildCardPath)
					throw new ArgumentException(string.Format(
						"Path Mismatch: Request Path '{0}' has invalid number of components compared to: '{1}'",
						pathInfo, restPath));
			}

			var requestKeyValuesMap = new Dictionary<string, string>();
			for (int i = 0; i < TotalComponentsCount; i++)
			{
				string variableName = variablesNames[i];
				if (variableName == null) continue;

				string propertyNameOnRequest;
				if (!propertyNamesMap.TryGetValue(variableName.ToLower(), out propertyNameOnRequest))
				{
					if (IgnoreParam.EqualsIgnoreCase(variableName))
						continue;

					throw new ArgumentException("Could not find property "
					                            + variableName + " on " + RequestType.Name);
				}

				string value = requestComponents.Length > 1 ? requestComponents[i] : null; //wildcard has arg mismatch
				if (value != null && i == TotalComponentsCount - 1)
				{
					var sb = new StringBuilder(value);
					for (int j = i + 1; j < requestComponents.Length; j++)
					{
						sb.Append(PathSeperatorChar + requestComponents[j]);
					}
					value = sb.ToString();
				}

				requestKeyValuesMap[propertyNameOnRequest] = value;
			}

			if (queryStringAndFormData != null)
			{
				//Query String and form data can override variable path matches
				//path variables < query string < form data
				foreach (var name in queryStringAndFormData)
				{
					requestKeyValuesMap[name.Key] = name.Value;
				}
			}

			return typeDeserializer.PopulateFromMap(fromInstance, requestKeyValuesMap);
		}

		public static string[] GetPathPartsForMatching(string pathInfo)
		{
			string[] parts = pathInfo.ToLower().Split(PathSeperatorChar)
			                         .Where(x => !string.IsNullOrEmpty(x)).ToArray();
			return parts;
		}

		public static IEnumerable<string> GetFirstMatchHashKeys(string[] pathPartsForMatching)
		{
			string hashPrefix = pathPartsForMatching.Length + PathSeperator;
			return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
		}

		public static IEnumerable<string> GetFirstMatchWildCardHashKeys(string[] pathPartsForMatching)
		{
			const string hashPrefix = WildCard + PathSeperator;
			return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
		}

		private static IEnumerable<string> GetPotentialMatchesWithPrefix(string hashPrefix, IEnumerable<string> pathPartsForMatching)
		{
			foreach (string part in pathPartsForMatching)
			{
				yield return hashPrefix + part;
				string[] subParts = part.Split(ComponentSeperator);
				if (subParts.Length == 1) continue;

				foreach (string subPart in subParts)
				{
					yield return hashPrefix + subPart;
				}
			}
		}

		private void RegisterCaseInsenstivePropertyNameMappings()
		{
			string propertyName = "";
			try
			{
				foreach (PropertyInfo propertyInfo in RequestType.GetSerializableProperties())
				{
					propertyName = propertyInfo.Name;
					propertyNamesMap.Add(propertyName.ToLower(), propertyName);
				}
				if (SerializationConfig.IncludePublicFields)
				{
					foreach (FieldInfo fieldInfo in RequestType.GetSerializableFields())
					{
						propertyName = fieldInfo.Name;
						propertyNamesMap.Add(propertyName.ToLower(), propertyName);
					}
				}
			}
			catch (Exception)
			{
				throw new AmbiguousMatchException("Property names are case-insensitive: " + RequestType.Name + "." + propertyName);
			}
		}

		public int MatchScore(string httpMethod, string[] withPathInfoParts)
		{
			bool isMatch = IsMatch(httpMethod, withPathInfoParts);
			if (!isMatch) return -1;

			bool exactVerb = httpMethod == AllowedVerbs;
			int score = exactVerb ? 10 : 1;
			score += Math.Max((10 - variableArgsCount), 1)*100;

			return score;
		}

		/// <summary>
		///     For performance withPathInfoParts should already be a lower case string
		///     to minimize redundant matching operations.
		/// </summary>
		/// <param name="httpMethod"></param>
		/// <param name="withPathInfoParts"></param>
		/// <returns></returns>
		public bool IsMatch(string httpMethod, string[] withPathInfoParts)
		{
			if (withPathInfoParts.Length != PathComponentsCount && !isWildCardPath) return false;
			if (!allowsAllVerbs && !allowedVerbs.Contains(httpMethod)) return false;

			if (!ExplodeComponents(ref withPathInfoParts)) return false;
			if (TotalComponentsCount != withPathInfoParts.Length && !isWildCardPath) return false;

			for (int i = 0; i < TotalComponentsCount; i++)
			{
				string literalToMatch = literalsToMatch[i];
				if (literalToMatch == null) continue;

				if (withPathInfoParts[i] != literalToMatch) return false;
			}

			return true;
		}

		private bool ExplodeComponents(ref string[] withPathInfoParts)
		{
			var totalComponents = new List<string>();
			for (int i = 0; i < withPathInfoParts.Length; i++)
			{
				string component = withPathInfoParts[i];
				if (string.IsNullOrEmpty(component)) continue;

				if (PathComponentsCount != TotalComponentsCount
				    && componentsWithSeparators[i])
				{
					string[] subComponents = component.Split(ComponentSeperator);
					if (subComponents.Length < 2) return false;
					totalComponents.AddRange(subComponents);
				}
				else
				{
					totalComponents.Add(component);
				}
			}

			withPathInfoParts = totalComponents.ToArray();
			return true;
		}

		public object CreateRequest(string pathInfo)
		{
			return CreateRequest(pathInfo, null, null);
		}

		public override int GetHashCode()
		{
			return UniqueMatchHashKey.GetHashCode();
		}
	}
}