using System;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ObjectRandomizer
{
    private static readonly BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static T RandomizeObject<T>(T value)
    {
        object boxed = value;
        RandomizeObject(boxed);
        return (T)boxed;
    }

    private static void RandomizeObject(object obj)
    {
        // Debug.Log($"Randomizing obj({obj}) of type({(obj?.GetType())})");
        if (obj == null)
        {
            // Debug.Log("Skipping null");
            return;
        }
        Type type = obj.GetType();

        foreach (FieldInfo field in type.GetFields(Flags))
        {
            // Debug.Log($"Randomizing field({field})");
            if (field.IsInitOnly)
                continue;

            Type fieldType = field.FieldType;

            // float (+ Range attribute support)
            if (fieldType == typeof(float))
            {
                RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

                float value = range != null
                    ? UnityEngine.Random.Range(range.min, range.max)
                    : UnityEngine.Random.value;

                field.SetValue(obj, value);
            }

            // int
            else if (fieldType == typeof(int))
            {
                RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();

                int value = range != null
                    ? UnityEngine.Random.Range(
                        Mathf.RoundToInt(range.min),
                        Mathf.RoundToInt(range.max) + 1)
                    : UnityEngine.Random.Range(0, 100);

                field.SetValue(obj, value);
            }

            // bool
            else if (fieldType == typeof(bool))
            {
                field.SetValue(obj, UnityEngine.Random.value > 0.5f);
            }

            // enum
            else if (fieldType.IsEnum)
            {
                Array values = Enum.GetValues(fieldType);
                field.SetValue(obj, values.GetValue(UnityEngine.Random.Range(0, values.Length)));
            }

            // Color
            else if (fieldType == typeof(Color))
            {
                field.SetValue(obj, Random.ColorHSV(
                    0f, 1f,
                    0.5f, 1f,
                    0.5f, 1f));
            }

            // Nested struct
            else if (fieldType.IsValueType && fieldType.Assembly == typeof(CreatureVisuals).Assembly)
            {
                object nested = field.GetValue(obj);
                RandomizeObject(nested);
                field.SetValue(obj, nested);
            }
        }
    }
}
