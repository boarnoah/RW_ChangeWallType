using RimWorld;
using Verse;

namespace ChangeWallType {
	public class Designator_ChangeWallType : Designator_SelectableThings {
		public Designator_ChangeWallType(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			var comp = item is ThingWithComps ? (item as ThingWithComps).GetComp<CompForbiddable>() : null;
			//TODO: Only highlight player faction's blueprints
			return comp != null && (comp.parent.def.IsBlueprint || comp.parent.def.isFrame);
		}

		override protected int ProcessCell(IntVec3 c) {
			var hitCount = 0;
			var cellThings = Find.ThingGrid.ThingsListAtFast(c);
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (thing.def.selectable && (thing.Faction == Faction.OfPlayer)) {
					if (thing.def.IsBlueprint) {
						Blueprint_Build replaceBluePrint = (Blueprint_Build) ThingMaker.MakeThing(ThingDef.Named(thing.def.defName), null);
						replaceBluePrint.SetFactionDirect(Faction.OfPlayer);
						//TODO: Configureable substance
						//TODO: Check if this bp can be made from new mat(?) use stuffCategories maybe
                        replaceBluePrint.stuffToUse = ThingDef.Named("BlocksSandstone");
						GenSpawn.Spawn(replaceBluePrint, thing.Position, thing.Rotation);
						thing.Destroy(DestroyMode.Cancel);
					} else if(thing.def.isFrame){
						//TODO: This
					}

					hitCount++;
				}
			}
			return hitCount;
		}
	}
}