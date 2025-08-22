using System.Collections.Immutable;
using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace New_Skill_Feats_and_Items;

public abstract class SkillItems
{
    internal static readonly Trait MaskTrait = ModManager.RegisterTrait("MaskTrait", new TraitProperties("MaskTrait", false));
    public static void AddItems()
    {
        ItemName dancingScarf = ModManager.RegisterNewItemIntoTheShop("Dancing Scarf", itemName =>
        {
            return new Item(itemName, new ModdedIllustration("SIAssets/DanceScarf.png"), "dancing scarf", 3, 60, Trait.Magical, Trait.Invested, Trait.Visual)
                .WithWornAt(Trait.Necklace)
                .WithDescription("{i}This long and billowing scarf is typically woven of silk or sheer fabric and adorned with bells or other jangling bits of shiny metal.{/i}\n\n" +
                                 "You have a +1 item bonus to Performance." +
                                 "\n\nYou can use {i}Swirling Scarf{/i} {icon:Action}: If your last action was a successful Performance check, you become concealed until the beginning of your next turn.")
                .WithItemAction((_, user) =>
                    {
                        return new CombatAction(user, new ModdedIllustration("SIAssets/DanceScarf.png"), "Swirling Scarf", [Trait.Manipulate, Trait.Basic],
                                "{b}Requirements{/b} On your most recent action, you succeeded at a Performance check" +
                                "\n\nYou become concealed until the beginning of your next turn.",
                                Target.Self().WithAdditionalRestriction((Func<Creature, string>)(self =>
                                        {
                                            if (self.Actions.ActionHistoryThisTurn.Count == 0)
                                                return "You didn't perform this turn";
                                            CombatAction combatAction = self.Actions.ActionHistoryThisTurn.Last();
                                            return (combatAction.ActiveRollSpecification?.TaggedDetermineBonus.InvolvedSkill is Skill.Performance && combatAction.CheckResult >= CheckResult.Success ? null : "Your most recent action wasn't a successful performance.")!;
                                        }
                                    )))
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.InvisibilityPoor)
                            .WithEffectOnSelf(self =>
                            {
                                self.AddQEffect(new QEffect(

                                        "Swirling Scarf",
                                        "You are concealed until the beginning of your next turn.",
                                        ExpirationCondition.ExpiresAtStartOfYourTurn,
                                        self,
                                        IllustrationName.InFog)
                                    { ThisCreatureCannotBeMoreVisibleThan = DetectionStrength.Concealed }

                                );

                            });
                    }, 
                    (_, _) => true)
                .WithPermanentQEffectWhenWorn((qfCoA, _) =>
                {
                    qfCoA.BonusToSkills = skill => skill == Skill.Performance ? new Bonus(1, BonusType.Item, "Dancing Scarf") : null;
                });
        });
        ModManager.RegisterActionOnEachCreature( creature =>
            {
                if (creature.CarriesItem(dancingScarf))
                {
                    creature.AddQEffect(new QEffect
                    {
                        StateCheck = AddDanceAction
                    });
                }
            }
        );
        ModManager.RegisterNewItemIntoTheShop("Crafter's Eyepiece", itemName =>
        {
            return new Item(itemName, new ModdedIllustration("SIAssets/Eyepiece.png"), "crafter's eyepiece", 3, 60, Trait.Magical, Trait.Invested)
                .WithWornAt(Trait.Headband)
                .WithDescription("{i}This rugged metal eyepiece etched with square patterns is designed to be worn over a single eye. Twisting the lens reveals a faint three-dimensional outline of an item you plan to build or repair, with helpful labels on the component parts.{/i}\n\n" +
                                 "You have a +1 item bonus to Crafting." +
                                 "\n\nOnce at the start of combat, as a {icon:FreeAction} free action, you can make improvements to a weapon or shield. Make a Crafting check against the standard DC by level of the item you're affecting, on a Success or better a weapon gains a +1 bonus to its damage rolls and a shield gains a +1 bonus to its hardness. If you are a master in Crafting, these bonuses increase to +2.")
                .WithOnCreatureWhenWorn((_, self) => { self.AddQEffect(SiHelpers.EyepieceChosenEffect());})
                .WithPermanentQEffectWhenWorn((qfCoA, _) =>
                {
                    qfCoA.BonusToSkills = skill => skill == Skill.Crafting ? new Bonus(1, BonusType.Item, "Crafter's Eyepiece") : null;
                    qfCoA.StartOfCombat = async innerSelf =>
                    {
                        List<string> itemOptionsString = [];
                        List<Item> itemOptions = [];
                        foreach (Item item1 in innerSelf.Owner.HeldItems)
                        {
                            itemOptionsString.Add(item1.Name);
                            itemOptions.Add(item1);
                        }
                        itemOptionsString.Add("none");
                        ChoiceButtonOption chosenOption = await innerSelf.Owner.AskForChoiceAmongButtons(
                            IllustrationName.QuestionMark,
                            "Choose an item to enhance.",
                            itemOptionsString.ToArray()
                        );
                        if (itemOptionsString[chosenOption.Index] != "none")
                        {
                            Item targetItem = itemOptions[chosenOption.Index];
                            await innerSelf.Owner.Battle.GameLoop.FullCast(SiHelpers.ChooseItemStart(innerSelf.Owner, targetItem));
                        }
                    };
                });
        });
        ModManager.RegisterNewItemIntoTheShop("Charlatan's Gloves", itemName =>
        {
            return new Item(itemName, new ModdedIllustration("SIAssets/Gloves.png"), "charlatan's gloves", 3, 50, Trait.Magical, Trait.Invested)
                .WithWornAt(Trait.Gloves)
                .WithDescription("{i}Tiny silver hooks decorate these fine silk gloves.{/i}\n\n" +
                                 "You have a +1 item bonus to Thievery. You can cast {i}open door {icon:Action}{/i} as an innate occult spell at will.")
                .WithOnCreatureWhenWorn((_, self) =>
                {
                    self.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, Trait.Innate, Ability.Charisma,
                        Trait.Occult).WithSpells([SpellId.OpenDoor], 1);
                })
                .WithPermanentQEffectWhenWorn((qfCoA, _) =>
                {
                    qfCoA.BonusToSkills = skill => skill == Skill.Thievery ? new Bonus(1, BonusType.Item, "Charlatan's Gloves") : null;
                });
        });
        ModManager.RegisterNewItemIntoTheShop("Scoundrel's Mask", itemName =>
        {
            Item scoundrelMask = new Item(itemName, ModData.Illustrations.Mask, "scoundrel's mask", 4, 80, Trait.Magical, Trait.Invested, Trait.Homebrew, MaskTrait)
                .WithWornAt(Trait.Mask)
                .WithDescription("{i}This mask appears to be a simple black leather mask, until it is worn, after which the mask seems to disappear. While wearing it, the mask subtly alters the wearer's facial expressions, aiding them in attempts at deceiving others. Occasionally, it also draws the wearer's eyes to their enemy's vulnerabilities.{/i}\n\n" +
                                 "You have a +1 item bonus to Deception." +
                                 "\n\nOnce per day, when you strike a creature who is off-guard with a weapon attack, you can deal an extra 1d6 precision damage to the target.")
                .WithPermanentQEffectWhenWorn((qf, _) =>
                {
                    Creature self = qf.Owner;
                    qf.StateCheckWithVisibleChanges = _ =>
                    {
                        if (self.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask") && SiHelpers.HasMaskQf(self))
                            self.RemoveAllQEffects(qff => SiHelpers.HasMaskQf(qff.Id, null));
                        return Task.CompletedTask;
                    };
                    qf.StartOfCombat = _ =>
                    {
                        qf.StateCheckLayer = 1;
                        if (!self.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask") && !SiHelpers.HasMaskQf(self))
                            qf.Owner.AddQEffect(SiHelpers.MaskAskIfCrit());
                        return Task.CompletedTask;
                    };
                    qf.BonusToSkills = skill => skill == Skill.Deception ? new Bonus(1, BonusType.Item, "Scoundrel's Mask") : null;
                    qf.YouDealDamageEvent = async (effect, damageEvent) =>
                    {
                        CombatAction? action = damageEvent.CombatAction;
                        Creature target = damageEvent.TargetCreature;
                        CheckResult result = damageEvent.CheckResult;
                        if (action != null && target.IsFlatFootedTo(self, action) && action.HasTrait(Trait.Weapon) && action.HasTrait(Trait.Attack) && action.HasTrait(Trait.Strike) 
                            && result >= CheckResult.Success && !self.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask")
                            && action.Item != null && !target.IsImmuneTo(Trait.PrecisionDamage))
                        {
                            if (self.HasEffect(ModData.QEffectIds.MaskAsk) || (self.HasEffect(ModData.QEffectIds.MaskAskIfCrit) && action.CheckResult == CheckResult.CriticalSuccess))
                            {
                                if (!await self.Battle.AskForConfirmation(effect.Owner, IllustrationName.QuestionMark,
                                        "Do you want to use Scoundrel's Mask to deal an extra 1d6 precision damage to the target?",
                                        "yes"))
                                    return;
                                damageEvent.KindedDamages.Add(new KindedDamage(
                                    DiceFormula.FromText("1d6", "Scoundrel's Mask"),
                                    damageEvent.KindedDamages.FirstOrDefault()!.DamageKind));
                                self.PersistentUsedUpResources.UsedUpActions.Add("ScoundrelsMask");
                            }
                            if (self.HasEffect(ModData.QEffectIds.MaskAuto) || (self.HasEffect(ModData.QEffectIds.MaskAutoIfCrit) && action.CheckResult == CheckResult.CriticalSuccess))
                            {
                                damageEvent.KindedDamages.Add(new KindedDamage(
                                    DiceFormula.FromText("1d6", "Scoundrel's Mask"),
                                    damageEvent.KindedDamages.FirstOrDefault()!.DamageKind));
                                self.PersistentUsedUpResources.UsedUpActions.Add("ScoundrelsMask");
                            }
                        }
                    };
                    qf.ProvideActionIntoPossibilitySection = (effect, section) => section.PossibilitySectionId == PossibilitySectionId.ItemActions && !effect.Owner.PersistentUsedUpResources.UsedUpActions.Contains("ScoundrelsMask") ? new ActionPossibility(SiHelpers.AddMaskToggle(effect.Owner)) : null;
                });
            return scoundrelMask;
        });
        ModManager.RegisterNewItemIntoTheShop("HuntersArrowhead", itemName =>
        {
            Item item = new Item(itemName, new ModdedIllustration("SIAssets/HuntersArrowhead.png"), "Hunter's Arrowhead", 4, 80, Trait.Enchantment, Trait.Magical, Trait.Invested, Trait.Worn)
                // .WithItemBonusToSkill(Skill.Survival)
                .WithDescription("{i}This arrowhead-shaped charm is not meant to be affixed to an arrow, but instead to be carried in a pocket or inside of a quiver.{/i}\n\nYou have a +1 item bonus to Survival.\n\nOnce per day as a {icon:Reaction} reaction, when you would miss with an attack roll with a bow, you gain a +2 circumstance bonus to that attack roll, this can turn a miss into a hit.")
                .WithPermanentQEffectWhenWorn((qf, _) =>
                {
                    Creature self = qf.Owner;
                    qf.BonusToSkills = skill => skill == Skill.Survival ? new Bonus(1, BonusType.Item, "Hunter's Arrowhead") : null;
                    qf.BeforeYourActiveRoll = (_, action, target) =>
                    { 
                        var circ = 0;
                        List<int> circBonuses = [];
                        var bonusEnumerable = action.ActiveRollSpecification?.DetermineBonus(action, self, target).Bonuses;
                        if (bonusEnumerable != null)
                            foreach (Bonus? bonus in bonusEnumerable)
                            {
                                if (bonus is { BonusType: BonusType.Circumstance })
                                    circBonuses.Add(bonus.Amount);
                            }
                        if (circBonuses.Count > 0)
                            circ = circBonuses.Max();
                        ImmutableList<int> save = [circ];
                        if (action.HasTrait(Trait.Strike) && action.ActiveRollSpecification != null && action.Item != null && action.Item.HasTrait(Trait.Bow) && !self.PersistentUsedUpResources.UsedUpActions.Contains("HuntersArrowhead") && circ < 2)
                        {
                            target.AddQEffect(new QEffect()
                                {
                                    YouAreTargetedByARoll = async (eff, combatAction, breakdown) =>
                                    {
                                        var ac = combatAction.ActiveRollSpecification?.TaggedDetermineDC.InvolvedDefense == Defense.AC ? combatAction.ActiveRollSpecification.TaggedDetermineDC.CalculatedNumberProducer.Invoke(combatAction, self, target).TotalNumber : 0;
                                        var miss = ac - breakdown.TotalRollValue;
                                        if (breakdown.CheckResult == CheckResult.Failure &&
                                            combatAction.HasTrait(Trait.Strike) && action.Item != null &&
                                            action.Item.HasTrait(Trait.Bow) && miss <= 2-save.FirstOrDefault())
                                        {
                                            if (await self.AskToUseReaction(
                                                    "Would you like to use Hunter's Arrowhead to turn a miss into a hit?"))
                                            {
                                                self.Overhead("hunter's arrowhead", Color.Lime,
                                                    self +
                                                    " utilizes Hunter's Arrowhead to turn a miss into a hit.");
                                                self.PersistentUsedUpResources.UsedUpActions.Add("HuntersArrowhead");
                                                eff.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                                self.AddQEffect(
                                                    new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                                                    {
                                                        BonusToAttackRolls = (_, cAction, _) =>
                                                        {
                                                            if (cAction.HasTrait(Trait.Strike) &&
                                                                cAction.Item != null &&
                                                                cAction.Item.HasTrait(Trait.Bow))
                                                            {
                                                                return new Bonus(2, BonusType.Circumstance,
                                                                    "Hunter's Arrowhead");
                                                            }
                                                            return null;
                                                        }
                                                    });
                                                return true;
                                            }
                                        }
                                        eff.ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction;
                                        return false;
                                    }
                                });
                        }
                        return Task.CompletedTask;
                    };
                });
            return item;
        });
        ModManager.RegisterActionOnEachCharacterSheet(values =>
        {
            SelectionOption maskSelections = new SingleFeatSelectionOption("MaskDefault",
                "Scoundrel's Mask Default Behavior", SelectionOption.PRECOMBAT_PREPARATIONS_LEVEL,
                feat => feat.HasTrait(MaskTrait));
            Item? item = values.Inventory.Backpack.Find(item1 => item1 != null && item1.HasTrait(MaskTrait));
            if (item != null && values.Inventory.IsBestWornItemInItsBodyPart(item))
            {
                values.Calculated.AddSelectionOption(maskSelections);
            }
        });
        ModManager.RegisterNewItemIntoTheShop("Choker of Nobility", itemName =>
        {
            return new Item(itemName, new ModdedIllustration("SIAssets/Choker.png"), "choker of nobility", 3, 60, Trait.Magical, Trait.Invested, Trait.Homebrew)
                .WithWornAt(Trait.Necklace)
                .WithDescription("{i}This fine golden choker endows the wearer with an aura of nobility, adding weight to their words.{/i}\n\n" +
                                 "You have a +1 item bonus to Diplomacy. You can cast {i}guidance {icon:Action}{/i} as an innate divine spell at will.")
                .WithOnCreatureWhenWorn((_, self) =>
                {
                    self.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, Trait.Innate, Ability.Charisma,
                        Trait.Divine).WithSpells([SpellId.Guidance], 1);
                })
                .WithPermanentQEffectWhenWorn((qfCoA, _) =>
                {
                    qfCoA.BonusToSkills = skill => skill == Skill.Diplomacy ? new Bonus(1, BonusType.Item, "Choker of Nobility") : null;
                });
        });
    }
    private static void AddDanceAction(QEffect self)
    {
        self.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
        {
            Innate = false,
            ProvideActionIntoPossibilitySection = delegate (QEffect effect, PossibilitySection section)
            {
                if (section.PossibilitySectionId == PossibilitySectionId.ItemActions)
                {
                    return new ActionPossibility(new CombatAction(effect.Owner, new ModdedIllustration("SIAssets/Dance.png"), "Dance", [Trait.Concentrate, Trait.Move, Trait.Visual, Trait.Basic],
                            "You perform a dance. Attempt a Performance check with an easy DC of your level. This action has no effect on its own, but allows you to use actions that depend on a successful performance check.",
                            Target.Self())
                        .WithActionCost(1)
                        .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Performance), Checks.FlatDC(Checks.LevelBasedDC(effect.Owner.Level)-2)))
                        .WithSoundEffect(SfxName.Drum));
                }
                return null;
            }
        });
    }
}