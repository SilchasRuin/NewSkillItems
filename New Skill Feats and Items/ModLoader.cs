using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace New_Skill_Feats_and_Items;

public class ModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        SkillItems.AddItems();
        foreach (Feat feat in SkillFeats.CreateSkillFeats())
        {
            ModManager.AddFeat(feat);
        }
        ModManager.RegisterActionOnEachCreature(cr =>
        {
            if (cr.HasFeat(ModData.FeatNames.Assurance))
                SkillFeats.CreateAssuranceToggle(cr);
        });
        ModManager.RegisterBooleanSettingsOption("AssuranceThreshold", "Skill Feats - Assurance Threshold", "Enabling this option changes the functionality of assurance, instead of deciding to use assurance or not, assurance will be automatically applied if it would be beneficial and not applied otherwise. This makes assurance less fiddly and more powerful." +
            "\n{b}NOTE{/b}: You must reload to remove selection options associated with assurance's default behavior.", false);
        // LoadOrder.WhenFeatsBecomeLoaded += () =>
        // {
        //     foreach (Feat feat in SkillFeats.AssuranceFeats().Where(ft =>
        //                  SkillFeats.Assurance?.Subfeats != null && !SkillFeats.Assurance.Subfeats.Contains(ft)))
        //     {
        //         SkillFeats.Assurance?.Subfeats?.Add(feat);
        //     }
        // };
    }

    public static Feat RegisterNewAssurance(Skill skill, Action<Feat>? adjustSubfeat = null)
    {
        Feat assuranceFeat = SkillFeats.AssuranceCreator(skill);
        adjustSubfeat?.Invoke(assuranceFeat);
        ModManager.AddFeat(assuranceFeat);
        SkillFeats.Assurance?.Subfeats?.Add(assuranceFeat);
        return assuranceFeat;
    }
}