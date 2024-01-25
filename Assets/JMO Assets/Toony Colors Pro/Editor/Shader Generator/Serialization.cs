using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

// Reflection-based serialization system: serialize simple value types, and specific classes (either those with the SerializeAs attribute, or special ones like Vector2, Vector3, ...)
// Used to serialize data and add it as a comment in generated shaders

namespace ToonyColorsPro
{
    namespace ShaderGenerator
    {
        public class Serialization
        {
            //Will serialize an object as "type(field:value,field2:value,field3:value...)" provided that they have fields with the [SerializeAs] attribute
            public static string Serialize(object obj, FieldInfo objFieldInfo = null)
            {
                string output = "";

                //fetch class SerializedAs attribute
                object[] classAttributes = obj.GetType().GetCustomAttributes(typeof(SerializeAsAttribute), false);
                if (classAttributes != null && classAttributes.Length == 1)
                {
                    SerializeAsAttribute serializedAsAttribute = classAttributes[0] as SerializeAsAttribute;

                    //class has a conditional serialization?
                    string conditionalFieldName = serializedAsAttribute.conditionalField;
                    if (!string.IsNullOrEmpty(conditionalFieldName))
                    {
                        bool forceSerialization = objFieldInfo != null &&
                                                  ((Attribute[])objFieldInfo.GetCustomAttributes(
                                                      typeof(ForceSerializationAttribute))).Length == 1;
                        if (!forceSerialization)
                        {
                            //try field
                            FieldInfo conditionalField = obj.GetType().GetField(conditionalFieldName);
                            if (conditionalField != null)
                            {
                                if (!(bool)conditionalField.GetValue(obj)) return null;
                            }
                            else
                            {
                                //try property
                                PropertyInfo conditionalProperty = obj.GetType().GetProperty(conditionalFieldName);
                                if (conditionalProperty != null)
                                {
                                    if (!(bool)conditionalProperty.GetValue(obj, null)) return null;
                                }
                                else
                                {
                                    Debug.LogError(string.Format(
                                        "Conditional field or property '{0}' doesn't exist for type '{1}'",
                                        conditionalFieldName, obj.GetType()));
                                }
                            }
                        }
                    }

                    string name = serializedAsAttribute.serializedName;
                    output = name + "(";
                }

                // properties with [SerializeAs] attribute
                // note: only used for unityVersion currently; see Config.cs
                List<PropertyInfo> properties = new List<PropertyInfo>(obj.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                foreach (PropertyInfo prop in properties)
                {
                    object[] attributes = prop.GetCustomAttributes(typeof(SerializeAsAttribute), true);
                    if (attributes != null && attributes.Length == 1)
                    {
                        string name = (attributes[0] as SerializeAsAttribute).serializedName;
                        output += string.Format("{0}:\"{1}\";", name, prop.GetValue(obj, null));
                    }
                }

                //get all fields, and look for [SerializeAs] attribute
                List<FieldInfo> fields = new List<FieldInfo>(obj.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                foreach (FieldInfo field in fields)
                {
                    object[] attributes = field.GetCustomAttributes(typeof(SerializeAsAttribute), true);
                    if (attributes != null && attributes.Length == 1)
                    {
                        string name = (attributes[0] as SerializeAsAttribute).serializedName;

                        //returns the value of an object as a string
                        Func<object, string> GetStringValue = null;
                        GetStringValue = @object =>
                        {
                            if (@object == null)
                                // Debug.LogError("Serialization error!\nTrying to get the string value of a null object.");
                                return "__NULL__";

                            Type type = @object.GetType();

                            //object types
                            if (!type.IsValueType && type != typeof(string))
                            {
                                //list
                                if (@object is IList)
                                {
                                    IList list = @object as IList;
                                    string values = "list[";
                                    foreach (object value in list)
                                        values += GetStringValue(value) + ",";
                                    return values.TrimEnd(',') + "]";
                                }

                                //dictionary
                                if (@object is IDictionary)
                                {
                                    IDictionary dict = @object as IDictionary;
                                    string kvp = "dict[";
                                    foreach (DictionaryEntry entry in dict)
                                        kvp += entry.Key + "=" + GetStringValue(entry.Value) + ",";
                                    return kvp.TrimEnd(',') + "]";
                                }

                                //else try to serialize with this serializer
                                object[] refAttributes = field.GetCustomAttributes(typeof(SerializeAsAttribute), true);
                                if (refAttributes != null && refAttributes.Length == 1)
                                    //serializable
                                    return Serialize(@object, field);

                                return null;
                            }

                            //string: enclose in quotes to prevent parsing errors (e.g. with parenthesis)
                            if (type == typeof(string)) return string.Format("\"{0}\"", @object);

                            // unity vectors: prevent printing values with commas
                            if (type == typeof(Vector2))
                            {
                                Vector2 v2 = (Vector2)@object;
                                return string.Format(CultureInfo.InvariantCulture, "({0}, {1})", v2.x, v2.y);
                            }

                            if (type == typeof(Vector3))
                            {
                                Vector3 v3 = (Vector3)@object;
                                return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2})", v3.x, v3.y, v3.z);
                            }

                            if (type == typeof(Vector4))
                            {
                                Vector4 v4 = (Vector4)@object;
                                return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", v4.x, v4.y,
                                    v4.z, v4.w);
                            }

                            if (type == typeof(Color))
                            {
                                Color c = (Color)@object;
                                return string.Format(CultureInfo.InvariantCulture, "RGBA({0}, {1}, {2}, {3})", c.r, c.g,
                                    c.b, c.a);
                                // return string.Format(CultureInfo.InvariantCulture, "{0}", c);
                            }

                            //value type: just return the toString version
                            return string.Format(CultureInfo.InvariantCulture, "{0}", @object);
                        };

                        string val = GetStringValue(field.GetValue(obj));
                        if (val == null)
                            Debug.LogError(string.Format("Can't serialize this reference type: '{0}'\nFor field: '{1}'",
                                field.FieldType, field.Name));
                        else
                            output += string.Format("{0}:{1};", name, val);
                    }
                }

                output = output.TrimEnd(';');
                output += ")";

                return output;
            }

            //Deserialize without knowing type
            public static object Deserialize(string data, object[] args = null)
            {
                //extract serialized class name
                int index = data.IndexOf('(');
                string serializedClassName = data.Substring(0, index);

                //fetch all serialized classes names, and try to match it
                Type type = null;
                Type[] allTypes = typeof(Serialization).Assembly.GetTypes();
                foreach (Type t in allTypes)
                {
                    object[] classAttributes = t.GetCustomAttributes(typeof(SerializeAsAttribute), false);
                    if (classAttributes != null && classAttributes.Length == 1)
                    {
                        string name = (classAttributes[0] as SerializeAsAttribute).serializedName;
                        if (name == serializedClassName)
                            //match!
                            type = t;
                    }
                }

                if (type == null)
                {
                    Debug.LogError(ShaderGenerator2.ErrorMsg("Can't find proper Type for serialized class named '<b>" +
                                                             serializedClassName + "</b>'"));
                    return null;
                }

                //return new object with correct type
                return Deserialize(data, type, args);
            }

            //Deserialize to a new object (needs a new() constructor, and valid arguments as 'args', if any)
            public static object Deserialize(string data, Type type, object[] args = null)
            {
                MethodInfo[] methods =
                    type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                foreach (MethodInfo method in methods)
                {
                    object[] deserializeCallbacks =
                        method.GetCustomAttributes(typeof(CustomDeserializeCallbackAttribute), false);
                    if (deserializeCallbacks.Length > 0) return method.Invoke(null, new object[] { data, args });
                }

                object obj = Activator.CreateInstance(type, args);
                return DeserializeTo(obj, data, type, args);
            }

            //Deserialize a specific type
            //'specialClasses': hook so that the caller can implement its own deserialization logic (used for Shader Property list in Config)
            public static object DeserializeTo(object obj, string data, Type type, object[] args = null,
                Dictionary<Type, Func<object, string, object>> specialClasses = null)
            {
                //extract parts of the input data, format should be "type(field:value;field2:value)"
                int index = data.IndexOf('(');

                string serializedClassName = data.Substring(0, index);
                string fieldsData = data.Substring(index + 1);
                fieldsData = fieldsData.Substring(0, fieldsData.Length - 1); //remove trailing ')'

                //fetch class serialized name and check against specified T type
                object[] classAttributes = type.GetCustomAttributes(typeof(SerializeAsAttribute), false);
                if (classAttributes != null && classAttributes.Length == 1)
                {
                    string name = (classAttributes[0] as SerializeAsAttribute).serializedName;
                    if (name != serializedClassName)
                    {
                        Debug.LogError(string.Format(
                            "Class doesn't match serialized class name.\nExpected '{0}', got '{1}'.",
                            serializedClassName, name));
                        return null;
                    }
                }

                //fetch all [SerializeAs] fields from that type
                List<FieldInfo> fields =
                    new List<FieldInfo>(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic |
                                                       BindingFlags.Public));
                Dictionary<string, FieldInfo> serializedFields = new Dictionary<string, FieldInfo>();
                foreach (FieldInfo field in fields)
                {
                    object[] attributes = field.GetCustomAttributes(typeof(SerializeAsAttribute), true);
                    if (attributes != null && attributes.Length == 1)
                    {
                        string name = (attributes[0] as SerializeAsAttribute).serializedName;
                        serializedFields.Add(name, field);
                    }
                }

                //converts a serialized string into a value
                Func<string, Type, object> StringToValue = null;
                StringToValue = (strValue, t) =>
                {
                    //special classes: call the callback specified by caller
                    if (specialClasses != null && specialClasses.ContainsKey(t))
                        return specialClasses[t].Invoke(obj, strValue);

                    //object types
                    if (!t.IsValueType && t != typeof(string))
                    {
                        // handle null values
                        if (strValue == "__NULL__") return null;

                        //list
                        if (typeof(IList).IsAssignableFrom(t))
                        {
                            //parse list values: remove 'list[' and ']' characters, and split on ','
                            string[] serializedValues = SplitExcludingBlocks(strValue.Substring(5, strValue.Length - 6),
                                ',', true, "()", "[]");

                            //find what T is for this List<T>
                            Type itemType = t.GetGenericArguments()[0];

                            //create new list with parsed serialized values
                            Type genericListType = typeof(List<>).MakeGenericType(itemType);
                            IList list = (IList)Activator.CreateInstance(genericListType);
                            foreach (string item in serializedValues)
                            {
                                if (string.IsNullOrEmpty(item))
                                    continue;

                                object v = StringToValue(item, itemType);
                                if (v != null)
                                    list.Add(v);
                            }

                            //assign new list for obj
                            return list;
                        }

                        //dict
                        if (typeof(IDictionary).IsAssignableFrom(t))
                        {
                            //parse dict values: remove 'dict[' and ']' characters, and split on ','
                            string[] serializedValues = SplitExcludingBlocks(strValue.Substring(5, strValue.Length - 6),
                                ',', true, "()", "[]");

                            //find what kind of KeyValuePair types are used
                            Type keyType = t.GetGenericArguments()[0];
                            Type valueType = t.GetGenericArguments()[1];

                            //create new dictionary with parsed serialized values
                            Type genericDictType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                            IDictionary dict = (IDictionary)Activator.CreateInstance(genericDictType);
                            foreach (string item in serializedValues)
                            {
                                if (string.IsNullOrEmpty(item))
                                    continue;

                                //gey key & value from format "key=value"
                                string[] kv = item.Split('=');
                                string key = kv[0];
                                string value = kv[1];

                                object k = StringToValue(key, keyType);
                                object v = StringToValue(value, valueType);

                                if (k != null && v != null)
                                    dict.Add(k, v);
                            }

                            //assign new list for obj
                            return dict;
                        }

                        //else try to deserialize
                        {
                            object value = Deserialize(strValue, t, args);
                            return value;
                        }
                    }

                    //Unity value-type structs
                    if (t == typeof(Vector2))
                    {
                        string[] v2data = strValue.Substring(1, strValue.Length - 2).Split(',');
                        return new Vector2(float.Parse(v2data[0], CultureInfo.InvariantCulture),
                            float.Parse(v2data[1], CultureInfo.InvariantCulture));
                    }

                    if (t == typeof(Vector3))
                    {
                        string[] v3data = strValue.Substring(1, strValue.Length - 2).Split(',');
                        return new Vector3(float.Parse(v3data[0], CultureInfo.InvariantCulture),
                            float.Parse(v3data[1], CultureInfo.InvariantCulture),
                            float.Parse(v3data[2], CultureInfo.InvariantCulture));
                    }

                    if (t == typeof(Vector4))
                    {
                        string[] v4data = strValue.Substring(1, strValue.Length - 2).Split(',');
                        return new Vector4(float.Parse(v4data[0], CultureInfo.InvariantCulture),
                            float.Parse(v4data[1], CultureInfo.InvariantCulture),
                            float.Parse(v4data[2], CultureInfo.InvariantCulture),
                            float.Parse(v4data[3], CultureInfo.InvariantCulture));
                    }

                    if (t == typeof(Color))
                    {
                        string[] cData = strValue.Substring("RGBA(".Length, strValue.Length - "RGBA(".Length - 1)
                            .Split(',');
                        return new Color(float.Parse(cData[0], CultureInfo.InvariantCulture),
                            float.Parse(cData[1], CultureInfo.InvariantCulture),
                            float.Parse(cData[2], CultureInfo.InvariantCulture),
                            float.Parse(cData[3], CultureInfo.InvariantCulture));
                    }

                    //enums
                    if (typeof(Enum).IsAssignableFrom(t)) return Enum.Parse(t, strValue);

                    //string: remove quotes to extract value
                    if (t == typeof(string))
                    {
                        // handle null values
                        if (strValue == "__NULL__") return null;

                        return strValue.Trim('"');
                    }

                    //value type: automatic conversion
                    return Convert.ChangeType(strValue, t, CultureInfo.InvariantCulture);
                };

                //iterate through entries in the source string
                string[] entries = SplitExcludingBlocks(fieldsData, ';', true, "()", "[]");
                foreach (string entry in entries)
                {
                    string[] kvp = SplitExcludingBlocks(entry, ':', true, "()");
                    string name = kvp[0];
                    string strValue = kvp[1];

                    if (serializedFields.ContainsKey(name))
                    {
                        FieldInfo fieldInfo = serializedFields[name];
                        object v = StringToValue(strValue, fieldInfo.FieldType);
                        if (v != null)
                            fieldInfo.SetValue(obj, v);
                    }
                }

                //on deserialize callback, if any
                List<MethodInfo> methods =
                    new(type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                foreach (MethodInfo method in methods)
                {
                    object[] deserializedAttributes =
                        method.GetCustomAttributes(typeof(OnDeserializeCallbackAttribute), false);
                    if (deserializedAttributes != null && deserializedAttributes.Length > 0)
                        //invoke the OnDeserialize callback
                        method.Invoke(obj, null);
                }

                return obj;
            }

            //Split a string excluding any characters found inside specified blocks
            //e.g.
            //  splitExcludingBlocks("list(a,b,c),list(d,e),list(f,g,h)", "()") will return
            //will return
            //  list(a,b,c)   list(d,e)   list(f,g,h)
            //and not
            // list(a   b   c)   list(d   e   list(f   g   h
            public static string[] SplitExcludingBlocks(string input, char separator, params string[] blocks)
            {
                return SplitExcludingBlocks(input, separator, false, false, blocks);
            }

            public static string[] SplitExcludingBlocks(string input, char separator, bool excludeQuotes,
                params string[] blocks)
            {
                return SplitExcludingBlocks(input, separator, excludeQuotes, false, blocks);
            }

            public static string[] SplitExcludingBlocks(string input, char separator, bool excludeQuotes,
                bool removeEmptyEntries, params string[] blocks)
            {
                foreach (string block in blocks)
                    if (block == "\"\"")
                        Debug.LogWarning("Using quotes block \"\" -> use excludeQuotes=true instead!");

                int insideBlock = 0;
                bool insideQuotes = false;
                int i = 0;
                StringBuilder currentWord = new StringBuilder();
                List<string> words = new List<string>();

                //get opening/ending chars for blocks
                List<char> openingChars = new List<char>(blocks.Length);
                List<char> closingChars = new List<char>(blocks.Length);
                foreach (string block in blocks)
                {
                    openingChars.Add(block[0]);
                    closingChars.Add(block[1]);
                }

                while (i < input.Length)
                {
                    if (!insideQuotes)
                    {
                        if (openingChars.Contains(input[i]))
                            insideBlock++;
                        else if (closingChars.Contains(input[i]))
                            insideBlock--;
                    }

                    if (excludeQuotes && input[i] == '"')
                    {
                        insideQuotes = !insideQuotes;
                        insideBlock += insideQuotes ? +1 : -1;
                    }

                    if (input[i] == separator && insideBlock == 0)
                    {
                        if (!removeEmptyEntries || currentWord.Length != 0) words.Add(currentWord.ToString());
                        currentWord.Length = 0;
                    }
                    else
                    {
                        currentWord.Append(input[i]);
                    }

                    i++;
                }

                if (!removeEmptyEntries || currentWord.Length != 0) words.Add(currentWord.ToString());

                return words.ToArray();
            }

            /// <summary>
            ///     Declare a class or field as serializable, and set its serialized short name
            /// </summary>
            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Property)]
            public class SerializeAsAttribute : Attribute
            {
	            /// <summary>
	            ///     Name of the field or property that will determine if the object can be serialized.
	            ///     Originally used to check if a Shader Property has been manually modified.
	            /// </summary>
	            public string conditionalField;

	            /// <summary>
	            ///     The short name to serialize that object, to reduce length of the serialized string.
	            /// </summary>
	            public string serializedName;

                public SerializeAsAttribute(string name, string conditionalField = null)
                {
                    serializedName = name;
                    this.conditionalField = conditionalField;
                }
            }

            /// <summary>
            ///     Force serialization, regardless of the "conditionalField" attribute value
            /// </summary>
            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
            public class ForceSerializationAttribute : Attribute
            {
            }

            /// <summary>
            ///     Declare a method as a callback to deserialize an object manually
            /// </summary>
            [AttributeUsage(AttributeTargets.Method)]
            public class CustomDeserializeCallbackAttribute : Attribute
            {
            }

            /// <summary>
            ///     Declare a method as a callback after an object has been deserialized
            /// </summary>
            [AttributeUsage(AttributeTargets.Method)]
            public class OnDeserializeCallbackAttribute : Attribute
            {
            }
        }
    }
}
