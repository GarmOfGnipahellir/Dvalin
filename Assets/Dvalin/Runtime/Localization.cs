using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace Dvalin
{
    /// <summary>
    /// Handles translation of plugin content.
    /// </summary>
    public class Localization : IDestroyable
    {
        private TextAsset m_localization;
        private Dictionary<string, string> m_translations = new Dictionary<string, string>();

        public Localization(TextAsset localization)
        {
            m_localization = localization;

            Patches.Localization.SetupLanguage.PostfixEvent += SetupLanguage;
            Patches.Localization.Translate.PostfixEvent += TryTranslate;

            SetupLanguage(global::Localization.instance.GetSelectedLanguage());
        }

        public void Destroy()
        {
            Patches.Localization.SetupLanguage.PostfixEvent -= SetupLanguage;
            Patches.Localization.Translate.PostfixEvent -= TryTranslate;
        }

        // heavily based on valheims language setup
        public void SetupLanguage(string language)
        {
            if (m_localization == null)
            {
                Logger.LogWarning("Failed to load language file");
                return;
            }

            StringReader reader = new StringReader(m_localization.text);
            string[] strArray = reader.ReadLine().Split(',');
            int index1 = -1;
            for (int index2 = 0; index2 < strArray.Length; ++index2)
            {
                if (strArray[index2] == language)
                {
                    index1 = index2;
                    break;
                }
            }
            if (index1 == -1)
            {
                Logger.LogWarning("Failed to find language:" + language);
                return;
            }
            foreach (List<string> stringList in DoQuoteLineSplit(reader))
            {
                if (stringList.Count != 0)
                {
                    string key = stringList[0];
                    if (!key.StartsWith("//") && key.Length != 0 && stringList.Count > index1)
                    {
                        string text = stringList[index1];
                        if (string.IsNullOrEmpty(text) || text[0] == '\r')
                            text = stringList[1];
                        AddWord(key, text);
                    }
                }
            }
            Logger.LogInfo("Loaded localization " + language);
        }

        public bool TryTranslate(string word, out string translated)
        {
            if (word.StartsWith("KEY_"))
            {
                translated = global::Localization.instance.GetBoundKeyString(word.Substring(4));
                return true;
            }

            if (m_translations.TryGetValue(word, out translated))
            {
                return true;
            }

            translated = "";
            return false;
        }

        // everything below is basically straight copy-pasta
        // from decompiled valheim source code

        private void AddWord(string key, string text)
        {
            m_translations.Remove(key);
            m_translations.Add(key, text);
        }

        private List<List<string>> DoQuoteLineSplit(TextReader reader)
        {
            List<List<string>> stringListList = new List<List<string>>();
            List<string> stringList = new List<string>();
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = false;
            while (true)
            {
                int num = reader.Read();
                switch (num)
                {
                    case -1:
                        goto label_2;
                    case 34:
                        flag = !flag;
                        continue;
                    default:
                        if (num == 44 && !flag)
                        {
                            stringList.Add(stringBuilder.ToString());
                            stringBuilder.Length = 0;
                            continue;
                        }
                        if (num == 10 && !flag)
                        {
                            stringList.Add(stringBuilder.ToString());
                            stringBuilder.Length = 0;
                            stringListList.Add(stringList);
                            stringList = new List<string>();
                            continue;
                        }
                        stringBuilder.Append((char)num);
                        continue;
                }
            }
        label_2:
            stringList.Add(stringBuilder.ToString());
            stringListList.Add(stringList);
            return stringListList;
        }
    }
}