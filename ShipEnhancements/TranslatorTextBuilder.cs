using System;
using System.Xml;
using UnityEngine;

namespace ShipEnhancements;

public static class TranslatorTextBuilder
{
    public static void AddTranslation(string content)
    {
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(content);

        XmlNode xmlNode = xmlDocument.SelectSingleNode("NomaiObject");
        XmlNodeList xmlNodeList = xmlNode.SelectNodes("TextBlock");

        foreach (object obj in xmlNodeList)
        {
            XmlNode xmlNode2 = (XmlNode)obj;
            var text = xmlNode2.SelectSingleNode("Text").InnerText;
            TranslationHandler.AddDialogue(text);
        }
    }
}
