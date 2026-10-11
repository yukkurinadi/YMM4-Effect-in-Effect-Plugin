using System.ComponentModel.DataAnnotations;

namespace EffectInEffectPlugin;

/// <summary>
/// 「かけるエフェクト」を適用する範囲。
/// </summary>
public enum EffectApplyMode
{
    [Display(Name = "変化部分のみ", Description = "「かけられるエフェクト」が変化させた部分にだけ「かけるエフェクト」を適用します。変化していない部分は元のまま残ります。")]
    Masked = 0,

    [Display(Name = "全体", Description = "「かけられるエフェクト」の適用結果全体に「かけるエフェクト」を適用します。")]
    Whole = 1,
}
