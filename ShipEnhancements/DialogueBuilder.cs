using OWML.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

namespace ShipEnhancements;

// Borrowed from New Horizons
// I would use New Horizons but I don't want this mod to have any dependencies
public static class DialogueBuilder
{
    public static void Make(GameObject go, string dialoguePath, string xmlPath, IModBehaviour mod)
    {
        var xml = File.ReadAllText(Path.Combine(mod.ModHelper.Manifest.ModFolderPath, xmlPath));
        var dialogueName = Path.GetFileNameWithoutExtension(xmlPath);
        AddToExistingDialogue(go, dialoguePath, xml, dialogueName);
    }

    private static CharacterDialogueTree AddToExistingDialogue(GameObject go, string dialoguePath, string xml, string dialogueName)
    {
        var dialogueObject = go.transform.Find(dialoguePath);
        //if (dialogueObject == null) dialogueObject = SearchUtilities.Find(info.pathToExistingDialogue);
        var existingDialogue = dialogueObject != null ? dialogueObject.GetComponent<CharacterDialogueTree>() : null;

        if (existingDialogue == null)
        {
            //ShipEnhancements.WriteDebugMessage($"Couldn't find dialogue at {dialoguePath}!", error: true);
            return null;
        }

        var existingAsset = existingDialogue._xmlCharacterDialogueAsset;
        if (existingAsset == null)
        {
            var dialogueDoc = new XmlDocument();
            dialogueDoc.LoadXml(xml);
            var xmlNode = dialogueDoc.SelectSingleNode("DialogueTree");
            AddTranslation(xmlNode);

            xml = xmlNode.OuterXml;

            var text = new TextAsset(xml)
            {
                // Text assets need a name to be used with VoiceMod
                name = dialogueName
            };
            existingDialogue.SetTextXml(text);

            FixDialogueNextFrame(existingDialogue);

            return existingDialogue;
        }

        var existingText = existingAsset.text;

        var existingDialogueDoc = new XmlDocument();
        existingDialogueDoc.LoadXml(existingText);
        var existingDialogueTree = existingDialogueDoc.DocumentElement.SelectSingleNode("//DialogueTree");

        var existingDialogueNodesByName = new Dictionary<string, XmlNode>();
        foreach (XmlNode existingDialogueNode in existingDialogueTree.GetChildNodes("DialogueNode"))
        {
            var name = existingDialogueNode.GetChildNode("Name").InnerText;
            existingDialogueNodesByName[name] = existingDialogueNode;
        }

        var additionalDialogueDoc = new XmlDocument();
        additionalDialogueDoc.LoadXml(xml);
        var newDialogueNodes = additionalDialogueDoc.DocumentElement.SelectSingleNode("//DialogueTree").GetChildNodes("DialogueNode");

        foreach (XmlNode newDialogueNode in newDialogueNodes)
        {
            var name = newDialogueNode.GetChildNode("Name").InnerText;

            if (existingDialogueNodesByName.TryGetValue(name, out var existingNode))
            {
                // We just have to merge the dialogue options
                var dialogueOptions = newDialogueNode.GetChildNode("DialogueOptionsList").GetChildNodes("DialogueOption");
                var existingDialogueOptionsList = existingNode.GetChildNode("DialogueOptionsList");
                if (existingDialogueOptionsList == null)
                {
                    existingDialogueOptionsList = existingDialogueDoc.CreateElement("DialogueOptionsList");
                    existingNode.AppendChild(existingDialogueOptionsList);
                }
                foreach (XmlNode node in dialogueOptions)
                {
                    var importedNode = existingDialogueOptionsList.OwnerDocument.ImportNode(node, true);
                    // We add them to the start because normally the last option is to return to menu or exit
                    existingDialogueOptionsList.PrependChild(importedNode);
                }
            }
            else
            {
                // We add the new dialogue node to the existing dialogue
                var importedNode = existingDialogueTree.OwnerDocument.ImportNode(newDialogueNode, true);
                existingDialogueTree.AppendChild(importedNode);
            }
        }

        // Character name is required for adding translations, something to do with how OW prefixes its dialogue
        var characterName = existingDialogueTree.SelectSingleNode("NameField").InnerText;
        AddTranslation(additionalDialogueDoc.GetChildNode("DialogueTree"), characterName);

        var newTextAsset = new TextAsset(existingDialogueDoc.OuterXml)
        {
            name = existingDialogue._xmlCharacterDialogueAsset.name
        };

        existingDialogue.SetTextXml(newTextAsset);

        FixDialogueNextFrame(existingDialogue);

        //MakeAttentionPoints(go, sector, existingDialogue, info);

        return existingDialogue;
    }

    private static void FixDialogueNextFrame(CharacterDialogueTree characterDialogueTree)
    {
        ShipEnhancements.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
        {
            var rawText = characterDialogueTree._xmlCharacterDialogueAsset.text;

            var doc = new XmlDocument();
            doc.LoadXml(rawText);
            var dialogueTree = doc.DocumentElement.SelectSingleNode("//DialogueTree");

            DoDialogueOptionsListReplacement(dialogueTree);

            var newTextAsset = new TextAsset(doc.OuterXml)
            {
                name = characterDialogueTree._xmlCharacterDialogueAsset.name
            };

            characterDialogueTree.SetTextXml(newTextAsset);
        });
    }

    /// <summary>
    /// Always call this after adding translations, else it won't update them properly
    /// </summary>
    /// <param name="dialogueTree"></param>
    private static void DoDialogueOptionsListReplacement(XmlNode dialogueTree)
    {
        var optionsListsByName = new Dictionary<string, XmlNode>();
        var dialogueNodes = dialogueTree.GetChildNodes("DialogueNode");
        foreach (XmlNode dialogueNode in dialogueNodes)
        {
            var optionsList = dialogueNode.GetChildNode("DialogueOptionsList");
            if (optionsList != null)
            {
                var name = dialogueNode.GetChildNode("Name").InnerText;
                optionsListsByName[name] = optionsList;
            }
        }
        foreach (var (name, optionsList) in optionsListsByName)
        {
            var replacement = optionsList.GetChildNode("ReuseDialogueOptionsListFrom");
            if (replacement != null)
            {
                var replacementName = replacement.InnerText;
                if (optionsListsByName.TryGetValue(replacementName, out var replacementOptionsList))
                {
                    if (replacementOptionsList.GetChildNode("ReuseDialogueOptionsListFrom") != null)
                    {
                        //ShipEnhancements.WriteDebugMessage($"Can not target a node with ReuseDialogueOptionsListFrom that also reuses options when making dialogue. Node {name} cannot reuse the list from {replacement.InnerText}", error: true);
                    }
                    var dialogueNode = optionsList.ParentNode;
                    dialogueNode.RemoveChild(optionsList);
                    dialogueNode.AppendChild(replacementOptionsList.Clone());

                    // Have to manually fix the translations here
                    var characterName = dialogueTree.SelectSingleNode("NameField").InnerText;

                    var xmlText = replacementOptionsList.SelectNodes("DialogueOption/Text");
                    foreach (object option in xmlText)
                    {
                        var optionData = (XmlNode)option;
                        var text = optionData.InnerText.Trim();
                        TranslationHandler.ReuseDialogueTranslation(text, new string[] { characterName, replacementName }, new string[] { characterName, name });
                    }
                }
                else
                {
                    //ShipEnhancements.WriteDebugMessage($"Could not reuse dialogue options list from node with Name {replacement.InnerText} to node with Name {name}", error: true);
                }
            }
        }
    }

    public static void AddTranslation(XmlNode xmlNode, string characterName = null)
    {
        var xmlNodeList = xmlNode.SelectNodes("DialogueNode");

        // When adding dialogue to existing stuff, we have to pass in the character name
        // Otherwise we translate it if its from a new dialogue object
        if (characterName == null)
        {
            characterName = xmlNode.SelectSingleNode("NameField").InnerText;
            TranslationHandler.AddDialogue(characterName);
        }

        foreach (object obj in xmlNodeList)
        {
            var xmlNode2 = (XmlNode)obj;
            var name = xmlNode2.SelectSingleNode("Name").InnerText;

            var xmlText = xmlNode2.SelectNodes("Dialogue/Page");
            foreach (object page in xmlText)
            {
                var pageData = (XmlNode)page;
                var text = pageData.InnerText;
                // The text is trimmed in DialogueText constructor (_listTextBlocks), so we also need to trim it for the key
                TranslationHandler.AddDialogue(text, true, name);
            }

            xmlText = xmlNode2.SelectNodes("DialogueOptionsList/DialogueOption/Text");
            foreach (object option in xmlText)
            {
                var optionData = (XmlNode)option;
                var text = optionData.InnerText;
                // The text is trimmed in CharacterDialogueTree.LoadXml, so we also need to trim it for the key
                TranslationHandler.AddDialogue(text, true, characterName, name);
            }
        }
    }

    private static Dictionary<string, string> _dialogueTranslationDictionary = new();

    public static List<XmlNode> GetChildNodes(this XmlNode parentNode, string tagName)
    {
        return parentNode.ChildNodes.Cast<XmlNode>().Where(node => node.LocalName == tagName).ToList();
    }

    public static XmlNode GetChildNode(this XmlNode parentNode, string tagName)
    {
        return parentNode.ChildNodes.Cast<XmlNode>().FirstOrDefault(node => node.LocalName == tagName);
    }
}
