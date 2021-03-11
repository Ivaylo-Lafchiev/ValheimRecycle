using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ValheimRecycle
{
    public class Utils
    {


        public static bool HaveEmptySlotsForRecipe(Inventory inventory, Recipe recipe, int quality)
        {
            int emptySlots = inventory.GetEmptySlots();
            int requiredSlots = 0;

            foreach (Piece.Requirement req in recipe.m_resources)
            {
                if (req.GetAmount(quality) > 0) requiredSlots++;
            }
            if (emptySlots >= requiredSlots) return true;
            return false;
        }

        public static void AddResources(Inventory inventory, Piece.Requirement[] requirements, int qualityLevel)
        {

            foreach (Piece.Requirement requirement in requirements)
            {
                if (requirement.m_resItem)
                {

                    int amount = requirement.GetAmount(qualityLevel + 1);
                    amount = (int)Math.Round(ValheimRecycle.instance.resourceMultiplier.Value * amount, 0);
                    if (amount > 0)
                    {
                        Debug.Log("Adding item: " + requirement.m_resItem.name);
                        Debug.Log("Amount: " + requirement.GetAmount(qualityLevel + 1));

                        inventory.AddItem(requirement.m_resItem.name, amount, requirement.m_resItem.m_itemData.m_quality, requirement.m_resItem.m_itemData.m_variant, 0L, "");
                    }
                }
            }
        }
        public class ObjectDumper
        {
            private int _level;
            private readonly int _indentSize;
            private readonly StringBuilder _stringBuilder;
            private readonly List<int> _hashListOfFoundElements;

            private ObjectDumper(int indentSize)
            {
                _indentSize = indentSize;
                _stringBuilder = new StringBuilder();
                _hashListOfFoundElements = new List<int>();
            }

            public static string Dump(object element)
            {
                return Dump(element, 2);
            }

            public static string Dump(object element, int indentSize)
            {
                var instance = new ObjectDumper(indentSize);
                return instance.DumpElement(element);
            }

            private string DumpElement(object element)
            {
                if (element == null || element is ValueType || element is string)
                {
                    Write(FormatValue(element));
                }
                else
                {
                    var objectType = element.GetType();
                    if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                    {
                        Write("{{{0}}}", objectType.FullName);
                        _hashListOfFoundElements.Add(element.GetHashCode());
                        _level++;
                    }

                    var enumerableElement = element as IEnumerable;
                    if (enumerableElement != null)
                    {
                        foreach (object item in enumerableElement)
                        {
                            if (item is IEnumerable && !(item is string))
                            {
                                _level++;
                                DumpElement(item);
                                _level--;
                            }
                            else
                            {
                                if (!AlreadyTouched(item))
                                    DumpElement(item);
                                else
                                    Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                            }
                        }
                    }
                    else
                    {
                        MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                        foreach (var memberInfo in members)
                        {
                            var fieldInfo = memberInfo as FieldInfo;
                            var propertyInfo = memberInfo as PropertyInfo;

                            if (fieldInfo == null && propertyInfo == null)
                                continue;

                            var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                            object value = fieldInfo != null
                                               ? fieldInfo.GetValue(element)
                                               : propertyInfo.GetValue(element, null);

                            if (type.IsValueType || type == typeof(string))
                            {
                                Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                            }
                            else
                            {
                                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                                Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                                var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                                _level++;
                                if (!alreadyTouched)
                                    DumpElement(value);
                                else
                                    Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                                _level--;
                            }
                        }
                    }

                    if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                    {
                        _level--;
                    }
                }

                return _stringBuilder.ToString();
            }

            private bool AlreadyTouched(object value)
            {
                if (value == null)
                    return false;

                var hash = value.GetHashCode();
                for (var i = 0; i < _hashListOfFoundElements.Count; i++)
                {
                    if (_hashListOfFoundElements[i] == hash)
                        return true;
                }
                return false;
            }

            private void Write(string value, params object[] args)
            {
                var space = new string(' ', _level * _indentSize);

                if (args != null)
                    value = string.Format(value, args);

                _stringBuilder.AppendLine(space + value);
            }

            private string FormatValue(object o)
            {
                if (o == null)
                    return ("null");

                if (o is DateTime)
                    return (((DateTime)o).ToShortDateString());

                if (o is string)
                    return string.Format("\"{0}\"", o);

                if (o is char && (char)o == '\0')
                    return string.Empty;

                if (o is ValueType)
                    return (o.ToString());

                if (o is IEnumerable)
                    return ("...");

                return ("{ }");
            }
        }
    }


}
