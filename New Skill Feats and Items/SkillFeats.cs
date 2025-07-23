using System.Runtime.CompilerServices;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Campaign.Path;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Library;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.IO;
using Dawnsbury.Modding;

namespace New_Skill_Feats_and_Items;

public abstract class SkillFeats
{
    public static IEnumerable<Feat> CreateSkillFeats()
    {
        //skill feats
        Feat virtuoso = new TrueFeat(ModData.FeatNames.Virtuoso, 1,
            "You have exceptional talent in performing.",
            "You have a +1 circumstance bonus to Performance. If you are a master in Performance, this bonus increases to +2.",
            [Trait.General, Trait.Skill]);
        AddVirtuosoLogic(virtuoso);
        yield return virtuoso;
        Feat intimidatingProwess = new TrueFeat(ModManager.RegisterFeatName("Intimidating Prowess"), 2,
            "Your physical might makes you much more imposing.",
            "You gain a +1 circumstance bonus to your Intimidation checks. Also, you don't take the -4 penalty when you demoralize a creature that doesn't understand your language, and your Demoralize action loses the auditory trait. If your Strength is +5 or higher and you are a master in Intimidation, this bonus increases to +2.",
            [Trait.General, Trait.Skill]);
            AddIntimidatingProwessLogic(intimidatingProwess);
        yield return intimidatingProwess;
        Feat tumblingTeamwork = new TrueFeat(ModManager.RegisterFeatName("Tumbling Teamwork"), 2,
            "Your tumbling distracts a foe enough to create an advantage for one of your allies.",
            "When you successfully Tumble Through an enemy's space, an ally who is adjacent to that enemy can Step as a reaction, but they must remain adjacent to that enemy.",
            [Trait.General, Trait.Skill]);
        AddTumblingTeamworkLogic(tumblingTeamwork);
        yield return tumblingTeamwork;
        Feat rootMagic = new TrueFeat(ModManager.RegisterFeatName("Root Magic"), 1,
            "Your talismans ward against foul magic.",
            "During your daily preparations, you can assemble a small pouch with bits of herbs, hair, sacred oils, and other ritual ingredients, which you give to one ally other than yourself." +
            "\n\nThe first time that day the ally attempts a saving throw against a spell or haunt, they gain a +1 circumstance bonus to the roll. This bonus increases to +2 if you're an expert in Occultism or +3 if you're legendary.",
            [Trait.General, Trait.Skill]);
        CreateRootMagicLogic(rootMagic);
        yield return rootMagic;
        //root magic selections
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            Feat rootChoice = new (ModManager.RegisterFeatName(ModData.FeatNames.RootMagicFeat.ToString() + (i + 1),
                    "Player Character " + (i + 1)), null, "", [], null);
            CreateRootChoiceLogic(rootChoice, index);
            yield return rootChoice;
        }
        //skill feats continued
        Feat dirtyTrick = new TrueFeat(ModData.FeatNames.DirtyTrick, 1,
            "You hook a foe's bootlaces together, pull their hat over their eyes, loosen their belt, or otherwise confound their mobility through an underhanded tactic.",
            "{b}Requirements{/b} You have a hand free and are within melee reach of an opponent." +
            "\nAttempt a Thievery check against the target's Reflex DC." +
            S.FourDegreesOfSuccess("The target is clumsy 1 until they use an Interact action to end the impediment", "As critical success, but the condition ends automatically after 1 round.", null, "You fall prone as your attempt backfires."),
            [Trait.General, Trait.Skill]).WithActionCost(1);
        CreateDirtyTrickLogic(dirtyTrick);
        yield return dirtyTrick;
        string description;
        if (!PlayerProfile.Instance.IsBooleanOptionEnabled("AssuranceThreshold"))
        {
            description =
                "Choose a skill you’re trained in. You can forgo rolling a skill check for that skill to instead receive a result of 10 + your proficiency bonus (do not apply any other bonuses, penalties, or modifiers)." +
                "\n{b}Special{/b} You can select this feat multiple times. Each time, choose a different skill and gain the benefits for that skill.";
        }
        else
        {
            description =
                "Choose a skill you’re trained in. The minimum result you can receive on a skill check is a result of 10 + your proficiency bonus (do not apply any other bonuses, penalties, or modifiers)." +
                "\n{b}Special{/b} You can select this feat multiple times. Each time, choose a different skill and gain the benefits for that skill.";
        }
        Feat assurance = new TrueFeat(ModData.FeatNames.Assurance, 1,
            "Even in the worst circumstances, you can perform basic tasks.",
            description, [Trait.General, Trait.Skill], AssuranceFeats());
        assurance.CanSelectMultipleTimes = true;
        if (!PlayerProfile.Instance.IsBooleanOptionEnabled("AssuranceThreshold"))
        {
            assurance.WithOnSheet(values =>
            {
                SelectionOption setup = new SingleFeatSelectionOption("AssuranceStart",
                        "Default Assurance Setting",
                        SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL, ft => ft.Tag is "Assurance Settings")
                    .WithIsOptional();
                values.AtEndOfRecalculation += sheetValues =>
                {
                    sheetValues.AddSelectionOption(setup);
                };
            });
        }
        else
        {
            assurance.WithOnSheet(values => values.GrantFeat(ModData.FeatNames.AssuranceThreshold));
        }
        yield return assurance;
        //assurance combat prep feats
        Feat assuranceOn = new Feat(ModData.FeatNames.AssuranceOn, null,
            "Assurance is applied to all relevant skill checks. You can change this setting with a free action under other maneuvers.",
            [], null).WithTag("Assurance Settings").WithPrerequisite(values => !values.HasFeat(ModData.FeatNames.AssuranceOff) &&  !values.HasFeat(ModData.FeatNames.AssuranceAsk) &&  !values.HasFeat(ModData.FeatNames.AssuranceThreshold), "You must pick only 1 assurance setting.");
        yield return assuranceOn;
        Feat assuranceOff = new Feat(ModData.FeatNames.AssuranceOff, null,
            "Assurance is not applied. You can change this setting with a free action under other maneuvers.",
            [], null).WithTag("Assurance Settings").WithPrerequisite(values => !values.HasFeat(ModData.FeatNames.AssuranceOn) &&  !values.HasFeat(ModData.FeatNames.AssuranceAsk) &&  !values.HasFeat(ModData.FeatNames.AssuranceThreshold), "You must pick only 1 assurance setting.");
        yield return assuranceOff;
        Feat assuranceAsk = new Feat(ModData.FeatNames.AssuranceAsk, null,
            "Before each relevant skill check you will be asked to apply assurance. You can change this setting with a free action under other maneuvers.",
            [], null).WithTag("Assurance Settings").WithPrerequisite(values => !values.HasFeat(ModData.FeatNames.AssuranceOff) &&  !values.HasFeat(ModData.FeatNames.AssuranceOn) &&  !values.HasFeat(ModData.FeatNames.AssuranceThreshold), "You must pick only 1 assurance setting.");
        yield return assuranceAsk;
        Feat assuranceThreshold = new Feat(ModData.FeatNames.AssuranceThreshold, null,
            "Assurance will be applied to all relevant skill checks but only if it would be helpful.", [], null);
        yield return assuranceThreshold;
        //skill feats continued
        Feat distractingPerformance = new TrueFeat(ModManager.RegisterFeatName("Distracting Performance"), 2,
            "Your performances are especially distracting, allowing your allies to Sneak away with ease.",
            "You Create a Diversion, except you roll a Performance check instead of Deception, and the benefits of successful checks apply to an ally of your choice instead of you. The effects of a success last until the end of that ally's turn, and can end early based on the ally's actions.",
            [Trait.Skill, Trait.General]).WithActionCost(1);
        CreateDistractingPerformance(distractingPerformance);
        yield return distractingPerformance;
        Feat tumblingTrick = new TrueFeat(ModManager.RegisterFeatName("Tumbling Trick"), 7,
            "You use underhanded tactics to confound an enemy's movement as you tumble past.",
            "When you successfully Tumble Through an enemy's space, you can attempt a Dirty Trick on that enemy as a reaction. If you critically succeeded at your check to Tumble Through, you gain a +1 circumstance bonus to your Thievery check for Dirty Trick. When used in this way, Dirty Trick does not increase your multiple attack penalty.",
            [Trait.Skill, Trait.General, Trait.Homebrew]).WithActionCost(-2);
        CreateTumblingTrickLogic(tumblingTrick);
        yield return tumblingTrick;
        Feat battleCry = new TrueFeat(ModManager.RegisterFeatName("Battle Cry"), 7,
            "RAAAAH! Your ferocious shout as you rush into the fray strikes terror in your enemies' hearts.",
            "At the start of combat, you can yell a mighty battle cry and Demoralize an observed foe as a free action. If you're legendary in Intimidation, you can use a reaction to Demoralize your foe when you critically succeed at an attack roll.",
            [Trait.General, Trait.Skill]);
        CreateBattleCryLogic(battleCry);
        yield return battleCry;
        Feat terrifyingResistance = new TrueFeat(ModManager.RegisterFeatName("Terrifying Resistance"), 2,
            "The spells of those you have Demoralized are less effective on you.",
            "If you succeed in Demoralizing a creature, for the next 24 hours you gain a +1 circumstance bonus to saving throws against that creature's spells.",
            [Trait.Skill, Trait.General]);
        CreateTerrifyingResistanceLogic(terrifyingResistance);
        yield return terrifyingResistance;
        Feat evangelize = new TrueFeat(ModManager.RegisterFeatName("Evangelize"), 7,
            "You point out a detail that incontrovertibly supports your faith, causing a listener's mind to whirl.",
            $"Attempt a Diplomacy check and compare the result to the Will DC of a single target that can hear you and understands your language; that target is then temporarily immune to Evangelize from you for the rest of the combat." +
            $"{S.FourDegreesOfSuccessReverse(null!, null!, "The target is stupefied 1 for 1 round.", "The target is stupefied 2 for 1 round.")}",
            [Trait.Skill, Trait.General, Trait.Homebrew]);
        CreateEvangelizeLogic(evangelize);
        yield return evangelize;
        //Waiting on changes to in-line rolls
        /*Feat talentEnvy = new TrueFeat(ModData.FeatNames.TalentEnvy, 7,
            "You give off a bedazzling glow with every performance, sparking feelings of severe envy and inadequacy in those who compare their talent to yours.",
            "Once per minute when you succeed at a Performance check, you can attempt to Demoralize an onlooker of your choice within 60 feet as a free action. If you critically succeeded on your Performance check, improve the degree of success for your Demoralize check by one step.",
            [Trait.Skill, Trait.General]);
        CreateTalentEnvyLogic(talentEnvy);
        yield return talentEnvy;*/
        //background feats
        BackgroundSelectionFeat performer = (BackgroundSelectionFeat)new BackgroundSelectionFeat(ModManager.RegisterFeatName("Virtuoso"), "You are a prodigy at a kind of performance. " +
                "Those around you growing up were certain you would become a talented performance artist, but something drew you into adventure instead.",
                "You're trained in {b}Performance{/b}. You gain the {b}Virtuosic Performer{/b} skill feat.",
                [new LimitedAbilityBoost(Ability.Dexterity, Ability.Charisma), new FreeAbilityBoost()])
            .WithOnSheet(sheet =>
                {
                    sheet.TrainInThisOrSubstitute(Skill.Performance);
                    sheet.GrantFeat(virtuoso.FeatName);
                }
            );
        yield return performer;
        BackgroundSelectionFeat trickster = (BackgroundSelectionFeat)new BackgroundSelectionFeat(
                ModManager.RegisterFeatName("Trickster"),
                "You enjoyed playing pranks on your fellows as a child, and, truth be told, you never grew up. As an adventurer you play pranks upon monsters instead.",
                "You're trained in {b}Thievery{/b}. You gain the {b}Dirty Trick{/b} skill feat.",
                [new LimitedAbilityBoost(Ability.Dexterity, Ability.Intelligence), new FreeAbilityBoost()])
            .WithOnSheet(sheet =>
                {
                    sheet.TrainInThisOrSubstitute(Skill.Thievery);
                    sheet.GrantFeat(dirtyTrick.FeatName);
                }
            );
        yield return trickster;
        foreach (Feat feat in assurance.Subfeats!)
        {
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
                     sheet.GrantFeat(assurance.FeatName, feat.FeatName);
                     sheet.TrainInThisOrSubstitute((Skill)(feat.Tag ?? Skill.Acrobatics));
                    }
                );
            yield return devoted;
        }
        BackgroundSelectionFeat warrior = (BackgroundSelectionFeat)new BackgroundSelectionFeat(
                ModManager.RegisterFeatName("Warrior{b}{/b}"),
                "In your younger days, you waded into battle as a mercenary, a warrior defending a nomadic people, or a member of a militia or army. You might have wanted to break out from the regimented structure of these forces, or you could have always been as independent a warrior as you are now.",
                "You're trained in {b}Intimidation{/b}. You gain the {b}Intimidating Glare{/b} skill feat.",
                [new LimitedAbilityBoost(Ability.Strength, Ability.Constitution), new FreeAbilityBoost()]
            )
            .WithOnSheet(sheet =>
                {
                    sheet.TrainInThisOrSubstitute(Skill.Intimidation);
                    sheet.GrantFeat(FeatName.IntimidatingGlare);
                }
            );
        yield return warrior;
        if (ModManager.TryParse("BonMot", out FeatName bonMot))
        {
            BackgroundSelectionFeat runawayNoble = (BackgroundSelectionFeat)new BackgroundSelectionFeat(
                ModManager.RegisterFeatName("Runaway Noble"),
                "There are many reasons for noble blood to abandon their responsibilities. Whether you fled for safety, for love, to sate a spontaneous spark of rebellion, or to escape unbearable expectations, you’ve left your lavish life behind for one of newfound experiences. However, how prepared you are for a life on the road is something else entirely.",
                "You're trained in {b}Diplomacy{/b}. You gain the {b}Bon Mot{/b} skill feat.",
                [new LimitedAbilityBoost(Ability.Charisma, Ability.Intelligence), new FreeAbilityBoost()]
                )
                .WithOnSheet(sheet =>
                    {
                        sheet.TrainInThisOrSubstitute(Skill.Diplomacy);
                        sheet.GrantFeat(bonMot);
                    }
                );
            yield return runawayNoble;
        }
        BackgroundSelectionFeat rootWorker = (BackgroundSelectionFeat)new BackgroundSelectionFeat(
                ModManager.RegisterFeatName("Root Worker"),
                "Some ailments can't be cured by herbs alone. You learned ritual remedies as well, calling on nature spirits to soothe aches and ward off the evil eye. Taking up with adventurers has given you company on the road, as well as protection from those who would brand you a fake—or worse.",
                "You're trained in {b}Occultism{/b}. You gain the {b}Root Magic{/b} skill feat.",
                [new LimitedAbilityBoost(Ability.Intelligence, Ability.Wisdom), new FreeAbilityBoost()]
            )
            .WithOnSheet(sheet =>
                {
                    sheet.TrainInThisOrSubstitute(Skill.Occultism);
                    sheet.GrantFeat(rootMagic.FeatName);
                }
            );
        yield return rootWorker;
        //selection option feats for scoundrel's mask
        yield return new Feat(ModManager.RegisterFeatName("Off"), null,"The default behavior for Scoundrel's Mask is off; it will not apply the damage from the item unless it is toggled on.", [SkillItems.MaskTrait], null).WithPermanentQEffect(null,cr => cr.StartOfCombat = effect =>
        {
            if (!effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask"))
                effect.Owner.AddQEffect(SiHelpers.MaskOff());
            return Task.CompletedTask;
        });
        yield return new Feat(ModManager.RegisterFeatName("Auto"), null,"The default behavior for Scoundrel's Mask is auto; it will apply the damage from the item once the conditions are met.", [SkillItems.MaskTrait], null).WithPermanentQEffect(null,cr => cr.StartOfCombat = effect =>
        {
            if (!effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask"))
                effect.Owner.AddQEffect(SiHelpers.MaskAuto());
            return Task.CompletedTask;
        });
        yield return new Feat(ModManager.RegisterFeatName("Auto if Crit"), null,"The default behavior for Scoundrel's Mask is auto if crit; it will apply the damage from the item once the conditions are met and the attack was a crit.", [SkillItems.MaskTrait], null).WithPermanentQEffect(null,cr => cr.StartOfCombat = effect =>
        {
            if (!effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask"))
                effect.Owner.AddQEffect(SiHelpers.MaskAutoIfCrit());
            return Task.CompletedTask;
        });
        yield return new Feat(ModManager.RegisterFeatName("Ask"), null,"The default behavior for Scoundrel's Mask is ask; it will ask to apply the damage from the item once the conditions are met.", [SkillItems.MaskTrait], null).WithPermanentQEffect(null,cr => cr.StartOfCombat = effect =>
        {
            if (!effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask"))
                effect.Owner.AddQEffect(SiHelpers.MaskAsk());
            return Task.CompletedTask;
        });
        yield return new Feat(ModManager.RegisterFeatName("Ask if Crit"), null,"The default behavior for Scoundrel's Mask is ask if crit; it will ask to apply the damage from the item once the conditions are met and the attack was a crit.", [SkillItems.MaskTrait], null).WithPermanentQEffect(null,cr => cr.StartOfCombat = effect =>
        {
            if (!effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask"))
                effect.Owner.AddQEffect(SiHelpers.MaskAskIfCrit());
            return Task.CompletedTask;
        });
    }
    private static void AddVirtuosoLogic(Feat virtuoso)
    {
        virtuoso.WithPermanentQEffectAndSameRulesText(qfCoA =>
                qfCoA.BonusToSkills = (skill) =>
                {
                    if (skill != Skill.Performance) return null;
                    int amount = 2;
                    if (qfCoA.Owner.Proficiencies.Get(Trait.Performance) < Proficiency.Master)
                    {
                        amount = 1;
                    }
                    return new Bonus(amount, BonusType.Circumstance, "Virtuoso");
                })
            .WithPrerequisite(sheet => sheet.GetProficiency(Trait.Performance) >= Proficiency.Trained, "You must be trained in Performance.");
    }

    private static void AddIntimidatingProwessLogic(Feat prowess)
    {
        prowess.WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect("Intimidating Prowess",
                        "You gain a +1 circumstance bonus to your Intimidation checks and you ignore the penalty for not sharing a language. If your Strength is +5 or higher and you are a master in Intimidation, this bonus increases to +2.")
                    {
                        Innate = true,
                        Id = QEffectId.IntimidatingGlare,
                        BonusToSkills = skill =>
                        {
                            if (skill != Skill.Intimidation) return null;
                            Bonus bonus = new (
                                cr.Abilities.Strength >= 20 &&
                                cr.Proficiencies.Get(Trait.Intimidation) >= Proficiency.Master
                                    ? 2
                                    : 1, BonusType.Circumstance, "Intimidating Prowess");
                            return bonus;
                        }
                    }
                );
            })
            .WithPrerequisite(values =>
                    values.HasFeat(FeatName.ExpertIntimidation) &&
                    values.FinalAbilityScores.TotalScore(Ability.Strength) >= 16,
                "You must be an expert in Intimidation and have at least +3 Strength."
            );
    }
    private static void AddTumblingTeamworkLogic(Feat tumble)
    {
        tumble.WithPrerequisite(values => values.GetProficiency(Trait.Acrobatics) >= Proficiency.Trained, "You must be at an expert in Acrobatics.")
            .WithOnCreature( cr =>
            cr.AddQEffect(new QEffect()
                {
                    StateCheckWithVisibleChanges = _ =>
                    {
                        cr.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                AfterYouTakeAction = (effect, action) =>
                                { 
                                    if (action is { ActionId: ActionId.TumbleThrough, CheckResult: >= CheckResult.Success }) 
                                    {
                                        effect.Tag = action.ChosenTargets.ChosenCreature;
                                        cr.AddQEffect(new QEffect()
                                        {
                                            StateCheck = qEffect =>
                                            {
                                                Creature? enemy = effect.Tag as Creature;
                                                qEffect.AddGrantingOfTechnical(
                                                    creature => enemy != null &&
                                                                creature.IsAdjacentTo(enemy) &&
                                                                creature.FriendOfAndNotSelf(cr),
                                                    qf =>
                                                    {
                                                        qf.AfterYouAcquireEffect = async (qEffect1, effect1) =>
                                                        {
                                                            if (qEffect1 == qf)
                                                            {
                                                                Creature self = effect1.Owner;
                                                                List<Option> tileOptions =
                                                                [
                                                                    new CancelOption(true)
                                                                ];
                                                                CombatAction? moveAction = Possibilities.Create(self)
                                                                    .Filter(ap =>
                                                                    {
                                                                        if (ap.CombatAction.ActionId != ActionId.Step)
                                                                            return false;
                                                                        ap.CombatAction.ActionCost = -2;
                                                                        ap.RecalculateUsability();
                                                                        return true;
                                                                    }).CreateActions(true).FirstOrDefault(pw =>
                                                                        pw.Action.ActionId == ActionId.Step) as CombatAction;
                                                                if (!await self.Battle.AskToUseReaction(self,
                                                                        "Would you like to step while remaining adjacent to the enemy who was tumbled through?"))
                                                                {
                                                                    qf.ExpiresAt = ExpirationCondition.Immediately;
                                                                    qEffect.ExpiresAt = qf.ExpiresAt;
                                                                    return;
                                                                }

                                                                CreateSteppableTiles(enemy!, self).ForEach(tile =>
                                                                    {
                                                                        if (moveAction == null ||
                                                                            !(bool)moveAction.Target.CanBeginToUse(self)) return;
                                                                        tileOptions.Add(moveAction.CreateUseOptionOn(tile)
                                                                            .WithIllustration(moveAction.Illustration));
                                                                    }
                                                                );
                                                                Option chosenTile = (await self.Battle.SendRequest(
                                                                    new AdvancedRequest(self,
                                                                        "Choose where to step to or right-click to cancel. You must remain adjacent to the enemy.",
                                                                        tileOptions)
                                                                    {
                                                                        IsMainTurn = false,
                                                                        IsStandardMovementRequest = true,
                                                                        TopBarIcon = IllustrationName.FleetStep,
                                                                        TopBarText =
                                                                            "Choose where to step to or right-click to cancel. You must remain adjacent to the enemy.",
                                                                    })).ChosenOption;
                                                                switch (chosenTile)
                                                                {
                                                                    case CancelOption:
                                                                        moveAction!.RevertRequested = true;
                                                                        qf.ExpiresAt = ExpirationCondition.Immediately;
                                                                        qEffect.ExpiresAt = qf.ExpiresAt;
                                                                        break; 
                                                                    case TileOption tOpt:
                                                                        await tOpt.Action();
                                                                        qf.ExpiresAt = ExpirationCondition.Immediately;
                                                                        qEffect.ExpiresAt = qf.ExpiresAt;
                                                                        break;
                                                                }
                                                            }
                                                        };
                                                    });
                                            }
                                        });
                                    }
                                    return Task.CompletedTask; 
                                } 
                            });
                        return Task.CompletedTask;
                    }
                }
            )
        );
    }
    private static void CreateRootMagicLogic(Feat root)
    {
        root.WithPrerequisite(values => values.GetProficiency(Trait.Occultism) >= Proficiency.Trained,
                "You must be trained in Occultism.")
            .WithOnSheet(values => values.AddSelectionOption(new SingleFeatSelectionOption(
                "RootMagicFunctionality",
                "Root Magic Choice",
                SelectionOption.MORNING_PREPARATIONS_LEVEL,
                ft => ft.Tag is "Root Magic Apply"
                )));
    }
    private static void CreateRootChoiceLogic(Feat root, int index)
    {
        root.WithNameCreator(_ =>
                $"Grant Root Magic to {GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"}")
            .WithRulesTextCreator(_ =>
                $"{GetCharacterSheetFromPartyMember(index)?.Name ?? "NULL"} will gain the benefits of your root magic feature.")
            .WithIllustrationCreator(_ =>
                GetCharacterSheetFromPartyMember(index)?.Illustration ?? IllustrationName.MagicHide)
            .WithTag("Root Magic Apply")
            .WithPermanentQEffect(
                "An ally chosen before the start of combat gains the effects of your Root Magic feature.",
                qfFeat =>
                {
                    qfFeat.StartOfCombat = qfThis =>
                    {
                        if (GetCharacterSheetFromPartyMember(index) is {} hero
                            && qfThis.Owner.Battle.AllCreatures.FirstOrDefault(cr2 =>
                                cr2 != qfThis.Owner 
                                && cr2.PersistentCharacterSheet == hero) is { } chosenCreature
                            && !chosenCreature.PersistentUsedUpResources.UsedUpActions.Contains("Root Magic"))
                        {
                            QEffect rootMagic = CreateRootEffectLogic(qfFeat.Owner);
                            rootMagic.Name = "Root Magic";
                            rootMagic.DoNotShowUpOverhead = false;
                            chosenCreature.AddQEffect(rootMagic);
                        }
                        return Task.CompletedTask;
                    };
                })
            .WithPrerequisite(values => // Can't select yourself
                    GetCharacterSheetFromPartyMember(index) != values.Sheet,
                "Can't select yourself");
    }

    private static void CreateDirtyTrickLogic(Feat dirtyTrick)
    {
        dirtyTrick.WithPermanentQEffect(
                "You confound a foe's mobility through an underhanded tactic. Attempt a Thievery check against the target's Reflex DC.",
                qf =>
                {
                    qf.ProvideActionIntoPossibilitySection = (_, section) =>
                    {
                        if (section.PossibilitySectionId == PossibilitySectionId.AttackManeuvers)
                        {
                            return new ActionPossibility(CreateDirtyTrickAction(qf.Owner));
                        }
                        return null;
                    };
                }
            )
            .WithPrerequisite(values => values.GetProficiency(Trait.Thievery) >= Proficiency.Trained,
                "You must be trained in Thievery.");
    }
    private static List<Feat> AssuranceFeats()
    {
        List<Feat> skills = [];
        foreach (Skill skill in Skills.AllSkills)
        {
            var description = !PlayerProfile.Instance.IsBooleanOptionEnabled("AssuranceThreshold") ? $"You can forgo rolling for {skill.HumanizeTitleCase2()} skill checks to instead receive a result of 10 + your proficiency bonus (do not apply any other bonuses, penalties, or modifiers)." : $"The minimum result you can receive on a {skill.HumanizeTitleCase2()} skill check is a result of 10 + your proficiency bonus (do not apply any other bonuses, penalties, or modifiers).";
            Feat assuranceFeat = new Feat(ModManager.RegisterFeatName("Assurance - " + skill.HumanizeTitleCase2()),
                "Even in the worst circumstances, you can perform basic tasks.",
                description,
                [],
                null).WithTag(skill);
            CreateAssuranceLogic(assuranceFeat, skill);
            skills.Add(assuranceFeat);
        }
        return skills;
    }

    internal static void CreateAssuranceToggle(Creature self)
    {
        if (!self.HasFeat(ModData.FeatNames.AssuranceThreshold))
        {
            self.AddQEffect(new QEffect()
            {
                ProvideActionIntoPossibilitySection = (_, section) =>
                {
                    if (section.PossibilitySectionId == PossibilitySectionId.OtherManeuvers)
                        return new ActionPossibility(new CombatAction(self,
                                IllustrationName.FreeAction, "Toggle Assurance",
                                [Trait.Basic, Trait.DoesNotBreakStealth],
                                "Select an assurance option. Ask is default behavior and asks each time a relevant skill is checked. Off disables assurance. On applies assurance to all relevant skill checks.",
                                Target.Self())
                            .WithActionCost(0)
                            .WithEffectOnSelf(async (action, innerSelf) =>
                                {
                                    List<string> assuranceChoices = ["off", "on", "ask", "cancel"];
                                    if (innerSelf.HasEffect(ModData.QEffectIds.AssuranceOff))
                                        assuranceChoices.RemoveAll(str => str == "off");
                                    if (innerSelf.HasEffect(ModData.QEffectIds.AssuranceOn))
                                        assuranceChoices.RemoveAll(str => str == "on");
                                    if (!innerSelf.HasEffect(ModData.QEffectIds.AssuranceOff) &&
                                        !innerSelf.HasEffect(ModData.QEffectIds.AssuranceOn))
                                        assuranceChoices.RemoveAll(str => str == "ask");
                                    ChoiceButtonOption chosenOption = await innerSelf.AskForChoiceAmongButtons(
                                        IllustrationName.QuestionMark,
                                        "Choose your preferred assurance setting.",
                                        assuranceChoices.ToArray());
                                    if (assuranceChoices[chosenOption.Index] != "cancel")
                                    {
                                        if (assuranceChoices[chosenOption.Index] == "off")
                                        {
                                            innerSelf.RemoveAllQEffects(qff =>
                                                qff.Id == ModData.QEffectIds.AssuranceOn);
                                            self.AddQEffect(AssuranceOff());
                                        }

                                        if (assuranceChoices[chosenOption.Index] == "on")
                                        {
                                            innerSelf.RemoveAllQEffects(qff =>
                                                qff.Id == ModData.QEffectIds.AssuranceOff);
                                            innerSelf.AddQEffect(AssuranceOn());
                                        }

                                        if (assuranceChoices[chosenOption.Index] == "ask")
                                        {
                                            innerSelf.RemoveAllQEffects(qff =>
                                                qff.Id == ModData.QEffectIds.AssuranceOn
                                                || qff.Id == ModData.QEffectIds.AssuranceOff);
                                        }
                                    }
                                    else action.RevertRequested = true;
                                }
                            )
                        );
                    return null;
                }
            });
        }
    }
    private static void CreateAssuranceLogic(Feat assuranceFeat, Skill skill)
    {
        Trait skillTrait = Skills.SkillToTrait(skill);
        assuranceFeat.WithPrerequisite(values => values.GetProficiency(skillTrait) >= Proficiency.Trained,
                $"You must be trained in {skill.HumanizeTitleCase2()}.")
            .WithOnCreature((sheet, self) =>
                {
                    self.AddQEffect( new QEffect()
                    {
                        StartOfCombat = _ =>
                        {
                            if (self.HasFeat(ModData.FeatNames.AssuranceOn) && !self.HasEffect(ModData.QEffectIds.AssuranceOn) && !self.HasFeat(ModData.FeatNames.AssuranceThreshold))
                                self.AddQEffect(AssuranceOn());
                            if (self.HasFeat(ModData.FeatNames.AssuranceOff) && !self.HasEffect(ModData.QEffectIds.AssuranceOff) && !self.HasFeat(ModData.FeatNames.AssuranceThreshold))
                                self.AddQEffect(AssuranceOff());
                            return Task.CompletedTask;
                        },
                        YouBeginAction = async (_, action) =>
                        {
                            if (!self.HasEffect(ModData.QEffectIds.AssuranceOff) && !self.HasEffect(ModData.QEffectIds.AssuranceOn) && !self.HasFeat(ModData.FeatNames.AssuranceThreshold) && DoesActionHaveBreakdownAndSkill(action, skill))
                                if (await self.Battle.AskForConfirmation(self, IllustrationName.QuestionMark,
                                        "Use assurance with this action?", "yes"))
                                    self.AddQEffect(AssuranceAsk());
                        },
                        BeforeYourActiveRoll = async (_, action, _) =>
                        {
                            if (!self.HasEffect(ModData.QEffectIds.AssuranceOff) && !self.HasEffect(ModData.QEffectIds.AssuranceOn) && !self.HasFeat(ModData.FeatNames.AssuranceThreshold) && action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill == skill)
                                if (await self.Battle.AskForConfirmation(self, IllustrationName.QuestionMark,
                                        "Use assurance with this action?", "yes"))
                                    self.AddQEffect(AssuranceAsk());
                        },
                        AfterYouTakeAction = (_, _) =>
                        {
                            if (self.HasEffect(ModData.QEffectIds.AssuranceAsk))
                            {
                                self.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.AssuranceAsk);
                            }
                            return Task.CompletedTask;
                        },
                        AdjustActiveRollCheckResult = (_, action, target, result1) =>
                        {
                            if (action.ActiveRollSpecification == null) return result1;
                            int check =
                                (sheet.Proficiencies.Get(skillTrait)
                                    .ToNumber(self.ProficiencyLevel) + 10) - action.ActiveRollSpecification.DetermineDC(action, action.Owner, target)
                                    .TotalNumber;
                            if (action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill == skill && (self.HasEffect(ModData.QEffectIds.AssuranceOn) || self.HasEffect(ModData.QEffectIds.AssuranceAsk) || (self.HasFeat(ModData.FeatNames.AssuranceThreshold) && Threshold(result1, check))))
                            {
                                switch (check)
                                {
                                    case >= 0 and < 10:
                                        return CheckResult.Success;
                                    case < 0 and > -10:
                                        return CheckResult.Failure;
                                    case >= 10:
                                        return CheckResult.CriticalSuccess;
                                    case <= -10:
                                        return CheckResult.CriticalFailure;
                                }
                            }
                            return result1;
                        }
                    });
                }
            );
    }

    private static void CreateDistractingPerformance(Feat distract)
    {
        distract.WithPrerequisite(values => values.GetProficiency(Trait.Performance) >= Proficiency.Expert,
                "You must be an expert in Performance.")
            .WithPermanentQEffect(
                null,
                effect =>
                {
                    effect.ProvideActionIntoPossibilitySection = (qEffect, section) =>
                    {
                        if (section.PossibilitySectionId == PossibilitySectionId.NonAttackManeuvers)
                        {
                            return new ActionPossibility(new CombatAction(qEffect.Owner, IllustrationName.HauntingHymn,
                                    "Distracting Performance",
                                    [Trait.Mental, Trait.Auditory, Trait.AttackDoesNotTargetAC,
                                        Trait.AlwaysHits],
                                    "You Create a Diversion, except you roll a Performance check instead of Deception, and the benefits of successful checks apply to an ally of your choice instead of you. The effects of a success last until the end of that ally's turn, and can end early based on the ally's actions.",
                                    Target.MultipleCreatureTargets(Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100),
                                            Target.Ranged(100), Target.Ranged(100), Target.Ranged(100))
                                        .WithMinimumTargets(1).WithMustBeDistinct())
                                .WithTag("DistractingPerformance")
                                .WithSoundEffect(SfxName.Drum)
                                .WithActionId(ActionId.CreateADiversion)
                                .WithActionCost(1)
                                .WithEffectOnChosenTargets(async (caster, targets) =>
                                    {
                                        Creature self = qEffect.Owner;
                                        List<Creature> friends = [];
                                        foreach (Creature creature in self.Battle.AllCreatures.Where(cr =>
                                                     cr.FriendOfAndNotSelf(self)))
                                            friends.Add(creature);
                                        Creature? friend = await self.Battle.AskToChooseACreature(self, friends,
                                            IllustrationName.QuestionMark, "Choose an ally to be hidden", "", "choose");
                                        int roll = R.NextD20();
                                        bool flag1 = caster.HasEffect(QEffectId.LengthyDiversion);
                                        foreach (Creature chosenCreature in targets.ChosenCreatures)
                                        {
                                            CheckBreakdown breakdown = CombatActionExecution.BreakdownAttack(
                                                new CombatAction(self, null!,
                                                        "Performative Distraction",
                                                        [Trait.Basic], "[this condition has no description]",
                                                        Target.Self()).WithActionId(ActionId.CreateADiversion)
                                                    .WithActiveRollSpecification(new ActiveRollSpecification(
                                                        TaggedChecks.SkillCheck(Skill.Performance),
                                                        TaggedChecks.DefenseDC(Defense.Perception))), chosenCreature);
                                            CheckBreakdownResult breakdownResult =
                                                new CheckBreakdownResult(breakdown, roll);
                                            string str1 = breakdown.DescribeWithFinalRollTotal(breakdownResult);
                                            DefaultInterpolatedStringHandler interpolatedStringHandler;
                                            int d20Roll;
                                            if (breakdownResult.CheckResult >= CheckResult.Success)
                                            {
                                                friend!.DetectionStatus.HiddenTo.Add(chosenCreature);
                                                bool flag2 =
                                                    breakdownResult.CheckResult == CheckResult.CriticalSuccess &
                                                    flag1;
                                                Microsoft.Xna.Framework.Color lime = Microsoft.Xna.Framework.Color.Lime;
                                                string str2 = flag2 ? "{b}{Green}Hidden{/}{/b}" : "{Green}Hidden{/}";
                                                string str3 = chosenCreature.ToString();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 3);
                                                interpolatedStringHandler.AppendLiteral(" (");
                                                ref DefaultInterpolatedStringHandler local =
                                                    ref interpolatedStringHandler;
                                                d20Roll = breakdownResult.D20Roll;
                                                string str4 = d20Roll.ToString() + breakdown.TotalCheckBonus.WithPlus();
                                                local.AppendFormatted(str4);
                                                interpolatedStringHandler.AppendLiteral("=");
                                                interpolatedStringHandler.AppendFormatted(breakdownResult.D20Roll +
                                                    breakdown.TotalCheckBonus);
                                                interpolatedStringHandler.AppendLiteral(" vs. ");
                                                interpolatedStringHandler.AppendFormatted(breakdown.TotalDC);
                                                interpolatedStringHandler.AppendLiteral(").");
                                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                                string log = $"{str2} from {str3}{stringAndClear}";
                                                string logDetails = str1;
                                                chosenCreature.Overhead("hidden from", lime, log, "Create a diversion",
                                                    logDetails);
                                                friend.AddQEffect(new QEffect(flag2 ? "Lengthy diversion" : "Diversion",
                                                    $"You'll continue to be hidden from {chosenCreature} even in plain sight until you take an action that breaks stealth.",
                                                    (ExpirationCondition)(flag2 ? 0 : 8), chosenCreature,
                                                    flag2
                                                        ? (Illustration)IllustrationName.CreateADiversion
                                                        : null)
                                                {
                                                    DoNotShowUpOverhead = true,
                                                    Id = QEffectId.CreateADiversion,
                                                    YouBeginAction =
                                                        (Func<QEffect, CombatAction, Task>)((effect1, action) =>
                                                        {
                                                            if (action.ActionId == ActionId.Step ||
                                                                action.ActionId == ActionId.Sneak ||
                                                                action.ActionId == ActionId.Hide)
                                                                return Task.CompletedTask;
                                                            effect1.ExpiresAt = ExpirationCondition.Immediately;
                                                            return Task.CompletedTask;
                                                        })
                                                });
                                            }
                                            else
                                            {
                                                Microsoft.Xna.Framework.Color red = Microsoft.Xna.Framework.Color.Red;
                                                string str5 = chosenCreature.ToString();
                                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(10, 3);
                                                interpolatedStringHandler.AppendLiteral(" (");
                                                ref DefaultInterpolatedStringHandler local =
                                                    ref interpolatedStringHandler;
                                                d20Roll = breakdownResult.D20Roll;
                                                string str6 = d20Roll.ToString() + breakdown.TotalCheckBonus.WithPlus();
                                                local.AppendFormatted(str6);
                                                interpolatedStringHandler.AppendLiteral("=");
                                                interpolatedStringHandler.AppendFormatted(breakdownResult.D20Roll +
                                                    breakdown.TotalCheckBonus);
                                                interpolatedStringHandler.AppendLiteral(" vs. ");
                                                interpolatedStringHandler.AppendFormatted(breakdown.TotalDC);
                                                interpolatedStringHandler.AppendLiteral(").");
                                                string stringAndClear = interpolatedStringHandler.ToStringAndClear();
                                                string log = $"{{Red}}Failure{{/}} vs. {str5}{stringAndClear}";
                                                string logDetails = str1;
                                                chosenCreature.Overhead("diversion failed", red, log, "Create a diversion",
                                                    logDetails);
                                            }
                                            chosenCreature.AddQEffect(new QEffect()
                                            {
                                                BonusToDefenses =
                                                    ((Func<QEffect, CombatAction, Defense, Bonus>)(
                                                        (_, action, defense) =>
                                                        {
                                                            if (defense != Defense.Perception ||
                                                                action.ActionId != ActionId.CreateADiversion ||
                                                                action.Owner != caster)
                                                                return null!;
                                                            if (caster.HasEffect(QEffectId.ConfabulatorLegendary))
                                                                return null!;
                                                            if (caster.HasEffect(QEffectId.ConfabulatorMaster))
                                                                return new Bonus(1, BonusType.Circumstance,
                                                                    "Fool me twice... (Confabulator master)");
                                                            return caster.HasEffect(QEffectId.ConfabulatorExpert)
                                                                ? new Bonus(2, BonusType.Circumstance,
                                                                    "Fool me twice... (Confabulator)")
                                                                : new Bonus(4, BonusType.Circumstance,
                                                                    "Fool me twice...");
                                                        }))!
                                            });
                                        }
                                    }));
                        }
                        return null;
                    };
                }
            );
    }

    private static void CreateTumblingTrickLogic(Feat tumblingTrick)
    {
        tumblingTrick
            .WithPrerequisite(
                values => values.HasFeat(ModData.FeatNames.DirtyTrick) &&
                          values.GetProficiency(Trait.Acrobatics) >= Proficiency.Master,
                "You must have the Dirty Trick feat and be a master in Acrobatics.")
            .WithPermanentQEffect( null, qf =>
                {
                    QEffect effect = new QEffect()
                    {
                        StateCheckWithVisibleChanges = _ =>
                        {
                            qf.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                                {
                                    AfterYouTakeAction = async (_, action) =>
                                    {
                                        Creature self = qf.Owner;
                                        if (action is { ActionId: ActionId.TumbleThrough, CheckResult: >= CheckResult.Success } &&
                                            self.HasFreeHand)
                                        {
                                            CombatAction? dirtyTrick = Possibilities.Create(self)
                                                .Filter(ap =>
                                                {
                                                    if (ap.CombatAction.ActionId != ModData.ActionIds.DirtyTrickId)
                                                        return false;
                                                    ap.CombatAction.ActionCost = 0;
                                                    ap.CombatAction.Traits.Add(Trait.AttackDoesNotIncreaseMultipleAttackPenalty);
                                                    ap.RecalculateUsability();
                                                    return true;
                                                }).CreateActions(true).FirstOrDefault(pw =>
                                                    pw.Action.ActionId == ModData.ActionIds.DirtyTrickId) as CombatAction;
                                            if (!await self.Battle.AskToUseReaction(self,
                                                    "You succeeded at Tumble Through, would you like to use Dirty Trick as a reaction?"))
                                                return;
                                            QEffect bonus = new()
                                            {
                                                BonusToAttackRolls = (_, combatAction, _) =>
                                                    combatAction.ActionId == ModData.ActionIds.DirtyTrickId
                                                        ? new Bonus(1, BonusType.Circumstance, "Tumbling Trick")
                                                        : null
                                            };
                                            if (action.CheckResult == CheckResult.CriticalSuccess)
                                                self.AddQEffect(bonus);
                                            (dirtyTrick!.Target as CreatureTarget)!.CreatureTargetingRequirements.Clear();
                                            if (!await self.Battle.GameLoop.FullCast(dirtyTrick, action.ChosenTargets))
                                            {
                                                dirtyTrick.RevertRequested = true;
                                                return;
                                            }
                                            self.RemoveAllQEffects(qff => qff == bonus);
                                        }
                                    }
                                }
                            );
                            return Task.CompletedTask;
                        }
                    };
                    qf.Owner.AddQEffect(effect);
                }
                );
    }
    private static void CreateBattleCryLogic(Feat battleCry)
    {
        battleCry.WithPrerequisite(values => values.GetProficiency(Trait.Intimidation) >= Proficiency.Master,
                "You must be a master in Intimidation.")
            .WithPermanentQEffect(null, qf =>
                {
                    Creature self = qf.Owner;
                    qf.StartOfCombat = async _ =>
                    {
                        CombatAction? cry = Possibilities.Create(self)
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Demoralize)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            }).CreateActions(true).FirstOrDefault(pw =>
                                pw.Action.ActionId == ActionId.Demoralize) as CombatAction;
                        if (self.Battle.AllCreatures.Any(cr => cr.EnemyOf(self) && cr.DistanceTo(self) <= 6 && !cr.IsImmuneTo(Trait.Mental)))
                        {
                            if (!await self.AskForConfirmation(IllustrationName.QuestionMark,
                                    "Would you like to demoralize an enemy?", "yes"))
                                return;
                            await self.Battle.GameLoop.FullCast(cry!);
                        }
                    };
                    qf.AfterYouTakeActionAgainstTarget = async (_, action, enemy, result) =>
                    {
                        CombatAction? cry = Possibilities.Create(self)
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Demoralize)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.RecalculateUsability();
                                return true;
                            }).CreateActions(true).FirstOrDefault(pw =>
                                pw.Action.ActionId == ActionId.Demoralize) as CombatAction;
                        if (action.HasTrait(Trait.Attack) && result == CheckResult.CriticalSuccess && self.Proficiencies.Get(Trait.Intimidation) == Proficiency.Legendary && !enemy.IsImmuneTo(Trait.Mental))
                        {
                            if (!await self.AskToUseReaction(
                                    "Would you like to use a reaction to demoralize the foe you just critically hit?"))
                                return;
                            await self.Battle.GameLoop.FullCast(cry!, action.ChosenTargets);
                        }
                    };
                });
    }

    private static void CreateTerrifyingResistanceLogic(Feat terrifyingResistance)
    {
        terrifyingResistance.WithPermanentQEffect(
            "If you succeed in Demoralizing a creature, for the rest of the combat you gain a +1 circumstance bonus to saving throws against that creature's spells.",
            qf => qf.AfterYouTakeActionAgainstTarget = (effect, action, target, result) =>
            {
                if (action.ActionId is ActionId.Demoralize && result >= CheckResult.Success && effect.Owner.FindQEffect(ModData.QEffectIds.Terrify)?.Tag != target)
                {
                    effect.Owner.AddQEffect( new QEffect()
                        {
                            Tag = target,
                            Id = ModData.QEffectIds.Terrify,
                            BonusToDefenses = (_, spell, defense) =>
                            {
                                if (spell != null && spell.HasTrait(Trait.Spell) && defense.IsSavingThrow() &&
                                    spell.Owner == target)
                                    return new Bonus(1, BonusType.Circumstance, "Terrifying Resistance");
                                return null;
                            }
                        }
                    );
                }
                return Task.CompletedTask;
            }
        );
    }

    private static void CreateEvangelizeLogic(Feat evangelizeFeat)
    {
        evangelizeFeat.WithPrerequisite(sheet => sheet.GetProficiency(Trait.Diplomacy) >= Proficiency.Master, "You must be a master in Diplomacy.")
            .WithPermanentQEffect(null, qf =>
            {
                qf.ProvideActionIntoPossibilitySection = (effect, section) =>
                {
                    CombatAction evangelize = new CombatAction(effect.Owner, IllustrationName.CrisisOfFaith,
                            "Evangelize", [Trait.Linguistic, Trait.Mental, Trait.Auditory],
                            $"Attempt a Diplomacy check ({S.SkillBonus(effect.Owner, Skill.Diplomacy)}) and compare the result to the Will DC of a single target that can hear you and understands your language; that target is then temporarily immune to Evangelize from you for the rest of the combat." +
                            $"{S.FourDegreesOfSuccessReverse(null!, null!, "The target is stupefied 1 for 1 round.", "The target is stupefied 2 for 1 round.")}",
                            Target.Ranged(24).WithAdditionalConditionOnTargetCreature((_, enemy) => enemy.DoesNotSpeakCommon ? Usability.NotUsableOnThisCreature("You cannot evangelize to a creature which doesn't understand you.") : Usability.Usable))
                        .WithActionCost(1).WithActiveRollSpecification(
                            new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Diplomacy),
                                TaggedChecks.DefenseDC(Defense.Will)))
                        .WithProjectileCone(IllustrationName.Demoralize, 24, ProjectileKind.Cone)
                        .WithActionId(ModData.ActionIds.EvangelizeId)
                        .WithShortDescription("Attempt a Diplomacy check against a single target, stupefying it on a success or better.")
                        .WithEffectOnEachTarget((_, caster, target, result) =>
                            {
                                switch (result)
                                {
                                    case CheckResult.CriticalSuccess:
                                        target.AddQEffect(QEffect.Stupefied(2).WithExpirationAtStartOfSourcesTurn(caster,1));
                                        break;
                                    case CheckResult.Success:
                                        target.AddQEffect(QEffect.Stupefied(1).WithExpirationAtStartOfSourcesTurn(caster,1));
                                        break;
                                    case CheckResult.Failure:
                                    case CheckResult.CriticalFailure:
                                        break;
                                }
                                target.AddQEffect(QEffect.ImmunityToTargeting(ModData.ActionIds.EvangelizeId, caster));
                                return Task.CompletedTask;
                            }
                            );
                    if (section.PossibilitySectionId == PossibilitySectionId.NonAttackManeuvers)
                        return new ActionPossibility(evangelize);
                    return null;
                };
            }
        );
    }
    /*private static void CreateTalentEnvyLogic(Feat talentEnvy)
    {
        talentEnvy.WithPrerequisite(
                sheet => sheet.GetProficiency(Trait.Performance) >= Proficiency.Master &&
                         sheet.HasFeat(ModData.FeatNames.Virtuoso),
                "You must be a master in Performance and have the Virtuosic Performer feat.")
            .WithPermanentQEffect(null, effect =>
            {
                effect.AfterYouTakeAction = async (qEffect, action) =>
                {
                    Creature self = qEffect.Owner;
                    QEffect noEnvy = new()
                    {
                        Id = ModData.QEffectIds.Envy,
                        Illustration = IllustrationName.DirgeOfDoom,
                        Name = "Talent Envy Cooldown",
                        Description = "You cannot use Talent Envy again until a minute has passed."
                    };
                    noEnvy.WithExpirationAtStartOfSourcesTurn(self, 10);
                    QEffect envyCrit = new() { AdjustActiveRollCheckResult = (_, combatAction, _, result) => combatAction.ActionId == ActionId.Demoralize ? result.ImproveByOneStep() : result };
                    if ((action.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill == Skill.Performance && action.CheckResult >= CheckResult.Success && !self.HasEffect(ModData.QEffectIds.Envy)) 
                        ||
                        (DoesActionHaveBreakdownAndSkill(action, Skill.Performance) && self.HasEffect(ModData.QEffectIds.Talent) && !self.HasEffect(ModData.QEffectIds.Envy)))
                    {
                        List<Creature> enemies = [];
                        enemies.AddRange(self.Battle.AllCreatures.Where(cr => cr.EnemyOf(self) && cr.DistanceTo(self) <= 12 && !cr.IsImmuneTo(Trait.Mental) && !cr.HasEffect(QEffectId.ImmunityToTargeting)));
                        CombatAction? demoralize = Possibilities.Create(self)
                            .Filter(ap =>
                            {
                                if (ap.CombatAction.ActionId != ActionId.Demoralize)
                                    return false;
                                ap.CombatAction.ActionCost = 0;
                                ap.CombatAction.Target = Target.Ranged(12);
                                ap.RecalculateUsability();
                                return true;
                            }).CreateActions(true).FirstOrDefault(pw =>
                                pw.Action.ActionId == ActionId.Demoralize) as CombatAction;
                        if (enemies.Count > 0)
                        {
                            if (await self.Battle.AskForConfirmation(self, IllustrationName.Demoralize,
                                "Do you wish to use Talent Envy to demoralize as a free action?", "yes"))
                            {
                                if (action.CheckResult == CheckResult.CriticalSuccess || ((string)self.FindQEffect(ModData.QEffectIds.Talent)?.Tag! == "CriticalSuccess") )
                                    self.AddQEffect(envyCrit);
                                if (!await self.Battle.GameLoop.FullCast(demoralize!))
                                {
                                    self.RemoveAllQEffects(qf =>
                                        qf == envyCrit);
                                }
                                self.AddQEffect(noEnvy);
                            }
                        }
                    }
                    if (action.ActionId == ActionId.Demoralize && self.HasEffect(envyCrit))
                        self.RemoveAllQEffects(qf => qf == envyCrit);
                };
            });
    }*/
    //utility functions
    private static QEffect CreateRootEffectLogic(Creature self)
    {
        if (ModManager.TryParse("RL_Haunt", out Trait haunt))
        {
            QEffect rootEffect = new QEffect()
            {
                Illustration = IllustrationName.MagicHide,
                Description = "You gain a bonus to a saving throw against a spell or haunt based on the proficiency of the character that provided this effect.",
                BeforeYourSavingThrow = async (effect, action, creature) =>
                {
                    if ((action.HasTrait(Trait.Spell) || action.HasTrait(haunt))
                        && !creature.PersistentUsedUpResources.UsedUpActions.Contains("Root Magic"))
                    {
                        await creature.Battle.GameLoop.FullCast(CreateRootFreeAction(self, creature));
                        creature.RemoveAllQEffects(qf => qf == effect);
                    }
                }
            };
            return rootEffect;
        }
        else
        {
            QEffect rootEffect = new QEffect()
            {
                Illustration = IllustrationName.MagicHide,
                Description = "You gain a bonus to a saving throw against a spell or haunt based on the proficiency of the character that provided this effect.",
                BeforeYourSavingThrow = async (effect, action, creature) =>
                {
                    if (action.HasTrait(Trait.Spell)
                        && !creature.PersistentUsedUpResources.UsedUpActions.Contains("Root Magic"))
                    {
                        await creature.Battle.GameLoop.FullCast(CreateRootFreeAction(self, creature));
                        creature.RemoveAllQEffects(qf => qf == effect);
                    }
                }
            };
            return rootEffect;
        }
    }
    private static CharacterSheet? GetCharacterSheetFromPartyMember(int index)
    {
        CharacterSheet? hero = null;
        if (CampaignState.Instance is { } campaign)
            hero = campaign.Heroes[index].CharacterSheet;
        else if (CharacterLibrary.Instance is { } library)
            hero = library.SelectedRandomEncounterParty[index];
        return hero;
    }
    private static IList<Tile> CreateSteppableTiles(Creature enemy, Creature player)
    {
        IList<Tile> floodFill = Pathfinding.Floodfill(player, player.Battle,
                new PathfindingDescription()
                {
                    Squares = 1,
                    Style = { MaximumSquares = 1 }
                })
            .Where(tile =>
                tile.LooksFreeTo(player) 
                && tile.IsAdjacentTo(enemy.Occupies)
                && (player.HasEffect(QEffectId.Flying) || player.HasEffect(QEffectId.IgnoresDifficultTerrain) || !tile.DifficultTerrain))
            .ToList();
        return floodFill;
    }
    private static CombatAction CreateRootFreeAction(Creature self, Creature caster)
    {
        CombatAction rootFreeAction =
            new CombatAction(caster, IllustrationName.MagicHide, "Root Magic", [], "You gain a bonus to a saving throw against a spell or haunt based on the proficiency of the character that provided this effect.", Target.Self())
                .WithActionCost(0)
                .WithEffectOnSelf(creature =>
                {
                    creature.AddQEffect(new QEffect(ExpirationCondition.Never)
                        {
                            Id = ModData.QEffectIds.RootMagic,
                            BonusToDefenses = (_, _, defense) =>
                            {
                                if (defense.IsSavingThrow())
                                    return new Bonus(self.Proficiencies.Get(Trait.Occultism) >= Proficiency.Legendary ? 3 : self.Proficiencies.Get(Trait.Occultism) >= Proficiency.Expert ? 2 : 1, BonusType.Circumstance,
                                        "Root Magic");
                                return null;
                            },
                            AfterYouMakeSavingThrow = (_, _, _) =>
                            {
                                caster.RemoveAllQEffects(qf => qf.Id == ModData.QEffectIds.RootMagic);
                            }
                        }
                    );
                    creature.PersistentUsedUpResources.UsedUpActions.Add("Root Magic");
                });
            return rootFreeAction;
    }
    private static CombatAction CreateDirtyTrickAction(Creature self)
    {
        CombatAction dirtyTrick = new CombatAction(self, IllustrationName.BootsOfBounding, "Dirty Trick",
                [Trait.Attack, Trait.Manipulate, Trait.AttackDoesNotTargetAC],
                $"Attempt a Thievery check ({S.SkillBonus(self, Skill.Thievery)}) against the target's Reflex DC." +
                S.FourDegreesOfSuccess("The target is clumsy 1 until they use an Interact action to end the impediment", "As critical success, but the condition ends automatically after 1 round.", null, "You fall prone as your attempt backfires."),
                Target.AdjacentCreature().WithAdditionalConditionOnTargetCreature((a, d) => !a.HasFreeHand ? Usability.CommonReasons.NoFreeHandForManeuver : !d.EnemyOf(a) ? Usability.NotUsableOnThisCreature("May only target enemies") : Usability.Usable))
            .WithActionCost(1)
            .WithShortDescription("Attempt a Thievery check against a target within reach, making it clumsy on a success or better.")
            .WithActionId(ModData.ActionIds.DirtyTrickId)
            .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Thievery),
                TaggedChecks.DefenseDC(Defense.Reflex)))
            .WithEffectOnChosenTargets((action, caster, target) =>
                {
                    switch (action.CheckResult)
                    {
                        case CheckResult.Success:
                        case CheckResult.CriticalSuccess:
                            QEffect tricked = QEffect.Clumsy(1);
                            tricked.ExpiresAt = action.CheckResult == CheckResult.CriticalSuccess
                                ? ExpirationCondition.Never
                                : ExpirationCondition.ExpiresAtEndOfYourTurn;
                            target.ChosenCreature!.AddQEffect(tricked);
                            if (action.CheckResult == CheckResult.CriticalSuccess)
                            {
                                target.ChosenCreature!.AddQEffect(new QEffect()
                                {
                                    Id = ModData.QEffectIds.DirtyTricked,
                                    ProvideContextualAction = qfInner =>
                                    {
                                        return new ActionPossibility(new CombatAction(qfInner.Owner,
                                                IllustrationName.BootsOfBounding, "Remove dirty trick",
                                                [Trait.Manipulate],
                                                "Removes clumsy from dirty trick.", Target.Self((_, ai) => ai.AlwaysIfSmartAndTakingCareOfSelf))
                                            .WithActionCost(1)
                                            .WithEffectOnSelf(cr =>
                                            { 
                                                if (cr.FindQEffect(QEffectId.Clumsy) is { } clumsyEffect)
                                                {
                                                    cr.RemoveAllQEffects(qfNew =>
                                                        qfNew == clumsyEffect ||
                                                        qfNew.Id == ModData.QEffectIds.DirtyTricked);
                                                }
                                            })
                                        );
                                    }

                                });
                            }
                            break;
                        case CheckResult.CriticalFailure:
                            caster.AddQEffect(QEffect.Prone());
                            break;
                    }
                    return Task.CompletedTask;
                }
            );
        return dirtyTrick;
    }
    private static QEffect AssuranceAsk()
    {
        QEffect ask = new()
        {
            Id = ModData.QEffectIds.AssuranceAsk,
            DoNotShowUpOverhead = true
        };
        return ask;
    }
    private static QEffect AssuranceOn()
    {
        QEffect on = new()
        {
            Id = ModData.QEffectIds.AssuranceOn,
            Illustration = new ModdedIllustration("SIAssets/On.png"),
            Description = "Assurance will be applied on relevant skill checks.",
            Name = "Assurance - On",
            DoNotShowUpOverhead = true
        };
        return on;
    }    
    private static QEffect AssuranceOff()
    {
        QEffect off = new()
        {
            Id = ModData.QEffectIds.AssuranceOff,
            Illustration = new ModdedIllustration("SIAssets/Off.png"),
            Description = "Assurance will not be applied.",
            Name = "Assurance - Off",
            DoNotShowUpOverhead = true
        };
        return off;
    }
    private static bool DoesActionHaveBreakdownAndSkill(CombatAction action, Skill skill)
    {
        switch (action)
        {
            case { Name: "Wing Buffet"} when skill is Skill.Athletics:
            case { ActionId: ActionId.Hide } when skill is Skill.Stealth:
            case { ActionId: ActionId.CreateADiversion } when skill is Skill.Deception:
            case { ActionId: ActionId.Sneak } when skill is Skill.Stealth:
            case { Tag: "DistractingPerformance" } when skill is Skill.Performance:
            case { Name: "Counter Performance" } when skill is Skill.Performance:
                return true;
        }
        return (action.HasTrait(Trait.LingeringComposition) || action.HasTrait(Trait.InspireHeroics)) && skill is Skill.Performance;
    }
    private static bool Threshold(CheckResult result, int check)
    {
        if (result != CheckResult.CriticalSuccess && check >= 0)
            return true;
        return result <= CheckResult.Failure && check > -10;
    }
    /*private static bool GetBreakdown(CombatAction action, Skill skill)
    {
        switch (action)
        {
            case { Name: "Step (Sneak)" } when skill is Skill.Stealth:
            case { Name: "Inspire Heroics" } when skill is Skill.Performance:
            case { Name: "Lingering Composition" } when skill is Skill.Performance:
            case { Name: "Hide" } when skill is Skill.Stealth:
            case { Name: "Create a Diversion"} when skill is Skill.Deception:
            case { Name: "Sneak" } when skill is Skill.Stealth:
            case { Name: "Performative Distraction" } when skill is Skill.Performance:
            case { Name: "Counter Performance" } when skill is Skill.Performance:
                return true;
        }
        return false;
    }*/
}