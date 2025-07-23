using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace New_Skill_Feats_and_Items;

internal abstract class ModData
{
    internal static class QEffectIds
    {
        internal static QEffectId ChosenShieldQf { get; } = ModManager.RegisterEnumMember<QEffectId>("Chosen Shield");
        internal static QEffectId ChosenWeaponQf { get; } = ModManager.RegisterEnumMember<QEffectId>("Chosen Weapon");
        internal static QEffectId DirtyTricked { get; } = ModManager.RegisterEnumMember<QEffectId>("DirtyTricked");
        internal static QEffectId RootMagic { get; } = ModManager.RegisterEnumMember<QEffectId>("RootMagic");
        internal static QEffectId AssuranceOn { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceOn");
        internal static QEffectId AssuranceOff { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceOff");
        internal static QEffectId AssuranceAsk { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceAsk");
        internal static QEffectId Terrify { get; } = ModManager.RegisterEnumMember<QEffectId>("Terrify");
        internal static QEffectId Envy { get; } = ModManager.RegisterEnumMember<QEffectId>("Envy");
        internal static QEffectId Talent { get; } = ModManager.RegisterEnumMember<QEffectId>("Talent");
        internal static QEffectId TalEnvy { get; } = ModManager.RegisterEnumMember<QEffectId>("TalEnvy");
        internal static QEffectId MaskAsk { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAsk");
        internal static QEffectId MaskAskIfCrit { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAskIfCrit");
        internal static QEffectId MaskAuto { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAuto");
        internal static QEffectId MaskAutoIfCrit { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAutoIfCrit");
        internal static QEffectId MaskOff { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskOff");
    }
    internal static class FeatNames
    {
        internal static readonly FeatName RootMagicFeat = ModManager.RegisterFeatName("RootMagicFeat", "Root Magic");
        internal static readonly FeatName Assurance = ModManager.RegisterFeatName("Assurance", "Assurance");
        internal static readonly FeatName DirtyTrick = ModManager.RegisterFeatName("DirtyTrick", "Dirty Trick");
        internal static readonly FeatName AssuranceOn = ModManager.RegisterFeatName("AssuranceOn", "Assurance - On");
        internal static readonly FeatName AssuranceOff = ModManager.RegisterFeatName("AssuranceOff", "Assurance - Off");
        internal static readonly FeatName AssuranceAsk = ModManager.RegisterFeatName("AssuranceAsk", "Assurance - Ask");
        internal static readonly FeatName AssuranceThreshold = ModManager.RegisterFeatName("AssuranceThreshold", "Assurance - Threshold");
        internal static readonly FeatName Virtuoso = ModManager.RegisterFeatName("VirtuosicPerformer", "Virtuosic Performer");
        internal static readonly FeatName TalentEnvy = ModManager.RegisterFeatName("TalentEnvy", "Talent Envy");
    }
    internal static class ActionIds
    {
        internal static readonly ActionId DirtyTrickId = ModManager.RegisterEnumMember<ActionId>("DirtyTrickId");
        internal static readonly ActionId EvangelizeId = ModManager.RegisterEnumMember<ActionId>("EvangelizeId");
    }
    
    internal static class Illustrations
    {
        internal static readonly Illustration Mask = new ModdedIllustration("SIAssets/Mask.png");
    }
    
}