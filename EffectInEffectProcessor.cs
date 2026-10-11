using System.Collections.Immutable;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace EffectInEffectPlugin;

internal sealed class EffectInEffectProcessor : IVideoEffectProcessor
{
    private readonly EffectInEffect _effect;
    private readonly IGraphicsDevicesAndContext _devices;

    private readonly List<IVideoEffectProcessor> _targetProcessors = [];
    private readonly List<IVideoEffectProcessor> _innerProcessors = [];

    private ImmutableList<IVideoEffect> _currentTarget = [];
    private ImmutableList<IVideoEffect> _currentInner = [];

    private DeltaMaskCustomEffect? _combiner;
    private ID2D1Image? _combinerOutput;
    private bool _combinerFailed;

    private ID2D1Image? _inputImage;
    private bool _disposed;

    public ID2D1Image Output { get; private set; } = null!;

    public EffectInEffectProcessor(EffectInEffect effect, IGraphicsDevicesAndContext devices)
    {
        _effect = effect;
        _devices = devices;
    }

    public DrawDescription Update(EffectDescription effectDescription)
    {
        if (_disposed || !_effect.IsEnabled)
        {
            Output = _inputImage!;
            return effectDescription.DrawDescription;
        }

        SynchronizeProcessors();

        ID2D1Image? input = _inputImage;
        var description = effectDescription;

        // 「かけられるエフェクト」を適用する
        ID2D1Image? target = ApplyChain(_targetProcessors, _currentTarget, input, ref description);

        // 「かけるエフェクト」を適用する
        ID2D1Image? inner = ApplyChain(_innerProcessors, _currentInner, target, ref description);

        if (HasEnabled(_currentTarget) && HasEnabled(_currentInner)
            && _effect.ApplyMode == EffectApplyMode.Masked
            && input is not null && target is not null && inner is not null
            && TryGetCombiner(out var combiner, out var combinerOutput))
        {
            // 対象が変化させた部分にだけ内側エフェクトの結果を合成する
            combiner.SetInput(0, input, true);
            combiner.SetInput(1, target, true);
            combiner.SetInput(2, inner, true);
            Output = combinerOutput;
        }
        else
        {
            // 全体に適用（またはフォールバック）
            Output = inner!;
        }

        return description.DrawDescription;
    }

    private static bool HasEnabled(ImmutableList<IVideoEffect> effects)
    {
        foreach (var effect in effects)
        {
            if (effect.IsEnabled)
                return true;
        }
        return false;
    }

    private static ID2D1Image? ApplyChain(
        List<IVideoEffectProcessor> processors,
        ImmutableList<IVideoEffect> effects,
        ID2D1Image? current,
        ref EffectDescription description)
    {
        for (int i = 0; i < processors.Count; i++)
        {
            if (i >= effects.Count || !effects[i].IsEnabled)
                continue;
            processors[i].SetInput(current);
            var updated = processors[i].Update(description);
            current = processors[i].Output;
            description = description with { DrawDescription = updated };
        }
        return current;
    }

    private bool TryGetCombiner(out DeltaMaskCustomEffect combiner, out ID2D1Image output)
    {
        if (_combiner is null && !_combinerFailed)
        {
            try
            {
                var created = new DeltaMaskCustomEffect(_devices);
                if (!created.IsEnabled)
                {
                    created.Dispose();
                    _combinerFailed = true;
                }
                else
                {
                    _combiner = created;
                    _combinerOutput = created.Output;
                }
            }
            catch
            {
                _combinerFailed = true;
            }
        }

        if (_combiner is not null && _combinerOutput is not null)
        {
            combiner = _combiner;
            output = _combinerOutput;
            return true;
        }

        combiner = null!;
        output = null!;
        return false;
    }

    public void SetInput(ID2D1Image? input)
    {
        if (!_disposed)
            _inputImage = input;
    }

    public void ClearInput()
    {
        if (!_disposed)
            _inputImage = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        DisposeProcessors(_targetProcessors);
        DisposeProcessors(_innerProcessors);
        _combinerOutput?.Dispose();
        _combinerOutput = null;
        if (_combiner is not null)
        {
            _combiner.SetInput(0, null, true);
            _combiner.SetInput(1, null, true);
            _combiner.SetInput(2, null, true);
            _combiner.Dispose();
            _combiner = null;
        }
    }

    private void SynchronizeProcessors()
    {
        SyncChain(_effect.TargetEffects, ref _currentTarget, _targetProcessors);
        SyncChain(_effect.InnerEffects, ref _currentInner, _innerProcessors);
    }

    private void SyncChain(ImmutableList<IVideoEffect> next, ref ImmutableList<IVideoEffect> current, List<IVideoEffectProcessor> processors)
    {
        if (ReferenceEquals(current, next) || current.SequenceEqual(next))
            return;
        DisposeProcessors(processors);
        foreach (var effect in next)
            processors.Add(effect.CreateVideoEffect(_devices));
        current = next;
    }

    private static void DisposeProcessors(List<IVideoEffectProcessor> processors)
    {
        foreach (var processor in processors)
            processor.Dispose();
        processors.Clear();
    }
}
