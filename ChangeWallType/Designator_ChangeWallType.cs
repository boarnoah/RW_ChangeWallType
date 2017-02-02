using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ChangeWallType {
	public class Designator_ChangeWallType : Designator_SelectableThings {
		public Designator_ChangeWallType(ThingDesignatorDef def) : base(def) {}

		private ThingDef _newStuff;

		protected override bool ThingIsRelevant(Thing item) {
			var comp = (item as ThingWithComps)?.GetComp<CompForbiddable>();
			//TODO: Only highlight player faction's blueprints
			return comp != null && (comp.parent.def.IsBlueprint || comp.parent.def.isFrame);
		}

		bool CanUseStuff(ThingDef newMaterial, ThingDef item) {
			List<StuffCategoryDef> newStuffCat = newMaterial.stuffProps.categories;
			List<StuffCategoryDef> itemStuffCat = new List<StuffCategoryDef>();
			bool canUse = false;

			if (item.IsBlueprint) {
				//Get item def name from blueprint def ("Wall_Blueprint" -> "Wall")
				string thingDefName = item.defName.Split('_')[0];
				itemStuffCat = ThingDef.Named(thingDefName).stuffCategories;
			} else if (item.isFrame) {
				itemStuffCat = item.stuffCategories;
			}

			//Skips if item's has no buildable material list (ex: Power Generator)
			if (itemStuffCat != null)
				canUse = newStuffCat.Intersect(itemStuffCat).Any();

			//TODO: Look into stuffProps.canMake (used in Designator_Build float menu construction).
			return canUse;
		}

		protected override int ProcessCell(IntVec3 c) {
			var hitCount = 0;
			var cellThings = Find.VisibleMap.thingGrid.ThingsListAtFast(c);
	
			for(int i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (thing.def.selectable && (thing.Faction == Faction.OfPlayer) && _newStuff != null) {
					if (CanUseStuff(_newStuff, thing.def)) {
						if (thing.def.IsBlueprint) {
							Blueprint_Build replaceBluePrint = (Blueprint_Build) ThingMaker.MakeThing(ThingDef.Named(thing.def.defName));
							replaceBluePrint.SetFactionDirect(Faction.OfPlayer);
							replaceBluePrint.stuffToUse = _newStuff;
							GenSpawn.Spawn(replaceBluePrint, thing.Position, thing.Map, thing.Rotation);
							thing.Destroy(DestroyMode.Cancel);
						} else if (thing.def.isFrame) {
							Frame replaceFrame = (Frame) ThingMaker.MakeThing(ThingDef.Named(thing.def.defName), _newStuff);
							replaceFrame.SetFactionDirect(Faction.OfPlayer);
							IntVec3 pos = thing.Position;
							Map map = thing.Map;
							Rot4 rot = thing.Rotation;
							//Destroys Frame's inner resourceContainer to reclaim resources
							//Needs to be done before spawning new frame at loc (else resourceContainer goes MIA)
							thing.Destroy(DestroyMode.Cancel);
							GenSpawn.Spawn(replaceFrame, pos, map, rot);
						}
						hitCount++;
					}
				}
			}
			return hitCount;
		}

		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);

			//Right click Stuff Menu
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			using (
				Dictionary<ThingDef, int>.KeyCollection.Enumerator enumerator =
					Find.VisibleMap.resourceCounter.AllCountedAmounts.Keys.GetEnumerator()) {
				bool showUnstocked = ChangeWallTypeController.Instance.ShowUnstockedMaterials;
				while (enumerator.MoveNext()) {
					ThingDef current = enumerator.Current;
					//TODO: Better check to identify "buildable" materials
					if (current != null && (current.IsStuff && current.stuffProps.CanMake(ThingDef.Named("Wall")))) {
						bool includeItem = true;

						if (!showUnstocked)
							includeItem = Find.VisibleMap.resourceCounter.GetCount(current) > 0;

						if (includeItem) {
							options.Add(new FloatMenuOption(current.LabelCap, () => { _newStuff = current; }) {tutorTag = current.defName});
						}
					}
				}
			}

			if (options.Count == 0) {
				//TODO: Change to no materials found (triggers when nothing in stock)
				Messages.Message("No materials found to designate with (is Rimworld Core loaded?)", MessageSound.RejectInput);
			} else {
				Find.WindowStack.Add(new FloatMenu(options) {vanishIfMouseDistant = true});
			}
		}
	}
}