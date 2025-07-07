using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Microsoft.Xna.Framework;

namespace New_Skill_Feats_and_Items;

internal abstract class SiHelpers
{
    public static CombatAction ChooseItemStart(Creature self, Item item)
    {
        CombatAction itemChoice = new CombatAction(self, item.Illustration, $"Choose {item.Name}", [],
                $"Choose {item.Name} and make a crafting check, if you succeed gain a benefit depending on if the item chosen was a shield or weapon.", Target.Self())
            .WithSoundEffect(Dawnsbury.Audio.SfxName.MagicWeapon)
            .WithActionCost(0)
            .WithActiveRollSpecification(new ActiveRollSpecification(TaggedChecks.SkillCheck(Skill.Crafting), 
                new TaggedCalculatedNumberProducer((_, _, _) =>
                    new CalculatedNumber(Checks.LevelBasedDC(item.Level), "Item Level-based DC",
                        []))))
            .WithEffectOnEachTarget(delegate (CombatAction _, Creature innerSelf, Creature _, CheckResult result)
            {
                if (result >= CheckResult.Success )
                {
                    innerSelf.AddQEffect(item.HasTrait(Trait.Shield) ? ChosenShield(item) : ChosenWeapon(item));
                }
                return Task.CompletedTask;
            });
            
        return itemChoice;
    }
    private static QEffect ChosenShield(Item item)
    {
        QEffect chooseShield = new QEffect($"Chosen Item ({item.Name})",
            "This item benefits from the effect of your Crafter's Eyepiece")
        {
            ExpiresAt = ExpirationCondition.Never,
            Tag = item,
            Illustration = item.Illustration,
            Id = ModData.QEffectIds.ChosenShieldQf
        };
        return chooseShield;
    }

    private static QEffect ChosenWeapon(Item item)
    {
        QEffect chooseWeapon = new QEffect($"Chosen Item ({item.Name})",
            "This item benefits from the effect of your Crafter's Eyepiece")
        {
            ExpiresAt = ExpirationCondition.Never,
            Tag = item,
            Illustration = item.Illustration,
            Id = ModData.QEffectIds.ChosenWeaponQf

        };
        return chooseWeapon;
    }
    public static QEffect EyepieceChosenEffect()
    {
        return new QEffect()
        {
            StateCheck = self =>
            {
                Creature owner = self.Owner;
                if (owner.FindQEffect(ModData.QEffectIds.ChosenWeaponQf) != null)
                {
                    QEffect? effect = owner.FindQEffect(ModData.QEffectIds.ChosenWeaponQf);
                    Item? weapon = effect?.Tag as Item;
                    owner.AddQEffect(new QEffect()
                    {
                        ExpiresAt = ExpirationCondition.Ephemeral,
                        YouDealDamageWithStrike = (_, action, damage, _) => action.Item != weapon ? damage : damage.Add(DiceFormula.FromText($"{(owner.Proficiencies.Get(Trait.Crafting) < Proficiency.Master ? 1 : 2)}", "Enhanced"))
                    });
                }
                if (owner.FindQEffect(ModData.QEffectIds.ChosenShieldQf) != null)
                {
                    QEffect? effect = owner.FindQEffect(ModData.QEffectIds.ChosenShieldQf);
                    Item? shield = effect?.Tag as Item;
                    int hardness = Items.GetItemTemplate(shield!.ItemName).Hardness;
                    shield.WithShieldProperties(owner.Proficiencies.Get(Trait.Crafting) < Proficiency.Master ? (hardness+=1) : (hardness+=2));
                }
            }
        };
    }
    internal static CombatAction AddMaskToggle(Creature self)
    {
        CombatAction maskToggle = new(self, ModData.Illustrations.Mask, "Scoundrel's Toggle",
            [Trait.Basic, Trait.DoesNotBreakStealth],
            "This allows you to toggle the default behavior for the bonus damage that Scoundrel's Mask can deal. There are 5 options:" +
            "\n{b}Ask{/b}, which checks every time you could deal damage\n{b}Ask if Crit{/b}, which checks every time you crit\n{b}Auto{/b}, which deals the damage the first time it could be dealt\n{b}Auto if Crit{/b}, which deals the damage on the first relevant crit\n{b}Off{/b} which prevents the damage from being dealt." +
            "\n\nThe default option is {b}Ask if Crit{/b} unless changed in precombat preparations.",
            Target.Self());
        maskToggle.WithActionCost(0)
            .WithEffectOnSelf(async (action, innerSelf) =>
            {
                List<string> maskChoices = ["Off","Auto","Auto if Crit","Ask","Ask if Crit","Cancel"];
                if (innerSelf.HasEffect(ModData.QEffectIds.MaskOff))
                    maskChoices.RemoveAll(str => str == "Off");
                if (innerSelf.HasEffect(ModData.QEffectIds.MaskAuto))
                    maskChoices.RemoveAll(str => str == "Auto");
                if (innerSelf.HasEffect(ModData.QEffectIds.MaskAutoIfCrit))
                    maskChoices.RemoveAll(str => str == "Auto if Crit");
                if (innerSelf.HasEffect(ModData.QEffectIds.MaskAsk))
                    maskChoices.RemoveAll(str => str == "Ask");
                if (innerSelf.HasEffect(ModData.QEffectIds.MaskAskIfCrit))
                    maskChoices.RemoveAll(str => str == "Ask if Crit");
                ChoiceButtonOption chosenOption = await innerSelf.AskForChoiceAmongButtons(
                    IllustrationName.QuestionMark,
                    "Choose your preferred scoundrel's mask setting.",
                    maskChoices.ToArray());
                if (maskChoices[chosenOption.Index] != "Cancel")
                {
                    if (maskChoices[chosenOption.Index] == "Off")
                    {
                        innerSelf.RemoveAllQEffects(qff => HasMaskQf(qff.Id, ModData.QEffectIds.MaskOff));
                        self.AddQEffect(MaskOff());
                    }
                    if (maskChoices[chosenOption.Index] == "Auto")
                    {
                        innerSelf.RemoveAllQEffects(qff => HasMaskQf(qff.Id, ModData.QEffectIds.MaskAuto));
                        innerSelf.AddQEffect(MaskAuto());
                    }
                    if (maskChoices[chosenOption.Index] == "Auto if Crit")
                    {
                        innerSelf.RemoveAllQEffects(qff => HasMaskQf(qff.Id, ModData.QEffectIds.MaskAutoIfCrit));
                        innerSelf.AddQEffect(MaskAutoIfCrit());
                    }
                    if (maskChoices[chosenOption.Index] == "Ask")
                    {
                        innerSelf.RemoveAllQEffects(qff => HasMaskQf(qff.Id, ModData.QEffectIds.MaskAsk));
                        innerSelf.AddQEffect(MaskAsk());
                    }
                    if (maskChoices[chosenOption.Index] == "Ask if Crit")
                    {
                        innerSelf.RemoveAllQEffects(qff =>HasMaskQf(qff.Id, ModData.QEffectIds.MaskAskIfCrit));
                        innerSelf.AddQEffect(MaskAskIfCrit());
                    }
                }
                else action.RevertRequested = true;
            });
        return maskToggle;
    }
    internal static QEffect MaskOff()
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.MaskOff,
            Name = "Off",
            Description = "You will not deal damage with scoundrel's mask",
            Illustration = new PlainTextPortraitIllustration(Color.DarkRed, "O"),
            DoNotShowUpOverhead = true,
            Tag = "MaskToggle"
        };
    }
    internal static QEffect MaskAuto()
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.MaskAuto,
            Name = "Auto",
            Description = "You will deal scoundrel's mask's damage if conditions are met.",
            Illustration = new PlainTextPortraitIllustration(Color.Green, "AT"),
            DoNotShowUpOverhead = true,
            Tag = "MaskToggle"
        };
    }
    internal static QEffect MaskAutoIfCrit()
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.MaskAutoIfCrit,
            Name = "Auto if Crit",
            Description = "You will deal scoundrel's mask's damage if conditions are met and you crit.",
            Illustration = new PlainTextPortraitIllustration(Color.Green, "C"),
            DoNotShowUpOverhead = true,
            Tag = "MaskToggle"
        };
    }
    internal static QEffect MaskAsk()
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.MaskAsk,
            Name = "Ask",
            Description = "You will be asked to deal scoundrel's mask's damage if conditions are met.",
            Illustration = new PlainTextPortraitIllustration(Color.Blue, "AK"),
            DoNotShowUpOverhead = true,
            Tag = "MaskToggle"
        };
    }
    internal static QEffect MaskAskIfCrit()
    {
        return new QEffect()
        {
            Id = ModData.QEffectIds.MaskAskIfCrit,
            Name = "Ask if Crit",
            Description = "You will be asked to deal scoundrel's mask's damage if conditions are met and you crit.",
            Illustration = new PlainTextPortraitIllustration(Color.Blue, "C"),
            DoNotShowUpOverhead = true,
            Tag = "MaskToggle"
        };
    }

    internal static bool HasMaskQf(QEffectId effectId, QEffectId? toCheck)
    {
        if (effectId == toCheck)
            return false;
        return (effectId == ModData.QEffectIds.MaskAsk || effectId == ModData.QEffectIds.MaskAskIfCrit || effectId == ModData.QEffectIds.MaskAutoIfCrit ||  effectId == ModData.QEffectIds.MaskAuto || effectId == ModData.QEffectIds.MaskOff) && effectId != toCheck;
    }
    internal static bool HasMaskQf(Creature self)
    {
        return self.HasEffect(ModData.QEffectIds.MaskAsk) || self.HasEffect(ModData.QEffectIds.MaskAskIfCrit) || self.HasEffect(ModData.QEffectIds.MaskAutoIfCrit) ||  self.HasEffect(ModData.QEffectIds.MaskAuto) || self.HasEffect(ModData.QEffectIds.MaskAuto);
    }
}