﻿using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using UnityEngine;
using Verse;

namespace HumanResources
{
    public static class ResearchTree_Patches
    {
        private static string ModName = "";
        private static NotImplementedException stubMsg = new NotImplementedException("ResearchTree reverse patch");

        public static Type ResearchNodeType() => AccessTools.TypeByName(ModName + ".ResearchNode");
        public static Type AssetsType() => AccessTools.TypeByName(ModName + ".Assets");
        public static Type TreeType() => AccessTools.TypeByName(ModName + ".Tree");
        public static Type NodeType() => AccessTools.TypeByName(ModName + ".Node");
        public static Type DummyNodeType() => AccessTools.TypeByName(ModName + ".DummyNode");
        public static Type MainTabType() => AccessTools.TypeByName(ModName + ".MainTabWindow_ResearchTree");
        public static Type ConstantsType() => AccessTools.TypeByName(ModName + ".Constants");

        public static void Execute(Harmony instance, string modName)
        {
            ModName = modName;

            //ResearchProjectDef_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetUnlockDefsAndDescs"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetUnlockDefsAndDescs)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetRecipesUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetRecipesUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetThingsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetThingsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:GetPlantsUnlocked"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(GetPlantsUnlocked)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(modName + ".ResearchProjectDef_Extensions:Ancestors"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Ancestors)))).Patch();

            //Node
            IsVisibleInfo = AccessTools.Method(NodeType(), "IsVisible");
            HighlightedInfo = AccessTools.Property(NodeType(), "Highlighted");
            RectInfo = AccessTools.Property(NodeType(), "Rect");
            largeLabelInfo = AccessTools.Field(NodeType(), "_largeLabel");
            LabelRectInfo = AccessTools.Property(NodeType(), "LabelRect");
            CostLabelRectInfo = AccessTools.Property(NodeType(), "CostLabelRect");
            CostIconRectInfo = AccessTools.Property(NodeType(), "CostIconRect");
            IconsRectInfo = AccessTools.Property(NodeType(), "IconsRect");

            //ResearchNode
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "BuildingPresent", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(BuildingPresent)))).Patch();
            instance.CreateReversePatcher(AccessTools.Method(ResearchNodeType(), "MissingFacilities", new Type[] { typeof(ResearchProjectDef) }),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(MissingFacilities)))).Patch();
            instance.Patch(AccessTools.Constructor(ResearchNodeType(), new Type[] { typeof(ResearchProjectDef) }),
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(ResearchNode_Postfix))));
            instance.Patch(AccessTools.Method(ResearchNodeType(), "Draw"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Draw_Prefix))));

            GetMissingRequiredRecursiveInfo = AccessTools.Method(ResearchNodeType(), "GetMissingRequiredRecursive");
            ChildrenInfo = AccessTools.Property(ResearchNodeType(), "Children");
            ColorInfo = AccessTools.Property(ResearchNodeType(), "Color");
            AvailableInfo = AccessTools.Property(ResearchNodeType(), "Available");
            CompletedInfo = AccessTools.Property(ResearchNodeType(), "Completed");
            ResearchInfo = AccessTools.Field(ResearchNodeType(), "Research");
            GetResearchTooltipStringInfo = AccessTools.Method(ResearchNodeType(), "GetResearchTooltipString");

            //Def_Extensions
            instance.CreateReversePatcher(AccessTools.Method(modName + ".Def_Extensions:DrawColouredIcon"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DrawColouredIcon)))).Patch();

            //MainTabWindow_ResearchTree
            instance.Patch(AccessTools.Method(MainTabType(), "DoWindowContents"),
                 null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(DoWindowContents_Postfix))));
            if (modName != "ResearchPal")
            {
                instance.Patch(AccessTools.Method(MainTabType(), "Notify_TreeInitialized"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }
            //fix for tree overlapping search bar on higher UI scales
            instance.Patch(AccessTools.Method(MainTabType(), "SetRects"), 
                null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(Set_Rects_Postfix))));

            InstanceInfo = AccessTools.Property(MainTabType(), "Instance");
            ZoomLevelInfo = AccessTools.Property(MainTabType(), "ZoomLevel");

            //Tree
            instance.Patch(AccessTools.Method(TreeType(), "PopulateNodes"),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Prefix))),
                new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(PopulateNodes_Postfix))));
            if (modName == "ResearchPal")
            {
                instance.Patch(AccessTools.Method(TreeType(), "Initialize"),
                    null, new HarmonyMethod(AccessTools.Method(typeof(ResearchTree_Patches), nameof(TreeInitialized_Postfix))));
            }

            //Constants
            EpsilonInfo = AccessTools.Field(ConstantsType(), "Epsilon");
            DetailedModeZoomLevelCutoffInfo = AccessTools.Field(ConstantsType(), "DetailedModeZoomLevelCutoff");
            MarginInfo = AccessTools.Field(ConstantsType(), "Margin");
            QueueLabelSizeInfo = AccessTools.Field(ConstantsType(), "QueueLabelSize");
            IconSizeInfo = AccessTools.Field(ConstantsType(), "IconSize");
            NodeMarginsInfo = AccessTools.Field(ConstantsType(), "NodeMargins");
            NodeSizeInfo = AccessTools.Field(ConstantsType(), "NodeSize");
            TopBarHeightInfo = AccessTools.Field(ConstantsType(), "TopBarHeight");
        }

        //Constants
        public static FieldInfo
            EpsilonInfo,
            DetailedModeZoomLevelCutoffInfo,
            MarginInfo,
            QueueLabelSizeInfo,
            IconSizeInfo,
            NodeMarginsInfo,
            NodeSizeInfo,
            TopBarHeightInfo;

        public static List<Pair<Def, string>> GetUnlockDefsAndDescs(ResearchProjectDef research, bool dedupe = true) { throw stubMsg; }
        public static bool BuildingPresent(ResearchProjectDef research) { throw stubMsg; }
        public static List<ThingDef> MissingFacilities(ResearchProjectDef research) { throw stubMsg; }
        public static void DrawColouredIcon(this Def def, Rect canvas) { throw stubMsg; }
        public static IEnumerable<RecipeDef> GetRecipesUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetThingsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static IEnumerable<ThingDef> GetPlantsUnlocked(this ResearchProjectDef research) { throw stubMsg; }
        public static List<ResearchProjectDef> Ancestors(this ResearchProjectDef research) { throw stubMsg; }

        public static ResearchProjectDef subjectToShow;
        private static MethodInfo MainTabCenterOnInfo => AccessTools.Method(MainTabType(), "CenterOn", new Type[] { NodeType() });
        private static PropertyInfo TreeNodesListInfo => AccessTools.Property(TreeType(), "Nodes");

        public static void DoWindowContents_Postfix(object __instance)
        {
            if (subjectToShow != null && treeReady)
            {
                int idx = treeNodesResearchCache.IndexOf(subjectToShow);
                MainTabCenterOnInfo.Invoke(__instance, new object[] { treeNodesList[idx] });
                subjectToShow = null;
            }
        }

        private static IList treeNodesList;

        private static bool treeReady = false;

        private static List<ResearchProjectDef> treeNodesResearchCache = new List<ResearchProjectDef>();

        public static void TreeInitialized_Postfix(object __instance)
        {
            treeNodesList = (IList)TreeNodesListInfo.GetValue(__instance);
            treeReady = !treeNodesResearchCache.NullOrEmpty();
        }

        private static bool populating = false;

        private static void PopulateNodes_Prefix()
        {
            populating = true;
        }

        private static void PopulateNodes_Postfix()
        {
            populating = false;
        }

        private static void ResearchNode_Postfix(ResearchProjectDef research)
        {
            if (populating)
            {
                treeNodesResearchCache.Add(research);
            }
        }

        private static void Set_Rects_Postfix(object __instance)
        {
            FieldInfo baseViewRectInfo = AccessTools.Field(MainTabType(), "_baseViewRect");
            baseViewRectInfo.SetValue(__instance, new Rect(
                Window.StandardMargin / Prefs.UIScale,
                (Constants.TopBarHeight + Constants.Margin + Window.StandardMargin),
                (Screen.width - Window.StandardMargin * 2f) / Prefs.UIScale,
                ((Screen.height - MainButtonDef.ButtonHeight - Window.StandardMargin * 3) / Prefs.UIScale) - Constants.TopBarHeight - Constants.Margin)
                );
        }

        //Node
        private static MethodInfo IsVisibleInfo;
        private static PropertyInfo
            HighlightedInfo,
            RectInfo,
            LabelRectInfo,
            CostLabelRectInfo,
            CostIconRectInfo,
            IconsRectInfo;
        private static FieldInfo largeLabelInfo;

        //Reserarch Node
        private static MethodInfo
            GetMissingRequiredRecursiveInfo,
            GetResearchTooltipStringInfo;
        private static PropertyInfo 
            ChildrenInfo,
            ColorInfo,
            AvailableInfo,
            CompletedInfo;
        private static FieldInfo ResearchInfo;

        //MainWindow
        private static PropertyInfo 
            InstanceInfo,
            ZoomLevelInfo;

        public static bool Draw_Prefix(object __instance, Rect visibleRect, bool forceDetailedMode = false)
        {
            //Reflected objects
            Rect rect = (Rect)RectInfo.GetValue(__instance);
            ResearchProjectDef Research = (ResearchProjectDef)ResearchInfo.GetValue(__instance);
            bool available = (bool)AvailableInfo.GetValue(__instance);
            bool completed = (bool)CompletedInfo.GetValue(__instance);
            //End of reflection info.

            if (!(bool)IsVisibleInfo.Invoke(__instance, new object[] { visibleRect }))
            {
                HighlightedInfo.SetValue(__instance, false);
                return false;
            }
            var detailedMode = forceDetailedMode || (float)ZoomLevelInfo.GetValue(InstanceInfo.GetValue(__instance)) < Constants.DetailedModeZoomLevelCutoff;
            var mouseOver = Mouse.IsOver(rect);

            if (Event.current.type == EventType.Repaint)
            {
                //researches that are completed or could be started immediately, and that have the required building(s) available
                GUI.color = mouseOver ? GenUI.MouseoverColor : (Color)ColorInfo.GetValue(__instance);
                if (mouseOver || (bool)HighlightedInfo.GetValue(__instance))
                    GUI.DrawTexture(rect, ResearchTree_Assets.ButtonActive);
                else
                    GUI.DrawTexture(rect, ResearchTree_Assets.Button);

                //grey out center to create a progress bar effect, completely greying out research not started.
                if (available)
                {
                    var progressBarRect = rect.ContractedBy(3f);
                    GUI.color = ResearchTree_Assets.ColorAvailable[Research.techLevel];
                    progressBarRect.xMin += Research.ProgressPercent * progressBarRect.width;
                    GUI.DrawTexture(progressBarRect, BaseContent.WhiteTex);
                }

                HighlightedInfo.SetValue(__instance, false);

                //draw the research label
                if (!completed && !available)
                    GUI.color = Color.grey;
                else
                    GUI.color = Color.white;

                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.WordWrap = false;
                    Text.Font = (bool)largeLabelInfo.GetValue(__instance) ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label((Rect)LabelRectInfo.GetValue(__instance), Research.LabelCap);
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.WordWrap = false;
                    Text.Font = GameFont.Medium;
                    Widgets.Label(rect, Research.LabelCap);
                }

                //draw research cost and icon
                if (detailedMode)
                {
                    Text.Anchor = TextAnchor.UpperRight;
                    Text.Font = Research.CostApparent > 1000000 ? GameFont.Tiny : GameFont.Small;
                    Widgets.Label((Rect)CostLabelRectInfo.GetValue(__instance), Research.CostApparent.ToStringByStyle(ToStringStyle.Integer));
                    GUI.DrawTexture((Rect)CostIconRectInfo.GetValue(__instance), !completed && !available ? ResearchTree_Assets.Lock : ResearchTree_Assets.ResearchIcon,
                                        ScaleMode.ScaleToFit);
                }

                Text.WordWrap = true;

                //attach description and further info to a tooltip
                string root = HarmonyPatches.ResearchPal ? "ResearchPal" : "Fluffy.ResearchTree";
                TooltipHandler.TipRegion(rect, new Func<string>(() => (string)GetResearchTooltipStringInfo.Invoke(__instance, new object[] { })), Research.GetHashCode());
                if (!BuildingPresent(Research))
                {
                    string languageKey = root + ".MissingFacilities";
                    TooltipHandler.TipRegion(rect, languageKey.Translate(string.Join(", ", MissingFacilities(Research).Select(td => td.LabelCap).ToArray())));
                }
                else if (!Research.TechprintRequirementMet)
                    TooltipHandler.TipRegion(rect, root + ".MissingTechprints".Translate(Research.TechprintsApplied, Research.techprintCount));

                //draw unlock icons
                if (detailedMode)
                {
                    Rect IconsRect = (Rect)IconsRectInfo.GetValue(__instance);
                    var unlocks = ResearchTree_Patches.GetUnlockDefsAndDescs(Research);
                    for (var i = 0; i < unlocks.Count; i++)
                    {
                        var iconRect = new Rect(
                            IconsRect.xMax - (i + 1) * (Constants.IconSize.x + 4f),
                            IconsRect.yMin + (IconsRect.height - Constants.IconSize.y) / 2f,
                            Constants.IconSize.x,
                            Constants.IconSize.y);

                        if (iconRect.xMin - Constants.IconSize.x < IconsRect.xMin &&
                                i + 1 < unlocks.Count)
                        {
                            //stop the loop if we're about to overflow and have 2 or more unlocks yet to print.
                            iconRect.x = IconsRect.x + 4f;
                            GUI.DrawTexture(iconRect, ResearchTree_Assets.MoreIcon, ScaleMode.ScaleToFit);
                            var tip = string.Join("\n", unlocks.GetRange(i, unlocks.Count - i).Select(p => p.Second).ToArray());
                            TooltipHandler.TipRegion(iconRect, tip);
                            //new TipSignal(tip, Settings.TipID, TooltipPriority.Pawn) );
                            break;
                        }

                        //draw icon
                        unlocks[i].First.DrawColouredIcon(iconRect);

                        //tooltip
                        TooltipHandler.TipRegion(iconRect, unlocks[i].Second);
                    }
                }

                if (mouseOver)
                    //highlight prerequisites if research available
                    if (available)
                    {
                        HighlightedInfo.SetValue(__instance, true);
                        foreach (var prerequisite in (IEnumerable<object>)GetMissingRequiredRecursiveInfo.Invoke(__instance, new object[] { }))
                            HighlightedInfo.SetValue(Convert.ChangeType(prerequisite, ResearchNodeType()), true);
                    }
                    //highlight children if completed
                    else if (completed)
                    {
                        foreach (var child in (IEnumerable<object>)ChildrenInfo.GetValue(__instance))
                            HighlightedInfo.SetValue(Convert.ChangeType(child, ResearchNodeType()), true);
                    }
            }

            Research.DrawAssignments(rect);

            //if clicked and not yet finished, queue up this research and all prereqs.
            if (Widgets.ButtonInvisible(rect))
            {
                //LMB is queue operations, RMB is info
                if (Event.current.button == 0) Research.SelectMenu(completed);
                if (DebugSettings.godMode && Prefs.DevMode && Event.current.button == 1 && !Research.IsFinished)
                {
                    Find.ResearchManager.FinishProject(Research);
                }
            }
            return false;
        }

    }
}


