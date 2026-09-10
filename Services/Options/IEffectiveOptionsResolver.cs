using MediaForge.Models;

namespace MediaForge.Services.Options;

public interface IEffectiveOptionsResolver
{
    EffectiveOptionsResult Resolve(EffectiveOptionsRequest request);
}
