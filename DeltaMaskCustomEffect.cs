using System.Reflection;
using Vortice;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace EffectInEffectPlugin;

/// <summary>
/// 差分マスク合成カスタムエフェクト。
/// 入力0＝元画像、入力1＝かけられるエフェクト適用後、入力2＝かけるエフェクト適用後を受け取り、
/// かけられるエフェクトが変化させた部分だけにかけるエフェクトの結果を合成する。
/// </summary>
internal sealed class DeltaMaskCustomEffect(IGraphicsDevicesAndContext devices)
    : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
{
    [CustomEffect(3)]
    private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
    {
        public EffectImpl() : base(LoadShaderBytes())
        {
        }

        public override void MapInputRectsToOutputRect(RawRect[] inputRects, RawRect[] inputOpaqueSubRects, out RawRect outputRect, out RawRect outputOpaqueSubRect)
        {
            // 対象エフェクトは入力より出力が大きくなることがある（縁取り等）ため、全入力の和集合を出力範囲とする。
            // inputRects[0]だけを使うと、はみ出した部分が切り落とされてしまう。
            if (inputRects.Length == 0)
            {
                outputRect = default;
            }
            else
            {
                int left = inputRects[0].Left, top = inputRects[0].Top;
                int right = inputRects[0].Right, bottom = inputRects[0].Bottom;
                for (int i = 1; i < inputRects.Length; i++)
                {
                    if (inputRects[i].Left < left) left = inputRects[i].Left;
                    if (inputRects[i].Top < top) top = inputRects[i].Top;
                    if (inputRects[i].Right > right) right = inputRects[i].Right;
                    if (inputRects[i].Bottom > bottom) bottom = inputRects[i].Bottom;
                }
                outputRect = new RawRect(left, top, right, bottom);
            }
            outputOpaqueSubRect = default;
        }

        public override void MapOutputRectToInputRects(RawRect outputRect, RawRect[] inputRects)
        {
            for (int i = 0; i < inputRects.Length; i++)
                inputRects[i] = outputRect;
        }

        protected override void UpdateConstants()
        {
        }

        private static byte[] LoadShaderBytes()
        {
            var assembly = typeof(EffectImpl).Assembly;
            using var stream = assembly.GetManifestResourceStream("EffectInEffectPlugin.DeltaMask.cso")
                ?? throw new InvalidOperationException("DeltaMask.cso が見つかりません。");
            var bytes = new byte[stream.Length];
            stream.ReadExactly(bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
