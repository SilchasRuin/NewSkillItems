using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace New_Skill_Feats_and_Items;

public abstract class ModData
{
    public static class QEffectIds
    {
        public static QEffectId ChosenShieldQf { get; } = ModManager.RegisterEnumMember<QEffectId>("Chosen Shield");
        public static QEffectId ChosenWeaponQf { get; } = ModManager.RegisterEnumMember<QEffectId>("Chosen Weapon");
        public static QEffectId DirtyTricked { get; } = ModManager.RegisterEnumMember<QEffectId>("DirtyTricked");
        public static QEffectId RootMagic { get; } = ModManager.RegisterEnumMember<QEffectId>("RootMagic");
        public static QEffectId AssuranceOn { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceOn");
        public static QEffectId AssuranceOff { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceOff");
        public static QEffectId AssuranceAsk { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceAsk");
        public static QEffectId Terrify { get; } = ModManager.RegisterEnumMember<QEffectId>("Terrify");
        public static QEffectId Envy { get; } = ModManager.RegisterEnumMember<QEffectId>("Envy");
        public static QEffectId Talent { get; } = ModManager.RegisterEnumMember<QEffectId>("Talent");
        public static QEffectId TalEnvy { get; } = ModManager.RegisterEnumMember<QEffectId>("TalEnvy");
        public static QEffectId MaskAsk { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAsk");
        public static QEffectId MaskAskIfCrit { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAskIfCrit");
        public static QEffectId MaskAuto { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAuto");
        public static QEffectId MaskAutoIfCrit { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskAutoIfCrit");
        public static QEffectId MaskOff { get; } = ModManager.RegisterEnumMember<QEffectId>("MaskOff");
        public static QEffectId AssuranceFeat { get; } = ModManager.RegisterEnumMember<QEffectId>("AssuranceFeat");
    }
    public static class FeatNames
    {
        public static readonly FeatName RootMagicFeat = ModManager.RegisterFeatName("RootMagicFeat", "Root Magic");
        public static readonly FeatName Assurance = ModManager.RegisterFeatName("Assurance", "Assurance");
        public static readonly FeatName DirtyTrick = ModManager.RegisterFeatName("DirtyTrick", "Dirty Trick");
        public static readonly FeatName AssuranceOn = ModManager.RegisterFeatName("AssuranceOn", "Assurance - On");
        public static readonly FeatName AssuranceOff = ModManager.RegisterFeatName("AssuranceOff", "Assurance - Off");
        public static readonly FeatName AssuranceAsk = ModManager.RegisterFeatName("AssuranceAsk", "Assurance - Ask");
        public static readonly FeatName AssuranceThreshold = ModManager.RegisterFeatName("AssuranceThreshold", "Assurance - Threshold");
        public static readonly FeatName Virtuoso = ModManager.RegisterFeatName("VirtuosicPerformer", "Virtuosic Performer");
        public static readonly FeatName TalentEnvy = ModManager.RegisterFeatName("TalentEnvy", "Talent Envy");
    }
    public static class ActionIds
    {
        public static readonly ActionId DirtyTrickId = ModManager.RegisterEnumMember<ActionId>("DirtyTrickId");
        public static readonly ActionId EvangelizeId = ModManager.RegisterEnumMember<ActionId>("EvangelizeId");
    }
    
    public static class Illustrations
    {
        public static readonly Illustration Mask = new ModdedIllustration("SIAssets/Mask.png");
    }
    
}