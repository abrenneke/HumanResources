﻿using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HumanResources
{
    class WorkGiver_ResearchTech : WorkGiver_Knowledge
    {
		public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
			if (!base.ShouldSkip(pawn, forced))
			{
				IEnumerable<ResearchProjectDef> expertise = from x in pawn.TryGetComp<CompKnowledge>().expertise
															where x.Value >= 1f
															select x.Key;
				IEnumerable<ResearchProjectDef> available = DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(x => !x.IsFinished).Except(expertise);
				return !available.Any();
			}
			return true;


		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			//Log.Message(pawn + " is looking for a research job...");
			Building_WorkTable Desk = t as Building_WorkTable;
			if (Desk != null)
			{
				var relevantBills = RelevantBills(Desk, pawn);
				if (!CheckJobOnThing(pawn, t, forced) | relevantBills.EnumerableNullOrEmpty())
				{
					//Log.Message("...no job on desk.");
					return false;
				}
				List<ResearchProjectDef> studyMaterial = new List<ResearchProjectDef>();
				//Log.Message("...relevant bills: " + RelevantBills(Desk, pawn).Count);
				foreach (Bill bill in relevantBills)
				{
					//Log.Message("...checking recipe: " + bill.recipe+", on bill "+bill.GetType());
					//Log.Message("...selected techs count: " + bill.SelectedTech().ToList().Count());
					studyMaterial.AddRange(bill.SelectedTech().Where(x => !x.IsFinished && x.TechprintRequirementMet && (x.requiredResearchBuilding == null || (bool)AccessTools.Property(typeof(ResearchProjectDef),"PlayerHasAnyAppropriateResearchBench").GetValue(x))));
				}
				availableTechs = studyMaterial;
				//Log.Message("...studyMaterial count is " + studyMaterial.Count());
				CompKnowledge techComp = pawn.TryGetComp<CompKnowledge>();
				techComp.AssignHomework(studyMaterial);
				//Log.Message("...homework count is " + techComp.HomeWork.Count());
				//if (techComp.HomeWork.Count() > 0) return true;
				if (studyMaterial.Intersect(techComp.HomeWork).Any()) return true;
				if (!JobFailReason.HaveReason) JobFailReason.Is("AlreadyKnowsThoseProjects".Translate(pawn), null);
				return false;
			}
			//Log.Message("case 4");
			return false;

		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			IBillGiver billGiver = thing as IBillGiver;
			if (billGiver != null && ThingIsUsableBillGiver(thing) && billGiver.BillStack.AnyShouldDoNow && billGiver.UsableForBillsAfterFueling())
			{
				LocalTargetInfo target = thing;
				if (pawn.CanReserve(target, 1, -1, null, forced) && !thing.IsBurning() && !thing.IsForbidden(pawn))
				{
					billGiver.BillStack.RemoveIncompletableBills();
					foreach (Bill bill in RelevantBills(thing, pawn))
					{
						if (bill.ShouldDoNow() && bill.PawnAllowedToStartAnew(pawn) && bill.SelectedTech().Intersect(availableTechs).Any())
						{
							//Log.Message("probing bill: pawn allowed is " + bill.PawnAllowedToStartAnew(pawn) + " for " + pawn);
							return new Job(TechJobDefOf.ResearchTech, target)
							{
								bill = bill
							};
						}
					}
				}
			}
			return null;
		}
	}
}
