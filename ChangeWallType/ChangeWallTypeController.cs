/**
From https://github.com/UnlimitedHugs/RimworldAllowTool
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace ChangeWallType {
	/**
	 * The hub of the mod.
	 * Injects the custom designators and handles hotkey presses.
	 */

	public class ChangeWallTypeController : ModBase {
		private static FieldInfo _resolvedDesignatorsField;
		public static ChangeWallTypeController Instance { get; private set; }

		private readonly List<DesignatorEntry> _activeDesignators = new List<DesignatorEntry>();

		private SettingHandle<bool> _settingGlobalHotkeys;
		private SettingHandle<bool> _settingShowUnstockedMaterials;

		public override string ModIdentifier => "ChangeWallType";

		public bool ShowUnstockedMaterials => _settingShowUnstockedMaterials;

		internal new ModLogger Logger => base.Logger;

		public UnlimitedDesignationDragger Dragger { get; private set; }

		private ChangeWallTypeController() {
			Instance = this;
		}

		public override void Initialize() {
			Dragger = new UnlimitedDesignationDragger();
			InitReflectionFields();
		}

		public override void Update() {
			Dragger.Update();
		}

		public override void OnGUI() {
			if (Current.Game == null || Current.Game.VisibleMap == null)
				return;
			var selectedDesignator = Find.MapUI.designatorManager.SelectedDesignator;
			foreach (DesignatorEntry t in _activeDesignators) {
				var designator = t.Designator;
				if (selectedDesignator != designator)
					continue;
				designator.SelectedOnGUI();
			}
			if (Event.current.type == EventType.KeyDown) {
				CheckForHotkeyPresses();
			}
		}

		public override void DefsLoaded() {
			LongEventHandler.ExecuteWhenFinished(InjectDesignators);
			// DesignationCategoryDef has delayed designator resolution, so we do, too
			PrepareSettingsHandles();
		}

		public override void SettingsChanged() {
			foreach (var entry in _activeDesignators) {
				entry.Designator.SetVisible(entry.VisibilitySetting.Value);
			}
		}

		private void InjectDesignators() {
			_activeDesignators.Clear();
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				if (designatorDef.Injected)
					continue;
				var resolvedDesignators = (List<Designator>) _resolvedDesignatorsField.GetValue(designatorDef.Category);
				var insertIndex = -1;
				for (var i = 0; i < resolvedDesignators.Count; i++) {
					if (resolvedDesignators[i].GetType() != designatorDef.insertAfter)
						continue;
					insertIndex = i;
					break;
				}
				if (insertIndex >= 0) {
					var designator =
						(Designator_SelectableThings) Activator.CreateInstance(designatorDef.designatorClass, designatorDef);
					resolvedDesignators.Insert(insertIndex + 1, designator);
					var handle = Settings.GetHandle("show" + designatorDef.defName,
						"setting_showTool_label".Translate(designatorDef.label), null, true);
					designator.SetVisible(handle.Value);
					_activeDesignators.Add(new DesignatorEntry(designator, designatorDef.hotkeyDef, handle));
					numDesignatorsInjected++;
				} else {
					Logger.Error($"Failed to inject {designatorDef.defName} after {designatorDef.insertAfter.Name}");
				}
				designatorDef.Injected = true;
			}
			if (numDesignatorsInjected > 0) {
				Logger.Trace("Injected " + numDesignatorsInjected + " designators");
			}
		}

		private void InitReflectionFields() {
			_resolvedDesignatorsField = typeof(DesignationCategoryDef).GetField("resolvedDesignators",
				BindingFlags.NonPublic | BindingFlags.Instance);
			if (_resolvedDesignatorsField == null)
				Logger.Error("failed to reflect DesignationCategoryDef.resolvedDesignators");
		}

		private void PrepareSettingsHandles() {
			_settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(),
				"setting_globalHotkeys_desc".Translate(), true);
			_settingShowUnstockedMaterials = Settings.GetHandle("showUnstockedMaterials", "Include unstocked materials",
				"Show materials with 0 stocked", true);
		}

		private void CheckForHotkeyPresses() {
			if (!_settingGlobalHotkeys || Find.VisibleMap == null)
				return;
			foreach (DesignatorEntry entry in _activeDesignators) {
				if (entry.Key == null || !entry.Key.JustPressed || !entry.VisibilitySetting.Value)
					continue;
				entry.Designator.ProcessInput(Event.current);
				break;
			}
		}

		private class DesignatorEntry {
			public readonly Designator_SelectableThings Designator;
			public readonly KeyBindingDef Key;
			public readonly SettingHandle<bool> VisibilitySetting;

			public DesignatorEntry(Designator_SelectableThings designator, KeyBindingDef key,
				SettingHandle<bool> visibilitySetting) {
				Designator = designator;
				Key = key;
				VisibilitySetting = visibilitySetting;
			}
		}
	}
}