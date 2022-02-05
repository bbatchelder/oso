using System.Text.Json;

namespace Oso;

public class Host
{
    private readonly Dictionary<ulong, object> _instances = new();

    public bool AcceptExpression { get; set; }
    public Dictionary<string, object> DeserializePolarDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new OsoException($"Expected a JSON object element, received {element.ValueKind}");

        return element.EnumerateObject()
                        .Select(property => new KeyValuePair<string, object>(property.Name, ParsePolarTerm(property.Value)))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public List<object> DeserializePolarList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            throw new OsoException($"Expected a JSON array element, received {element.ValueKind}");

        return element.EnumerateArray().Select(ParsePolarTerm).ToList();
    }

    /// <summary>
    /// Make an instance of a class from a <see cref="List&lt;object&gt;" /> of fields. 
    /// </summary>
    internal void MakeInstance(string className, List<object> constructorArgs, ulong instanceId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    internal bool IsA(JsonElement instance, string className)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    internal bool IsSubclass(string leftTag, string rightTag)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// </summary>
    internal bool Subspecializer(ulong instanceId, string leftTag, string rightTag)
    {
        throw new NotImplementedException();
    }
    /// <summary>
    /// Turn a Polar term passed across the FFI boundary into an <see cref="object" />.
    /// </summary>
    public object ParsePolarTerm(JsonElement term)
    {
        /*
            {
                "value": {"String": "someValue" }
            }

         */
        // TODO: Would this be better as a JsonConverter?
        JsonElement value = term.GetProperty("value");
        var property = value.EnumerateObject().First();
        string tag = property.Name;
        switch (tag)
        {
            case "String":
                return property.Value.GetString();
            case "Boolean":
                return property.Value.GetBoolean();
            case "Number":
                JsonProperty numProperty = property.Value.EnumerateObject().First();
                string numType = numProperty.Name;
                switch (numType)
                {
                    case "Integer":
                        return numProperty.Value.GetInt32();
                    case "Float":
                        if (numProperty.Value.ValueKind == JsonValueKind.String)
                        {
                            return numProperty.Value.GetString() switch
                            {
                                "Infinity" => double.PositiveInfinity,
                                "-Infinity" => double.NegativeInfinity,
                                "NaN" => double.NaN,
                                var f => throw new OsoException($"Expected a floating point number, got `{f}`"),
                            };
                        }
                        return numProperty.Value.GetDouble();
                }
                throw new OsoException("Unexpected Number type: {numType}");
            case "List":
                return DeserializePolarList(property.Value);
            case "Dictionary":
                return DeserializePolarDictionary(property.Value.GetProperty("fields"));
            case "ExternalInstance":
                throw new NotImplementedException();
            // return getInstance(property.Value.GetProperty("instance_id").GetUInt64());
            case "Call":
                List<object> args = DeserializePolarList(property.Value.GetProperty("args"));
                throw new NotImplementedException();
            // return new Predicate(property.Value.GetProperty("name").GetString(), args);
            case "Variable":
                throw new NotImplementedException();
            // return new Variable(property.Value.GetString());
            case "Expression":
                if (!AcceptExpression)
                {
                    // TODO: More specific exceptions?
                    // throw new Exceptions.UnexpectedPolarTypeError(Exceptions.UNEXPECTED_EXPRESSION_MESSAGE);
                    const string unexpectedExpressionMessage = "Received Expression from Polar VM. The Expression type is only supported when\n"
                        + "using data filtering features. Did you perform an "
                        + "operation over an unbound variable in your policy?\n\n"
                        + "To silence this error and receive an Expression result, pass\n"
                        + "acceptExpression as true to Oso.query.";
                    throw new OsoException(unexpectedExpressionMessage);
                }
                throw new NotImplementedException();
            /*
            return new Expression(
                Enum.Parse<Operator>(property.Value.GetProperty("operator").GetString()),
                DeserializePolarList(property.Value.GetProperty("args")));
                */
            case "Pattern":
                throw new NotImplementedException();
            /*
            JsonProperty pattern = value.GetProperty("Pattern");
            string patternTag = pattern.Name;
            return patternTag switch
            {
                "Instance" => new Pattern(
                                            pattern.Value.GetProperty("tag").GetString,
                                            DeserializePolarDictionary(pattern.Value.GetProperty("fields").GetProperty("fields"))),
                "Dictionary" => new Pattern(null, DeserializePolarDictionary(pattern.Value)),
                _ => throw new Exceptions.UnexpectedPolarTypeError("Pattern: " + patternTag),
            };
            */
            default:
                // throw new Exceptions.UnexpectedPolarTypeError(tag);
                // TODO: Rename PolarException to OsoException.
                throw new OsoException($"Unexpected polar type: {tag}");
        }
    }

    public JsonElement SerializePolarTerm(object value)
    {
        // Build Polar value
        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream);
        writer.WriteStartObject();
        writer.WriteNumber("id", 0);
        writer.WriteNumber("offset", 0);
        writer.WriteStartObject("value");
        if (value is bool b)
        {
            writer.WriteBoolean("Boolean", b);
        }
        else if (value is int i)
        {
            writer.WriteStartObject("Number");
            writer.WriteNumber("Integer", i);
            writer.WriteEndObject();
        }
        else if (value is double or float)
        {
            writer.WriteStartObject("Number");
            writer.WritePropertyName("Float");
            double doubleValue = (double)value;
            if (double.IsPositiveInfinity(doubleValue))
            {
                writer.WriteStringValue("Infinity");
            }
            else if (double.IsNegativeInfinity(doubleValue))
            {
                writer.WriteStringValue("-Infinity");
            }
            else if (double.IsNaN(doubleValue))
            {
                writer.WriteStringValue("NaN");
            }
            else
            {
                writer.WriteNumberValue(doubleValue);
            }
            writer.WriteEndObject();
        } else if (value is string stringValue) {
            writer.WriteString("String", stringValue);
        } else if (value != null && (value.GetType().IsArray || (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(List<>))))
        {
            SerializePolarList(writer, value);
        }
        else if (value != null && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            SerializePolarDictionary(writer, value);
        }
        /*
        else if (value is Predicate pred)
        {
            if (pred.args == null) pred.args = new ArrayList<Object>();
            jVal.put(
                "Call", new JsonElement(Dictionary.of("name", pred.name, "args", javaListToPolar(pred.args))));
        }
        else if (value is Variable variable)
        {
            jVal.put("Variable", value);
            writer.WriteStartObject("Variable");
        }
        else if (value is Expression expression)
        {
            jVal.put("Expression", expressionJSON);
            writer.WriteStartObject("Expression");
            writer.WriteString("operator", expression.Operator.ToString());
            writer.WriteStartArray("args");

            expressionJSON.put("args", javaListToPolar(expression.getArgs()));
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        else if (value is Pattern pattern)
        {
            if (pattern.getTag() == null)
            {
                jVal.put("Pattern", toPolarTerm(pattern.getFields()));
            }
            else
            {
                JsonElement fieldsJSON = new JsonElement();
                fieldsJSON.put("fields", javaDictionarytoPolar(pattern.getFields()));

                JsonElement instanceJSON = new JsonElement();
                instanceJSON.put("tag", pattern.getTag());
                instanceJSON.put("fields", fieldsJSON);

                JsonElement patternJSON = new JsonElement();
                patternJSON.put("Instance", instanceJSON);

                jVal.put("Pattern", patternJSON);
            }
        }
        else
        {
            writer.WriteStartObject("ExternalInstance");
            long instanceId;

            // if the object is a Class, then it will already have an instance ID
            if (value is Type) {
                instanceId = classIds.get(value);
            }

            // attrs.put("instance_id", cacheInstance(value, instanceId));
            // attrs.put("repr", value?.ToString() ?? "null"); // value == null ? "null" : value.toString());
            writer.WriteStartObject("instance_id", cacheInstance(value, instanceId));
            writer.WriteString("repr", value?.ToString());

            // pass a class_repr string *for registered types only*
            if (value != null)
            {
                Class classFromValue = value.getClass();
                String stringifiedClassFromValue = classFromValue.toString();
                stringifiedClassFromValue =
                    classIds.containsKey(classFromValue) ? stringifiedClassFromValue : "null";
                attrs.put("class_repr", stringifiedClassFromValue);
            }
            else
            {
                writer.WriteNull("class_repr");
            }

            writer.WriteEndObject();
        }
        */

        // Build Polar term
        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();
        var reader = new Utf8JsonReader(stream.ToArray());
        return JsonElement.ParseValue(ref reader);
    }

    void SerializePolarList(Utf8JsonWriter writer, object listLikeObject)
    {
        // We support int, double, float, bool, and string
        writer.WriteStartArray("List");
        if (listLikeObject is IEnumerable<int> intList)
        {
            foreach(var element in intList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else if (listLikeObject is IEnumerable<double> doubleList)
        {
            foreach(var element in doubleList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else if (listLikeObject is IEnumerable<float> floatList)
        {
            foreach(var element in floatList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else if (listLikeObject is IEnumerable<bool> boolList)
        {
            foreach(var element in boolList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else if (listLikeObject is IEnumerable<string> stringList)
        {
            foreach(var element in stringList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else if (listLikeObject is IEnumerable<object> objList)
        {
            foreach(var element in objList)
            {
                writer.WriteRawValue(SerializePolarTerm(element).ToString());
            }
        }
        else
        {
            throw new OsoException($"Cannot support list of type {listLikeObject.GetType()}.");
        }

        writer.WriteEndArray();
    }

    void SerializePolarDictionary(Utf8JsonWriter writer, object dictObject)
    {
        writer.WritePropertyName("Dictionary");
        writer.WriteStartObject();
        writer.WriteStartObject("fields");
        // Polar only supports dictionaries with string keys. Convert a map to a map of
        // string keys.
        if (dictObject is Dictionary<string, int> intMap)
        {
            foreach (var (k, v) in intMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else if (dictObject is Dictionary<string, double> doubleMap)
        {
            foreach (var (k, v) in doubleMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else if (dictObject is Dictionary<string, float> floatMap)
        {
            foreach (var (k, v) in floatMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else if (dictObject is Dictionary<string, bool> boolMap)
        {
            foreach (var (k, v) in boolMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else if (dictObject is Dictionary<string, string> stringMap)
        {
            foreach (var (k, v) in stringMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else if (dictObject is Dictionary<string, object> objMap)
        {
            foreach (var (k, v) in objMap)
            {
                writer.WritePropertyName(k);
                writer.WriteRawValue(SerializePolarTerm(v).ToString());
            }
        }
        else
        {
            //throw new Exceptions.UnexpectedPolarTypeError("Cannot convert map with non-string keys to Polar");
            throw new OsoException("Unexpected polar type: Cannot convert map with non-string keys to Polar");
        }
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    public bool Operator(string op, List<object> args)
    {
        throw new NotImplementedException();
        /*
        Object left = args.get(0), right = args.get(1);
        if (op.equals("Eq")) {
            if (left == null) return left == right;
            else return left.equals(right);
        }
        throw new Exceptions.UnimplementedOperation(op);
        */
    }

    /// <summary>
    /// Determine if an instance has been cached.
    /// </summary>
    public bool HasInstance(ulong instanceId) => _instances.ContainsKey(instanceId);

    private object GetInstance(ulong instanceId)
    {
        return _instances.TryGetValue(instanceId, out object? value)
            ? value
            : throw new OsoException($"Unregistered instance: {instanceId}");
    }
}