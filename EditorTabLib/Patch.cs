﻿using ADOFAI;
using DG.Tweening;
using EditorTabLib.Components;
using EditorTabLib.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EditorTabLib
{
    internal static class Patch
    {
        [HarmonyPatch]
        public static class ParseEnumPatch
        {
            public static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(RDUtils), "ParseEnum", null, null).MakeGenericMethod(new Type[]
                {
                typeof(LevelEventType)
                });
            }

            public static bool cancelled = true;

            public static bool Prefix(string str, ref LevelEventType __result)
            {
                if (CustomTabManager.byName.TryGetValue(str, out CustomTabManager.CustomTab tab))
                {
                    __result = (LevelEventType)tab.type;
                    cancelled = false;
                    return false;
                }
                return true;
            }

            public static void Postfix(string str, ref LevelEventType __result)
            {
                if (!cancelled)
                {
                    cancelled = true;
                    return;
                }
                if (CustomTabManager.byName.TryGetValue(str, out CustomTabManager.CustomTab tab))
                    __result = (LevelEventType)tab.type;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "Awake")]
        public static class AwakePatch
        {
            public static bool Prefix()
            {
                Main.AddOrDeleteAllTabs(true);
                return true;
            }
            public static void Postfix()
            {
                CustomTabManager.SortTab();
            }
        }

        [HarmonyPatch(typeof(EditorConstants), "IsSetting")]
        public static class IsSettingPatch
        {
            public static void Postfix(LevelEventType type, ref bool __result)
            {
                if (CustomTabManager.byType.ContainsKey((int)type))
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(scnEditor), "GetSelectedFloorEvents")]
        public static class GetSelectedFloorEventsPatch
        {
            public static void Postfix(LevelEventType eventType, ref List<LevelEvent> __result)
            {
                if (CustomTabManager.byType.ContainsKey((int)eventType) && __result == null)
                    __result = new List<LevelEvent>();
            }
        }

        [HarmonyPatch(typeof(InspectorPanel), "ShowPanel")]
        public static class ShowPanelPatch
        {
            public static bool cancelled = true;

            public static bool Prefix(InspectorPanel __instance, LevelEventType eventType, int eventIndex = 0)
            {
                Postfix(__instance, eventType, eventIndex);
                cancelled = false;
                return !CustomTabManager.byType.ContainsKey((int)eventType);
            }

            public static void Postfix(InspectorPanel __instance, LevelEventType eventType, int eventIndex = 0)
            {
                if (!cancelled)
                {
                    cancelled = true;
                    return;
                }
                if (!CustomTabManager.byType.TryGetValue((int)eventType, out CustomTabManager.CustomTab tab))
                    return;
                __instance.Set("showingPanel", true);
                __instance.editor.SaveState(true, false);
                __instance.editor.changingState++;
                PropertiesPanel propertiesPanel = null;
                foreach (PropertiesPanel propertiesPanel2 in __instance.panelsList)
                {
                    if (propertiesPanel2.levelEventType == eventType)
                    {
                        propertiesPanel2.gameObject.SetActive(true);
                        propertiesPanel = propertiesPanel2;
                    }
                    else
                        propertiesPanel2.gameObject.SetActive(false);
                }
                __instance.title.text =
                    tab.title.TryGetValue(RDString.language, out string title) ?
                    title :
                    (tab.title.TryGetValue(SystemLanguage.English, out title) ?
                    title :
                    (tab.title.Values.Count > 0 ?
                    tab.title.Values.ElementAt(0) :
                    tab.name));
                LevelEvent levelEvent = new LevelEvent(0, (LevelEventType)tab.type);
                int num = 1;

                if (propertiesPanel == null)
                    goto IL_269;
                if (levelEvent == null)
                    goto IL_269;
                __instance.selectedEvent = levelEvent;
                __instance.selectedEventType = levelEvent.eventType;
                propertiesPanel.SetProperties(levelEvent, true);
                IEnumerator enumerator2 = __instance.tabs.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    RectTransform rect = (RectTransform)enumerator2.Current;
                    InspectorTab component = rect.gameObject.GetComponent<InspectorTab>();
                    if (!(component == null))
                    {
                        if (eventType == component.levelEventType)
                        {
                            component.SetSelected(true);
                            component.eventIndex = eventIndex;
                            if (component.cycleButtons != null)
                                component.cycleButtons.text.text = string.Format("{0}/{1}", eventIndex + 1, num);
                        }
                        else
                            component.SetSelected(false);
                    }
                }
                goto IL_269;
            IL_269:
                __instance.editor.changingState--;
                __instance.Set("showingPanel", false);
            }
        }

        [HarmonyPatch(typeof(PropertiesPanel), "Init")]
        public static class PropertyPanelPatch
        {
            public static void Postfix(PropertiesPanel __instance, InspectorPanel panel, LevelEventInfo levelEventInfo)
            {
                if (CustomTabManager.byType.TryGetValue((int)levelEventInfo.type, out CustomTabManager.CustomTab tab) && tab.page != null)
                {
                    __instance.inspectorPanel = panel;
                    VerticalLayoutGroup layoutGroup = __instance.content.GetComponent<VerticalLayoutGroup>();
                    ContentSizeFitter sizeFitter = layoutGroup.GetOrAddComponent<ContentSizeFitter>();
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = false;
                    __instance.content.gameObject.GetOrAddComponent<ScrollRect>().movementType = ScrollRect.MovementType.Unrestricted;
                    __instance.content.gameObject.AddComponent(tab.page).Set("properties", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(InspectorTab), "SetSelected")]
        public static class SetSelectedPatch
        {
            public static void Postfix(InspectorTab __instance, bool selected)
            {
                int type = (int)__instance.levelEventType;
                if (!CustomTabManager.byType.ContainsKey(type))
                    return;
                RectTransform rect = __instance.panel.panelsList.Find(panel => panel.inspectorPanel == __instance.panel).content;
                CustomTabBehaviour behaviour = __instance.panel.panelsList.Find(panel => panel.levelEventType == __instance.levelEventType)?.content?.GetComponent<CustomTabBehaviour>();
                if (selected)
                    behaviour?.OnFocused();
                else if (__instance.icon.color.a == 1)
                    behaviour?.OnUnFocused();
                if (!selected)
                    __instance.eventIndex = 0;
                __instance.cycleButtons.gameObject.SetActive(false);
                RectTransform component = __instance.GetComponent<RectTransform>();
                float num = 0f;
                Vector2 endValue = new Vector2(num, component.sizeDelta.y);
                component.DOKill(false);
                component.DOSizeDelta(endValue, 0.05f, false).SetUpdate(true);
                float num2 = selected ? 0f : 3f;
                num2 -= num / 2f;
                component.DOAnchorPosX(num2, 0.05f, false).SetUpdate(true);
                float alpha = selected ? 0.7f : 0.45f;
                ColorBlock colors = __instance.button.colors;
                colors.normalColor = Color.white.WithAlpha(alpha);
                __instance.button.colors = colors;
                __instance.icon.DOKill(false);
                float alpha2 = selected ? 1f : 0.6f;
                __instance.icon.DOColor(Color.white.WithAlpha(alpha2), 0.05f).SetUpdate(true);
            }
        }

        [HarmonyPatch(typeof(PropertyControl), "Setup")]
        public static class SetupPatch
        {
            public static void Postfix(PropertyControl __instance)
            {
                if (!(__instance is PropertyControl_Export instance))
                    return;
                if (!(__instance.propertyInfo.value_default is UnityAction action))
                    return;
                instance.exportButton.onClick.RemoveAllListeners();
                instance.exportButton.onClick.AddListener(action);
                if (instance.propertyInfo.customLocalizationKey == null)
                {
                    string str = "editor." + instance.propertyInfo.levelEventInfo.name + "." + instance.propertyInfo.name;
                    instance.buttonText.text = RDString.GetWithCheck(str, out bool flag, null);
                    if (!flag)
                        instance.buttonText.text = RDString.GetWithCheck("editor." + instance.propertyInfo.name, out _, null);
                }
                else
                    instance.buttonText.text = (instance.propertyInfo.customLocalizationKey == "") ? "" : RDString.Get(instance.propertyInfo.customLocalizationKey, null);
            }
        }

        [HarmonyPatch(typeof(Property), "info", MethodType.Setter)]
        public static class SetInfoPatch
        {
            public static void Postfix(Property __instance)
            {
                if (__instance.info.type == PropertyType.Export)
                    __instance.label.text = "";
            }
        }
    }
}
