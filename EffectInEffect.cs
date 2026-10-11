using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace EffectInEffectPlugin;

/// <summary>
/// すべての映像エフェクトに「エフェクト内エフェクト」をかけられるようにするための万能ラッパー。
/// 「かけられるエフェクト」にかけたい対象を入れ、「かけるエフェクト」に内側エフェクトを入れると、
/// 既定では対象が変化させた部分にだけ内側エフェクトが適用される。
/// 入れ子は無制限（このエフェクト自体を内側に入れることも可能）。
/// </summary>
[VideoEffect("エフェクト内エフェクト", [VideoEffectCategories.Decoration], ["エフェクト内エフェクト", "effect in effect", "入れ子", "ネスト", "ラッパー", "wrapper", "コンテナ", "container"], IsAviUtlSupported = false)]
public sealed class EffectInEffect : VideoEffectBase
{
    public override string Label => $"エフェクト内エフェクト (対象{TargetEffects.Count}/追加{InnerEffects.Count})";

    [Display(GroupName = "エフェクト内エフェクト", Name = "かけられるエフェクト", Description = "エフェクトをかけられる側（対象）の映像エフェクト。すべての映像エフェクトをここに入れることができます。複数入れた場合は上から順に適用されます。")]
    [VideoEffectSelector(PropertyEditorSize = PropertyEditorSize.FullWidth)]
    public ImmutableList<IVideoEffect> TargetEffects
    {
        get;
        set
        {
            if (!Set(ref field, value))
                return;
            OnPropertyChanged(nameof(Label));
        }
    } = [];

    [Display(GroupName = "エフェクト内エフェクト", Name = "かけるエフェクト", Description = "「かけられるエフェクト」の適用結果に対してかける内側エフェクト。上から順に適用されます。")]
    [VideoEffectSelector(PropertyEditorSize = PropertyEditorSize.FullWidth)]
    public ImmutableList<IVideoEffect> InnerEffects
    {
        get;
        set
        {
            if (!Set(ref field, value))
                return;
            OnPropertyChanged(nameof(Label));
        }
    } = [];

    [Display(GroupName = "エフェクト内エフェクト", Name = "適用範囲", Description = "「かけるエフェクト」を適用する範囲。「変化部分のみ」では対象エフェクトが変化させた部分以外は元のまま残ります。")]
    [EnumComboBox]
    public EffectApplyMode ApplyMode
    {
        get => applyMode;
        set => Set(ref applyMode, value);
    }
    EffectApplyMode applyMode = EffectApplyMode.Masked;

    public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) =>
        new EffectInEffectProcessor(this, devices);

    public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) =>
        [];

    protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [.. TargetEffects, .. InnerEffects];
}
