using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Paragon.Plugins.ScreenCapture
{
    public static class LocaleUtil
    {
        public static void SwitchLanguage(FrameworkElement element, string localeTag)
        {
            string localeFilePath = GetLocaleXAMLFilePath(GetElementName(element), localeTag);

            if (File.Exists(localeFilePath))
            {
                var languageDictionary = new ResourceDictionary
                {
                    Source = new Uri(localeFilePath)
                };

                var foundDict = -1;
                // Remove any localization dictionaries loaded
                for (int i = 0; i < element.Resources.MergedDictionaries.Count; i++)
                {
                    var mergedDict = element.Resources.MergedDictionaries[i];
                    if (mergedDict.Contains("ResourceDictionaryName"))
                    {
                        if (mergedDict["ResourceDictionaryName"].ToString().StartsWith("Loc-"))
                        {
                            foundDict = i;
                            break;
                        }
                    }
                }

                if (foundDict == -1)
                {
                    element.Resources.MergedDictionaries.Add(languageDictionary);
                }
                else
                {
                    element.Resources.MergedDictionaries[foundDict] = languageDictionary;
                }
            }
        }

        // Returns the locale xaml file path
        private static string GetLocaleXAMLFilePath(string element, string localeTag)
        {
            string localeFilePath = element + "." + localeTag + ".xaml";
            string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string directoryPath = Path.Combine(directory, "i18N");
            return Path.Combine(directoryPath, localeFilePath);
        }

        // Returns the element name
        private static string GetElementName(FrameworkElement element)
        {
            string elementType = element.GetType().ToString();
            var elementNames = elementType.Split('.');
            string elementName = "";
            if (elementNames.Length >= 2)
            {
                elementName = elementNames[elementNames.Length - 1];
            }
            return elementName;
        }
    }
}
