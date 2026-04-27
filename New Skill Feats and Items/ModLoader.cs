using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display;
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
        SkillFeats.AssuranceFeats();
        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            SkillFeats.StaticAssuranceFeats.Sort((feat, feat1) => string.Compare(feat.Name, feat1.Name, StringComparison.Ordinal));
            // for (var i = 0; i < SkillFeats.StaticAssuranceFeats.Count; i++)
            // {
            //     SkillFeats.StaticAssuranceFeats[i].WithZOrder(i);
            // }
            SkillFeats.Assurance?.Subfeats = SkillFeats.StaticAssuranceFeats;
            foreach (Feat feat in SkillFeats.Assurance?.Subfeats!)
            {
                ModManager.AddFeat(feat);
                BackgroundSelectionFeat devoted = (BackgroundSelectionFeat)new BackgroundSelectionFeat(
                        ModManager.RegisterFeatName((feat.Tag is Skill featTag ? featTag : Skill.Acrobatics)
                            .HumanizeTitleCase2() + " Focus"), "You spent long hours mastering "+(feat.Tag is Skill skill ? skill : Skill.Acrobatics)
                        .HumanizeTitleCase2() + " and now you can almost do it in your sleep.", $"You're trained in {{b}}{(feat.Tag is Skill tag ? tag : Skill.Acrobatics)
                            .HumanizeTitleCase2()}{{/b}}. You gain the {{b}}Assurance{{/b}} skill feat for {(feat.Tag is Skill tag1 ? tag1 : Skill.Acrobatics)
                            .HumanizeTitleCase2()}.",
                        [
                            new LimitedAbilityBoost(Ability.Constitution,
                                (feat.Tag is Skill skill1 ? skill1 : Skill.Acrobatics).ToAbility()),
                            new FreeAbilityBoost()
                        ]
                    )
                    .WithOnSheet(sheet =>
                        {
                            sheet.GrantFeat(ModData.FeatNames.Assurance, feat.FeatName);
                            sheet.TrainInThisOrSubstitute((Skill)(feat.Tag ?? Skill.Acrobatics));
                        }
                    );
                ModManager.AddFeat(devoted);
            }
        };
    }

    public static Feat RegisterNewAssurance(Skill skill, Action<Feat>? adjustSubfeat = null)
    {
        Feat assuranceFeat = SkillFeats.AssuranceCreator(skill);
        adjustSubfeat?.Invoke(assuranceFeat);
        // ModManager.AddFeat(assuranceFeat);
        // SkillFeats.Assurance?.Subfeats?.Add(assuranceFeat);
        SkillFeats.StaticAssuranceFeats.Add(assuranceFeat);
        return assuranceFeat;
    }
}